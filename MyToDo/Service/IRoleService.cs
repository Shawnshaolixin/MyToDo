using MyToDo.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyToDo.Service
{
    public interface IRoleService
    {
        Task<ApiResponse<List<RoleDto>>> GetAllAsync(string? search = null);
        Task<ApiResponse<RoleDto>> GetByIdAsync(int id);
        Task<ApiResponse<RoleDto>> AddAsync(RoleDto dto);
        Task<ApiResponse<RoleDto>> UpdateAsync(RoleDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<bool>> AssignPermissionsAsync(int roleId, List<int> permissionIds);
        Task<ApiResponse<List<PermissionDto>>> GetAllPermissionsAsync();
        Task<ApiResponse<PermissionDto>> AddPermissionAsync(PermissionDto dto);
        Task<ApiResponse<PermissionDto>> UpdatePermissionAsync(PermissionDto dto);
        Task<ApiResponse<bool>> DeletePermissionAsync(int id);
    }
}
