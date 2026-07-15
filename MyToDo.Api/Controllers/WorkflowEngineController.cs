using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Extensions;
using MyToDo.Api.Services.Workflow;

namespace MyToDo.Api.Controllers
{
    /// <summary>
    /// 工作流调试与演示入口。
    /// 负责初始化示例流程、启动流程、触发排程恢复，以及查询流程实例状态。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowEngineController : ControllerBase
    {
        private readonly MyToDoContext _context;
        private readonly IWorkflowRuntime _workflowRuntime;
        private readonly IApsScheduler _apsScheduler;
        private readonly ILogger<WorkflowEngineController> _logger;

        public WorkflowEngineController(
            MyToDoContext context,
            IWorkflowRuntime workflowRuntime,
            IApsScheduler apsScheduler,
            ILogger<WorkflowEngineController> logger)
        {
            _context = context;
            _workflowRuntime = workflowRuntime;
            _apsScheduler = apsScheduler;
            _logger = logger;
        }

        /// <summary>
        /// 创建一条最小可运行的演示流程及其工单，便于联调工作流、排程和工位回调链路。
        /// </summary>
        [HttpPost("bootstrap")]
        public async Task<ApiResponse<object>> BootstrapAsync([FromBody] BootstrapWorkflowRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var sanitizedWorkflowName = WorkflowLogSanitizer.Sanitize(request.WorkflowName);
                var sanitizedWorkOrderNo = WorkflowLogSanitizer.Sanitize(request.WorkOrderNo);
                var sanitizedRequiredResourceType = WorkflowLogSanitizer.Sanitize(request.RequiredResourceType);

                _logger.LogInformation(
                    "Bootstrapping demo workflow. WorkflowName={WorkflowName}, WorkOrderNo={WorkOrderNo}, RequiredResourceType={RequiredResourceType}",
                    sanitizedWorkflowName,
                    sanitizedWorkOrderNo,
                    sanitizedRequiredResourceType);

                var workOrderNo = string.IsNullOrWhiteSpace(request.WorkOrderNo)
                    ? $"WO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28]
                    : request.WorkOrderNo;

                var workflow = new Workflow
                {
                    Id = Guid.NewGuid(),
                    Name = request.WorkflowName,
                    CreatedAt = DateTime.UtcNow
                };

                var version = new WorkflowVersion
                {
                    Id = Guid.NewGuid(),
                    WorkflowId = workflow.Id,
                    VersionNumber = 1,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow
                };

                var startNode = new WorkflowNode
                {
                    Id = Guid.NewGuid(),
                    WorkflowVersionId = version.Id,
                    NodeKey = "start",
                    NodeType = WorkflowNodeType.Start
                };
                var scheduleNode = new WorkflowNode
                {
                    Id = Guid.NewGuid(),
                    WorkflowVersionId = version.Id,
                    NodeKey = "schedule",
                    NodeType = WorkflowNodeType.ScheduleTask,
                    RequiredResourceType = request.RequiredResourceType,
                    EstimatedDurationMinutes = request.EstimatedDurationMinutes
                };
                var workstationNode = new WorkflowNode
                {
                    Id = Guid.NewGuid(),
                    WorkflowVersionId = version.Id,
                    NodeKey = "workstation",
                    NodeType = WorkflowNodeType.WorkstationTask,
                    RequiredResourceType = request.RequiredResourceType
                };
                var endNode = new WorkflowNode
                {
                    Id = Guid.NewGuid(),
                    WorkflowVersionId = version.Id,
                    NodeKey = "end",
                    NodeType = WorkflowNodeType.End
                };

                var edges = new[]
                {
                    new WorkflowEdge { Id = Guid.NewGuid(), WorkflowVersionId = version.Id, FromNodeId = startNode.Id, ToNodeId = scheduleNode.Id },
                    new WorkflowEdge { Id = Guid.NewGuid(), WorkflowVersionId = version.Id, FromNodeId = scheduleNode.Id, ToNodeId = workstationNode.Id },
                    new WorkflowEdge { Id = Guid.NewGuid(), WorkflowVersionId = version.Id, FromNodeId = workstationNode.Id, ToNodeId = endNode.Id }
                };

                var workOrder = new WorkOrder
                {
                    Id = Guid.NewGuid(),
                    WorkOrderNo = workOrderNo,
                    WorkflowVersionId = version.Id,
                    Priority = request.Priority,
                    EarliestStartTime = DateTime.UtcNow,
                    Status = WorkOrderStatus.Submitted,
                    CreatedAt = DateTime.UtcNow
                };

                var resource = new SchedulingResource
                {
                    Id = Guid.NewGuid(),
                    Name = $"{request.RequiredResourceType}-A",
                    ResourceType = request.RequiredResourceType
                };

                _context.Workflows.Add(workflow);
                _context.WorkflowVersions.Add(version);
                _context.WorkflowNodes.AddRange(startNode, scheduleNode, workstationNode, endNode);
                _context.WorkflowEdges.AddRange(edges);
                _context.WorkOrders.Add(workOrder);
                _context.SchedulingResources.Add(resource);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Demo workflow bootstrapped. WorkflowId={WorkflowId}, WorkflowVersionId={WorkflowVersionId}, WorkOrderId={WorkOrderId}",
                    workflow.Id,
                    version.Id,
                    workOrder.Id);

                return new ApiResponse<object>(true, "初始化成功", new
                {
                    WorkflowId = workflow.Id,
                    WorkflowVersionId = version.Id,
                    WorkOrderId = workOrder.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bootstrap demo workflow. WorkflowName={WorkflowName}", WorkflowLogSanitizer.Sanitize(request.WorkflowName));
                return new ApiResponse<object>(false, $"初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动指定工单对应的流程版本。
        /// 启动后流程会自动推进，直到遇到等待点或直接结束。
        /// </summary>
        [HttpPost("start")]
        public async Task<ApiResponse<object>> StartAsync([FromBody] StartWorkflowRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starting workflow through API. WorkOrderId={WorkOrderId}, WorkflowVersionId={WorkflowVersionId}",
                request.WorkOrderId,
                request.WorkflowVersionId);

            var instance = await _workflowRuntime.StartAsync(request.WorkOrderId, request.WorkflowVersionId, cancellationToken);

            _logger.LogInformation(
                "Workflow started through API. WorkflowInstanceId={WorkflowInstanceId}, WorkOrderId={WorkOrderId}",
                instance.Id,
                request.WorkOrderId);

            return new ApiResponse<object>(true, "流程启动成功", new { WorkflowInstanceId = instance.Id });
        }

        /// <summary>
        /// 执行一次 APS 排程，并把已成功排程的任务结果回填到等待中的流程实例。
        /// </summary>
        [HttpPost("schedule")]
        public async Task<ApiResponse<object>> ScheduleAsync(CancellationToken cancellationToken)
        {
            var results = await _apsScheduler.ScheduleAsync(cancellationToken);

            _logger.LogInformation("APS scheduling finished. ScheduledCount={ScheduledCount}", results.Count);

            foreach (var result in results)
            {
                var resumed = await _workflowRuntime.ResumeAsync(
                    WorkflowBookmarkTypes.ScheduleTaskScheduled,
                    result.SchedulableTaskId.ToString(),
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Processed scheduled task resume. BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}, Resumed={Resumed}, ResourceId={ResourceId}",
                    WorkflowBookmarkTypes.ScheduleTaskScheduled,
                    result.SchedulableTaskId,
                    resumed,
                    result.ResourceId);
            }

            return new ApiResponse<object>(true, "排程执行完成", new
            {
                ScheduledCount = results.Count,
                Results = results.Select(x => new
                {
                    x.SchedulableTaskId,
                    x.ResourceId,
                    x.StartTime,
                    x.EndTime
                }).ToList()
            });
        }

        /// <summary>
        /// 手动按书签恢复流程，适合模拟外部系统回调或事件到达场景。
        /// </summary>
        [HttpPost("resume")]
        public async Task<ApiResponse<bool>> ResumeAsync([FromBody] ResumeBookmarkRequest request, CancellationToken cancellationToken)
        {
            var sanitizedBookmarkType = WorkflowLogSanitizer.Sanitize(request.BookmarkType);
            var sanitizedBookmarkKey = WorkflowLogSanitizer.Sanitize(request.BookmarkKey);

            _logger.LogInformation(
                "Resuming workflow through API. BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}",
                sanitizedBookmarkType,
                sanitizedBookmarkKey);

            var resumed = await _workflowRuntime.ResumeAsync(request.BookmarkType, request.BookmarkKey, request.Input, cancellationToken);

            if (!resumed)
            {
                _logger.LogWarning(
                    "Workflow resume request did not match any active bookmark. BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}",
                    sanitizedBookmarkType,
                    sanitizedBookmarkKey);
            }

            return new ApiResponse<bool>(resumed, resumed ? "恢复成功" : "未找到可恢复的书签", resumed);
        }

        /// <summary>
        /// 查询流程实例当前状态，便于查看挂起书签和排程任务的运行情况。
        /// </summary>
        [HttpGet("instances/{instanceId:guid}")]
        public async Task<ApiResponse<object>> GetInstanceStatusAsync(Guid instanceId, CancellationToken cancellationToken)
        {
            var instance = await _context.WorkflowInstances
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == instanceId, cancellationToken);

            if (instance == null)
            {
                return new ApiResponse<object>(false, "流程实例不存在");
            }

            var bookmarks = await _context.WorkflowBookmarks
                .AsNoTracking()
                .Where(x => x.WorkflowInstanceId == instanceId && x.Status == WorkflowBookmarkStatus.Active)
                .Select(x => new { x.BookmarkType, x.BookmarkKey })
                .ToListAsync(cancellationToken);

            var tasks = await _context.SchedulableTasks
                .AsNoTracking()
                .Where(x => x.WorkflowInstanceId == instanceId)
                .Select(x => new
                {
                    x.Id,
                    x.Status,
                    x.RequiredResourceType,
                    x.ScheduledResourceId,
                    x.ScheduledStartTime,
                    x.ScheduledEndTime
                })
                .ToListAsync(cancellationToken);

            return new ApiResponse<object>(true, "获取成功", new
            {
                instance.Id,
                instance.WorkOrderId,
                instance.Status,
                instance.StartedAt,
                instance.CompletedAt,
                Bookmarks = bookmarks,
                Tasks = tasks
            });
        }
    }
}
