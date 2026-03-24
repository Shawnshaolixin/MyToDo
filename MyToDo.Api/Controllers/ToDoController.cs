using Microsoft.AspNetCore.Mvc;
using MyToDo.Api.Extensions;
using MyToDo.Api.Services;

namespace MyToDo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToDoController : ControllerBase
    {
        private readonly IToDoService _service;

        public ToDoController(IToDoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ApiResponse<IList<ToDoDto>>> GetAll([FromQuery] string? search, [FromQuery] int? status)
        {
            return await _service.GetAllAsync(search, status);
        }

        [HttpGet("{id}")]
        public async Task<ApiResponse<ToDoDto>> GetById(int id)
        {
            return await _service.GetByIdAsync(id);
        }

        [HttpPost]
        public async Task<ApiResponse<ToDoDto>> Add([FromBody] ToDoDto dto)
        {
            return await _service.AddAsync(dto);
        }

        [HttpPut]
        public async Task<ApiResponse<ToDoDto>> Update([FromBody] ToDoDto dto)
        {
            return await _service.UpdateAsync(dto);
        }

        [HttpDelete("{id}")]
        public async Task<ApiResponse<bool>> Delete(int id)
        {
            return await _service.DeleteAsync(id);
        }

        [HttpGet("summary")]
        public async Task<ApiResponse<SummaryDto>> GetSummary()
        {
            return await _service.GetSummaryAsync();
        }
    }
}
