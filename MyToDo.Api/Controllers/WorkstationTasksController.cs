using Microsoft.AspNetCore.Mvc;
using MyToDo.Api.Extensions;
using MyToDo.Api.Services.Workstation;

namespace MyToDo.Api.Controllers
{
    /// <summary>
    /// API endpoints for managing workstation task instances within a running workflow.
    /// A WorkstationTask node is created by the workflow runtime and exposed here
    /// for the operator to configure, start, and monitor.
    /// </summary>
    [ApiController]
    [Route("api/workstation-tasks")]
    public class WorkstationTasksController : ControllerBase
    {
        private readonly WorkstationTaskAppService _service;

        public WorkstationTasksController(WorkstationTaskAppService service)
        {
            _service = service;
        }

        /// <summary>Returns the list of experiments available on the associated workstation.</summary>
        [HttpGet("{workflowNodeInstanceId:guid}/experiments")]
        public async Task<ApiResponse<object>> GetExperimentsAsync(
            Guid workflowNodeInstanceId,
            CancellationToken cancellationToken)
        {
            var experiments = await _service.GetExperimentsAsync(workflowNodeInstanceId, cancellationToken);
            return new ApiResponse<object>(true, "获取成功", experiments);
        }

        /// <summary>Returns the parameter schema for a specific experiment.</summary>
        [HttpGet("{workflowNodeInstanceId:guid}/experiments/{experimentId}/parameters")]
        public async Task<ApiResponse<object>> GetExperimentParametersAsync(
            Guid workflowNodeInstanceId,
            string experimentId,
            CancellationToken cancellationToken)
        {
            var parameters = await _service.GetExperimentParametersAsync(
                workflowNodeInstanceId, experimentId, cancellationToken);
            return new ApiResponse<object>(true, "获取成功", parameters);
        }

        /// <summary>Saves the operator's experiment selection and parameters.</summary>
        [HttpPost("{workflowNodeInstanceId:guid}/config")]
        public async Task<ApiResponse<bool>> SaveConfigAsync(
            Guid workflowNodeInstanceId,
            [FromBody] SaveConfigRequest request,
            CancellationToken cancellationToken)
        {
            await _service.SaveConfigAsync(
                workflowNodeInstanceId,
                request.ExperimentDefinitionId,
                request.ParametersJson,
                cancellationToken);
            return new ApiResponse<bool>(true, "保存成功", true);
        }

        /// <summary>Sends the start command to the workstation device.</summary>
        [HttpPost("{workflowNodeInstanceId:guid}/start")]
        public async Task<ApiResponse<object>> StartExperimentAsync(
            Guid workflowNodeInstanceId,
            CancellationToken cancellationToken)
        {
            var result = await _service.StartExperimentAsync(workflowNodeInstanceId, cancellationToken);
            return new ApiResponse<object>(result.Success, result.ErrorMessage ?? "实验已启动",
                new { result.DeviceJobId });
        }

        /// <summary>Resolves an operator prompt.</summary>
        [HttpPost("prompts/{promptId:guid}/resolve")]
        public async Task<ApiResponse<bool>> ResolvePromptAsync(
            Guid promptId,
            [FromBody] ResolvePromptRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.ResolvePromptAsync(promptId, request.Resolution, cancellationToken);
            return new ApiResponse<bool>(result.Success, result.ErrorMessage ?? "操作成功", result.Success);
        }
    }

    public class SaveConfigRequest
    {
        public string ExperimentDefinitionId { get; set; } = string.Empty;
        public string ParametersJson { get; set; } = "{}";
    }

    public class ResolvePromptRequest
    {
        public string Resolution { get; set; } = string.Empty;
    }
}
