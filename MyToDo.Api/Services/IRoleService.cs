using MyToDo.Api.Extensions;

namespace MyToDo.Api.Services
{
    public interface IRoleService
    {
        Task<ApiResponse<IList<RoleDto>>> GetAllAsync(string? search = null);
        Task<ApiResponse<RoleDto>> GetByIdAsync(int id);
        Task<ApiResponse<RoleDto>> AddAsync(RoleDto dto);
        Task<ApiResponse<RoleDto>> UpdateAsync(RoleDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<bool>> AssignPermissionsAsync(AssignPermissionsDto dto);
        Task<ApiResponse<IList<PermissionDto>>> GetAllPermissionsAsync();
        Task<ApiResponse<PermissionDto>> AddPermissionAsync(PermissionDto dto);
        Task<ApiResponse<PermissionDto>> UpdatePermissionAsync(PermissionDto dto);
        Task<ApiResponse<bool>> DeletePermissionAsync(int id);
    }
}
