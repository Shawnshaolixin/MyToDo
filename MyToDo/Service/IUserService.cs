using MyToDo.Common.Models;
using MyToDo.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyToDo.Service
{
    public interface IUserService
    {
        Task<ApiResponse<List<UserDto>>> GetAllAsync(string? search = null, int? status = null);
        Task<ApiResponse<UserDto>> GetByIdAsync(int id);
        Task<ApiResponse<UserDto>> AddAsync(UserDto dto);
        Task<ApiResponse<UserDto>> UpdateAsync(UserDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<bool>> AssignRolesAsync(int userId, List<int> roleIds);
    }
}
