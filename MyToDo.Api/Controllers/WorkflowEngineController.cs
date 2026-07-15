using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Extensions;
using MyToDo.Api.Services.Workflow;

namespace MyToDo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowEngineController : ControllerBase
    {
        private readonly MyToDoContext _context;
        private readonly IWorkflowRuntime _workflowRuntime;
        private readonly IApsScheduler _apsScheduler;

        public WorkflowEngineController(
            MyToDoContext context,
            IWorkflowRuntime workflowRuntime,
            IApsScheduler apsScheduler)
        {
            _context = context;
            _workflowRuntime = workflowRuntime;
            _apsScheduler = apsScheduler;
        }

        [HttpPost("bootstrap")]
        public async Task<ApiResponse<object>> BootstrapAsync([FromBody] BootstrapWorkflowRequest request, CancellationToken cancellationToken)
        {
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
                WorkOrderNo = request.WorkOrderNo,
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

            return new ApiResponse<object>(true, "初始化成功", new
            {
                WorkflowId = workflow.Id,
                WorkflowVersionId = version.Id,
                WorkOrderId = workOrder.Id
            });
        }

        [HttpPost("start")]
        public async Task<ApiResponse<object>> StartAsync([FromBody] StartWorkflowRequest request, CancellationToken cancellationToken)
        {
            var instance = await _workflowRuntime.StartAsync(request.WorkOrderId, request.WorkflowVersionId, cancellationToken);
            return new ApiResponse<object>(true, "流程启动成功", new { WorkflowInstanceId = instance.Id });
        }

        [HttpPost("schedule")]
        public async Task<ApiResponse<object>> ScheduleAsync(CancellationToken cancellationToken)
        {
            var results = await _apsScheduler.ScheduleAsync(cancellationToken);

            foreach (var result in results)
            {
                await _workflowRuntime.ResumeAsync(
                    WorkflowBookmarkTypes.ScheduleTaskScheduled,
                    result.SchedulableTaskId.ToString(),
                    result,
                    cancellationToken);
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

        [HttpPost("resume")]
        public async Task<ApiResponse<bool>> ResumeAsync([FromBody] ResumeBookmarkRequest request, CancellationToken cancellationToken)
        {
            var resumed = await _workflowRuntime.ResumeAsync(request.BookmarkType, request.BookmarkKey, request.Input, cancellationToken);
            return new ApiResponse<bool>(resumed, resumed ? "恢复成功" : "未找到可恢复的书签", resumed);
        }

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
