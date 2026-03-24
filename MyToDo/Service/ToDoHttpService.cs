using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MyToDo.Common.Models;

namespace MyToDo.Service
{
    public class ToDoHttpService : IToDoService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public ToDoHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse<List<ToDoDto>>> GetAllAsync(string? search = null, int? status = null)
        {
            var url = "api/todo";
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (status.HasValue)
                queryParams.Add($"status={status.Value}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<List<ToDoDto>>>(url, _jsonOptions)
                    ?? new ApiResponse<List<ToDoDto>> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<ToDoDto>> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<ToDoDto>> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<ToDoDto>>($"api/todo/{id}", _jsonOptions)
                    ?? new ApiResponse<ToDoDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ToDoDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<ToDoDto>> AddAsync(ToDoDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/todo", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<ToDoDto>>(_jsonOptions)
                    ?? new ApiResponse<ToDoDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ToDoDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<ToDoDto>> UpdateAsync(ToDoDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/todo", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<ToDoDto>>(_jsonOptions)
                    ?? new ApiResponse<ToDoDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ToDoDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/todo/{id}");
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
                    ?? new ApiResponse<bool> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<SummaryDto>> GetSummaryAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<SummaryDto>>("api/todo/summary", _jsonOptions)
                    ?? new ApiResponse<SummaryDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<SummaryDto> { Status = false, Message = ex.Message };
            }
        }
    }
}
