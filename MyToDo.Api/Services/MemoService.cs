using MyToDo.Api.Entities;
using MyToDo.Api.Extensions;
using MyToDo.Api.Repositories;

namespace MyToDo.Api.Services
{
    public class MemoService : IMemoService
    {
        private readonly IBaseRepository<Memo> _repository;

        public MemoService(IBaseRepository<Memo> repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<IList<MemoDto>>> GetAllAsync(string? search = null)
        {
            try
            {
                var items = await _repository.GetAllAsync();
                if (!string.IsNullOrWhiteSpace(search))
                    items = items.Where(x => (x.Title ?? string.Empty).Contains(search) || (x.Content ?? string.Empty).Contains(search)).ToList();
                return new ApiResponse<IList<MemoDto>>(true, "获取成功", items.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                return new ApiResponse<IList<MemoDto>>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<MemoDto>> GetByIdAsync(int id)
        {
            try
            {
                var item = await _repository.GetAsync(id);
                if (item == null)
                    return new ApiResponse<MemoDto>(false, "数据不存在");
                return new ApiResponse<MemoDto>(true, "获取成功", MapToDto(item));
            }
            catch (Exception ex)
            {
                return new ApiResponse<MemoDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<MemoDto>> AddAsync(MemoDto dto)
        {
            try
            {
                var entity = new Memo
                {
                    Title = dto.Title,
                    Content = dto.Content,
                    Status = dto.Status
                };
                var result = await _repository.AddAsync(entity);
                return new ApiResponse<MemoDto>(true, "添加成功", MapToDto(result));
            }
            catch (Exception ex)
            {
                return new ApiResponse<MemoDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<MemoDto>> UpdateAsync(MemoDto dto)
        {
            try
            {
                var existing = await _repository.GetAsync(dto.Id);
                if (existing == null)
                    return new ApiResponse<MemoDto>(false, "数据不存在");
                existing.Title = dto.Title;
                existing.Content = dto.Content;
                existing.Status = dto.Status;
                var result = await _repository.UpdateAsync(existing);
                return new ApiResponse<MemoDto>(true, "更新成功", MapToDto(result));
            }
            catch (Exception ex)
            {
                return new ApiResponse<MemoDto>(false, ex.Message);
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

        private static MemoDto MapToDto(Memo e) => new()
        {
            Id = e.Id,
            Title = e.Title,
            Content = e.Content,
            Status = e.Status,
            CreateDate = e.CreateDate,
            UpdateDate = e.UpdateDate
        };
    }
}
