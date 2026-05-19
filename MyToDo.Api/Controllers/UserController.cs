using Microsoft.AspNetCore.Mvc;
using MyToDo.Api.Extensions;
using MyToDo.Api.Services;

namespace MyToDo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ApiResponse<IList<UserDto>>> GetAll([FromQuery] string? search, [FromQuery] int? status)
        {
            return await _service.GetAllAsync(search, status);
        }

        [HttpGet("{id}")]
        public async Task<ApiResponse<UserDto>> GetById(int id)
        {
            return await _service.GetByIdAsync(id);
        }

        [HttpPost]
        public async Task<ApiResponse<UserDto>> Add([FromBody] UserDto dto)
        {
            return await _service.AddAsync(dto);
        }

        [HttpPut]
        public async Task<ApiResponse<UserDto>> Update([FromBody] UserDto dto)
        {
            return await _service.UpdateAsync(dto);
        }

        [HttpDelete("{id}")]
        public async Task<ApiResponse<bool>> Delete(int id)
        {
            return await _service.DeleteAsync(id);
        }

        [HttpPost("assign-roles")]
        public async Task<ApiResponse<bool>> AssignRoles([FromBody] AssignRolesDto dto)
        {
            return await _service.AssignRolesAsync(dto);
        }
    }
}
