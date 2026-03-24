using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MyToDo.Common.Models;

namespace MyToDo.Service
{
    public class MemoHttpService : IMemoService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public MemoHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse<List<MemoDto>>> GetAllAsync(string? search = null)
        {
            var url = "api/memo";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"?search={Uri.EscapeDataString(search)}";
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<List<MemoDto>>>(url, _jsonOptions)
                    ?? new ApiResponse<List<MemoDto>> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<MemoDto>> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<MemoDto>> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<MemoDto>>($"api/memo/{id}", _jsonOptions)
                    ?? new ApiResponse<MemoDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<MemoDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<MemoDto>> AddAsync(MemoDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/memo", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<MemoDto>>(_jsonOptions)
                    ?? new ApiResponse<MemoDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<MemoDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<MemoDto>> UpdateAsync(MemoDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/memo", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<MemoDto>>(_jsonOptions)
                    ?? new ApiResponse<MemoDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<MemoDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/memo/{id}");
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
                    ?? new ApiResponse<bool> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Status = false, Message = ex.Message };
            }
        }
    }
}
