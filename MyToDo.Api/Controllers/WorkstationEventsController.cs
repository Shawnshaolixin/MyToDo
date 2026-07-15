using Microsoft.AspNetCore.Mvc;
using MyToDo.Api.Extensions;
using MyToDo.Api.Services.Workstation;

namespace MyToDo.Api.Controllers
{
    /// <summary>
    /// Receives event callbacks from workstation devices.
    /// The device posts events here; WorkstationEventAppService persists them
    /// and drives workflow resumption.
    /// </summary>
    [ApiController]
    [Route("api/workstations")]
    public class WorkstationEventsController : ControllerBase
    {
        private readonly WorkstationEventAppService _service;

        public WorkstationEventsController(WorkstationEventAppService service)
        {
            _service = service;
        }

        /// <summary>
        /// Accepts a device event.  Supported EventType values:
        /// ExperimentCompleted, ExperimentFailed, PromptRaised, StatusUpdate.
        /// </summary>
        [HttpPost("events")]
        public async Task<ApiResponse<bool>> ReceiveEventAsync(
            [FromBody] WorkstationEventInput input,
            CancellationToken cancellationToken)
        {
            await _service.HandleEventAsync(input, cancellationToken);
            return new ApiResponse<bool>(true, "事件已接收", true);
        }
    }
}
