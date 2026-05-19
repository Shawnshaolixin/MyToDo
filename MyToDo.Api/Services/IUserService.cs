using MyToDo.Api.Extensions;

namespace MyToDo.Api.Services
{
    public interface IUserService
    {
        Task<ApiResponse<IList<UserDto>>> GetAllAsync(string? search = null, int? status = null);
        Task<ApiResponse<UserDto>> GetByIdAsync(int id);
        Task<ApiResponse<UserDto>> AddAsync(UserDto dto);
        Task<ApiResponse<UserDto>> UpdateAsync(UserDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<bool>> AssignRolesAsync(AssignRolesDto dto);
    }
}
