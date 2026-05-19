using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities;
using MyToDo.Api.Extensions;

namespace MyToDo.Api.Services
{
    public class UserService : IUserService
    {
        private readonly MyToDoContext _context;

        public UserService(MyToDoContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<IList<UserDto>>> GetAllAsync(string? search = null, int? status = null)
        {
            try
            {
                var query = _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(u => u.UserName.Contains(search) || u.Email.Contains(search));
                if (status.HasValue)
                    query = query.Where(u => u.Status == status.Value);
                var items = await query.ToListAsync();
                return new ApiResponse<IList<UserDto>>(true, "获取成功", items.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                return new ApiResponse<IList<UserDto>>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<UserDto>> GetByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                                               .FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return new ApiResponse<UserDto>(false, "用户不存在");
                return new ApiResponse<UserDto>(true, "获取成功", MapToDto(user));
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<UserDto>> AddAsync(UserDto dto)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
                    return new ApiResponse<UserDto>(false, "用户名已存在");

                var user = new User
                {
                    UserName = dto.UserName,
                    Password = HashPassword(dto.Password),
                    Email = dto.Email,
                    Status = dto.Status,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return new ApiResponse<UserDto>(true, "添加成功", MapToDto(user));
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateAsync(UserDto dto)
        {
            try
            {
                var user = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                                               .FirstOrDefaultAsync(u => u.Id == dto.Id);
                if (user == null)
                    return new ApiResponse<UserDto>(false, "用户不存在");

                if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName && u.Id != dto.Id))
                    return new ApiResponse<UserDto>(false, "用户名已存在");

                user.UserName = dto.UserName;
                user.Email = dto.Email;
                user.Status = dto.Status;
                if (!string.IsNullOrWhiteSpace(dto.Password))
                    user.Password = HashPassword(dto.Password);
                user.UpdateDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return new ApiResponse<UserDto>(true, "更新成功", MapToDto(user));
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return new ApiResponse<bool>(false, "用户不存在", false);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return new ApiResponse<bool>(true, "删除成功", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }

        public async Task<ApiResponse<bool>> AssignRolesAsync(AssignRolesDto dto)
        {
            try
            {
                var user = await _context.Users.Include(u => u.UserRoles)
                                               .FirstOrDefaultAsync(u => u.Id == dto.UserId);
                if (user == null)
                    return new ApiResponse<bool>(false, "用户不存在", false);

                _context.UserRoles.RemoveRange(user.UserRoles);
                foreach (var roleId in dto.RoleIds)
                    _context.UserRoles.Add(new UserRole { UserId = dto.UserId, RoleId = roleId });

                await _context.SaveChangesAsync();
                return new ApiResponse<bool>(true, "角色分配成功", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }

        private static UserDto MapToDto(User u) => new()
        {
            Id = u.Id,
            UserName = u.UserName,
            Password = string.Empty,
            Email = u.Email,
            Status = u.Status,
            CreateDate = u.CreateDate,
            UpdateDate = u.UpdateDate,
            RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList(),
            RoleNames = u.UserRoles.Select(ur => ur.Role?.Name ?? string.Empty).ToList()
        };

        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
