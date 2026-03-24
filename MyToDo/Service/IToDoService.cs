using MyToDo.Common.Models;

namespace MyToDo.Service
{
    public interface IToDoService
    {
        Task<ApiResponse<List<ToDoDto>>> GetAllAsync(string? search = null, int? status = null);
        Task<ApiResponse<ToDoDto>> GetByIdAsync(int id);
        Task<ApiResponse<ToDoDto>> AddAsync(ToDoDto dto);
        Task<ApiResponse<ToDoDto>> UpdateAsync(ToDoDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<SummaryDto>> GetSummaryAsync();
    }
}
