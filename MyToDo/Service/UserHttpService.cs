using MyToDo.Common.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyToDo.Service
{
    public class UserHttpService : IUserService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public UserHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse<List<UserDto>>> GetAllAsync(string? search = null, int? status = null)
        {
            var url = "api/user";
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (status.HasValue)
                queryParams.Add($"status={status.Value}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<List<UserDto>>>(url, _jsonOptions)
                    ?? new ApiResponse<List<UserDto>> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<UserDto>> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<UserDto>> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<UserDto>>($"api/user/{id}", _jsonOptions)
                    ?? new ApiResponse<UserDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<UserDto>> AddAsync(UserDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/user", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(_jsonOptions)
                    ?? new ApiResponse<UserDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateAsync(UserDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/user", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(_jsonOptions)
                    ?? new ApiResponse<UserDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/user/{id}");
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
                    ?? new ApiResponse<bool> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<bool>> AssignRolesAsync(int userId, List<int> roleIds)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/user/assign-roles",
                    new { userId, roleIds }, _jsonOptions);
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
