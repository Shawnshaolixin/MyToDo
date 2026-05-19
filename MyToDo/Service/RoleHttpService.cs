using MyToDo.Common.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyToDo.Service
{
    public class RoleHttpService : IRoleService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public RoleHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse<List<RoleDto>>> GetAllAsync(string? search = null)
        {
            var url = "api/role";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"?search={Uri.EscapeDataString(search)}";
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<List<RoleDto>>>(url, _jsonOptions)
                    ?? new ApiResponse<List<RoleDto>> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<RoleDto>> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<RoleDto>> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<RoleDto>>($"api/role/{id}", _jsonOptions)
                    ?? new ApiResponse<RoleDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<RoleDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<RoleDto>> AddAsync(RoleDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/role", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<RoleDto>>(_jsonOptions)
                    ?? new ApiResponse<RoleDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<RoleDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<RoleDto>> UpdateAsync(RoleDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/role", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<RoleDto>>(_jsonOptions)
                    ?? new ApiResponse<RoleDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<RoleDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/role/{id}");
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
                    ?? new ApiResponse<bool> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<bool>> AssignPermissionsAsync(int roleId, List<int> permissionIds)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/role/assign-permissions",
                    new { roleId, permissionIds }, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
                    ?? new ApiResponse<bool> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<List<PermissionDto>>> GetAllPermissionsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResponse<List<PermissionDto>>>("api/role/permissions", _jsonOptions)
                    ?? new ApiResponse<List<PermissionDto>> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PermissionDto>> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<PermissionDto>> AddPermissionAsync(PermissionDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/role/permissions", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<PermissionDto>>(_jsonOptions)
                    ?? new ApiResponse<PermissionDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PermissionDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<PermissionDto>> UpdatePermissionAsync(PermissionDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/role/permissions", dto, _jsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiResponse<PermissionDto>>(_jsonOptions)
                    ?? new ApiResponse<PermissionDto> { Status = false, Message = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PermissionDto> { Status = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<bool>> DeletePermissionAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/role/permissions/{id}");
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
