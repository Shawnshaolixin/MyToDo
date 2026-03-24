using MyToDo.Api.Extensions;

namespace MyToDo.Api.Services
{
    public interface IToDoService
    {
        Task<ApiResponse<IList<ToDoDto>>> GetAllAsync(string? search = null, int? status = null);
        Task<ApiResponse<ToDoDto>> GetByIdAsync(int id);
        Task<ApiResponse<ToDoDto>> AddAsync(ToDoDto dto);
        Task<ApiResponse<ToDoDto>> UpdateAsync(ToDoDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<SummaryDto>> GetSummaryAsync();
    }
}
