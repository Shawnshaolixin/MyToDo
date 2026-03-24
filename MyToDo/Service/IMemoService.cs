using MyToDo.Common.Models;

namespace MyToDo.Service
{
    public interface IMemoService
    {
        Task<ApiResponse<List<MemoDto>>> GetAllAsync(string? search = null);
        Task<ApiResponse<MemoDto>> GetByIdAsync(int id);
        Task<ApiResponse<MemoDto>> AddAsync(MemoDto dto);
        Task<ApiResponse<MemoDto>> UpdateAsync(MemoDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
