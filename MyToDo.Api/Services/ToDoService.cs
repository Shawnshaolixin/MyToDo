using MyToDo.Api.Entities;
using MyToDo.Api.Extensions;
using MyToDo.Api.Repositories;
using MyToDo.Api.Services;

namespace MyToDo.Api.Services
{
    public class ToDoService : IToDoService
    {
        private readonly IBaseRepository<ToDo> _repository;
        private readonly IBaseRepository<Memo> _memoRepository;

        public ToDoService(IBaseRepository<ToDo> repository, IBaseRepository<Memo> memoRepository)
        {
            _repository = repository;
            _memoRepository = memoRepository;
        }

        public async Task<ApiResponse<IList<ToDoDto>>> GetAllAsync(string? search = null, int? status = null)
        {
            try
            {
                var items = await _repository.GetAllAsync();
                if (!string.IsNullOrWhiteSpace(search))
                    items = items.Where(x => (x.Title ?? string.Empty).Contains(search) || (x.Content ?? string.Empty).Contains(search)).ToList();
                if (status.HasValue)
                    items = items.Where(x => x.Status == status.Value).ToList();
                return new ApiResponse<IList<ToDoDto>>(true, "获取成功", items.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                return new ApiResponse<IList<ToDoDto>>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<ToDoDto>> GetByIdAsync(int id)
        {
            try
            {
                var item = await _repository.GetAsync(id);
                if (item == null)
                    return new ApiResponse<ToDoDto>(false, "数据不存在");
                return new ApiResponse<ToDoDto>(true, "获取成功", MapToDto(item));
            }
            catch (Exception ex)
            {
                return new ApiResponse<ToDoDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<ToDoDto>> AddAsync(ToDoDto dto)
        {
            try
            {
                var entity = MapToEntity(dto);
                var result = await _repository.AddAsync(entity);
                return new ApiResponse<ToDoDto>(true, "添加成功", MapToDto(result));
            }
            catch (Exception ex)
            {
                return new ApiResponse<ToDoDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<ToDoDto>> UpdateAsync(ToDoDto dto)
        {
            try
            {
                var existing = await _repository.GetAsync(dto.Id);
                if (existing == null)
                    return new ApiResponse<ToDoDto>(false, "数据不存在");
                existing.Title = dto.Title;
                existing.Content = dto.Content;
                existing.Status = dto.Status;
                var result = await _repository.UpdateAsync(existing);
                return new ApiResponse<ToDoDto>(true, "更新成功", MapToDto(result));
            }
            catch (Exception ex)
            {
                return new ApiResponse<ToDoDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                await _repository.DeleteAsync(id);
                return new ApiResponse<bool>(true, "删除成功", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }

        public async Task<ApiResponse<SummaryDto>> GetSummaryAsync()
        {
            try
            {
                var todos = await _repository.GetAllAsync();
                var memos = await _memoRepository.GetAllAsync();
                int total = todos.Count;
                int completed = todos.Count(x => x.Status == 1);
                double ratio = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;
                var summary = new SummaryDto
                {
                    ToDoCount = total - completed,
                    CompletedCount = completed,
                    CompletedRatio = ratio,
                    MemoCount = memos.Count,
                    ToDoList = todos.Where(x => x.Status == 0).Take(6).Select(MapToDto).ToList(),
                    MemoList = memos.Take(6).Select(m => new MemoDto
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Content = m.Content,
                        Status = m.Status,
                        CreateDate = m.CreateDate,
                        UpdateDate = m.UpdateDate
                    }).ToList()
                };
                return new ApiResponse<SummaryDto>(true, "获取成功", summary);
            }
            catch (Exception ex)
            {
                return new ApiResponse<SummaryDto>(false, ex.Message);
            }
        }

        private static ToDoDto MapToDto(ToDo e) => new()
        {
            Id = e.Id,
            Title = e.Title,
            Content = e.Content,
            Status = e.Status,
            CreateDate = e.CreateDate,
            UpdateDate = e.UpdateDate
        };

        private static ToDo MapToEntity(ToDoDto dto) => new()
        {
            Title = dto.Title,
            Content = dto.Content,
            Status = dto.Status
        };
    }
}
