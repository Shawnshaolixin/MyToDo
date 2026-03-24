using Microsoft.AspNetCore.Mvc;
using MyToDo.Api.Extensions;
using MyToDo.Api.Services;

namespace MyToDo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemoController : ControllerBase
    {
        private readonly IMemoService _service;

        public MemoController(IMemoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ApiResponse<IList<MemoDto>>> GetAll([FromQuery] string? search)
        {
            return await _service.GetAllAsync(search);
        }

        [HttpGet("{id}")]
        public async Task<ApiResponse<MemoDto>> GetById(int id)
        {
            return await _service.GetByIdAsync(id);
        }

        [HttpPost]
        public async Task<ApiResponse<MemoDto>> Add([FromBody] MemoDto dto)
        {
            return await _service.AddAsync(dto);
        }

        [HttpPut]
        public async Task<ApiResponse<MemoDto>> Update([FromBody] MemoDto dto)
        {
            return await _service.UpdateAsync(dto);
        }

        [HttpDelete("{id}")]
        public async Task<ApiResponse<bool>> Delete(int id)
        {
            return await _service.DeleteAsync(id);
        }
    }
}
