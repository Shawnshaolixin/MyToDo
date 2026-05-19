using Microsoft.AspNetCore.Mvc;
using MyToDo.Api.Extensions;
using MyToDo.Api.Services;

namespace MyToDo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _service;

        public RoleController(IRoleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ApiResponse<IList<RoleDto>>> GetAll([FromQuery] string? search)
        {
            return await _service.GetAllAsync(search);
        }

        [HttpGet("{id}")]
        public async Task<ApiResponse<RoleDto>> GetById(int id)
        {
            return await _service.GetByIdAsync(id);
        }

        [HttpPost]
        public async Task<ApiResponse<RoleDto>> Add([FromBody] RoleDto dto)
        {
            return await _service.AddAsync(dto);
        }

        [HttpPut]
        public async Task<ApiResponse<RoleDto>> Update([FromBody] RoleDto dto)
        {
            return await _service.UpdateAsync(dto);
        }

        [HttpDelete("{id}")]
        public async Task<ApiResponse<bool>> Delete(int id)
        {
            return await _service.DeleteAsync(id);
        }

        [HttpPost("assign-permissions")]
        public async Task<ApiResponse<bool>> AssignPermissions([FromBody] AssignPermissionsDto dto)
        {
            return await _service.AssignPermissionsAsync(dto);
        }

        [HttpGet("permissions")]
        public async Task<ApiResponse<IList<PermissionDto>>> GetAllPermissions()
        {
            return await _service.GetAllPermissionsAsync();
        }

        [HttpPost("permissions")]
        public async Task<ApiResponse<PermissionDto>> AddPermission([FromBody] PermissionDto dto)
        {
            return await _service.AddPermissionAsync(dto);
        }

        [HttpPut("permissions")]
        public async Task<ApiResponse<PermissionDto>> UpdatePermission([FromBody] PermissionDto dto)
        {
            return await _service.UpdatePermissionAsync(dto);
        }

        [HttpDelete("permissions/{id}")]
        public async Task<ApiResponse<bool>> DeletePermission(int id)
        {
            return await _service.DeletePermissionAsync(id);
        }
    }
}
