using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities;
using MyToDo.Api.Extensions;

namespace MyToDo.Api.Services
{
    public class RoleService : IRoleService
    {
        private readonly MyToDoContext _context;

        public RoleService(MyToDoContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<IList<RoleDto>>> GetAllAsync(string? search = null)
        {
            try
            {
                var query = _context.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission).AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(r => r.Name.Contains(search) || r.Description.Contains(search));
                var items = await query.ToListAsync();
                return new ApiResponse<IList<RoleDto>>(true, "获取成功", items.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                return new ApiResponse<IList<RoleDto>>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<RoleDto>> GetByIdAsync(int id)
        {
            try
            {
                var role = await _context.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                                               .FirstOrDefaultAsync(r => r.Id == id);
                if (role == null)
                    return new ApiResponse<RoleDto>(false, "角色不存在");
                return new ApiResponse<RoleDto>(true, "获取成功", MapToDto(role));
            }
            catch (Exception ex)
            {
                return new ApiResponse<RoleDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<RoleDto>> AddAsync(RoleDto dto)
        {
            try
            {
                if (await _context.Roles.AnyAsync(r => r.Name == dto.Name))
                    return new ApiResponse<RoleDto>(false, "角色名称已存在");

                var role = new Role
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                return new ApiResponse<RoleDto>(true, "添加成功", MapToDto(role));
            }
            catch (Exception ex)
            {
                return new ApiResponse<RoleDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<RoleDto>> UpdateAsync(RoleDto dto)
        {
            try
            {
                var role = await _context.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                                               .FirstOrDefaultAsync(r => r.Id == dto.Id);
                if (role == null)
                    return new ApiResponse<RoleDto>(false, "角色不存在");

                if (await _context.Roles.AnyAsync(r => r.Name == dto.Name && r.Id != dto.Id))
                    return new ApiResponse<RoleDto>(false, "角色名称已存在");

                role.Name = dto.Name;
                role.Description = dto.Description;
                role.UpdateDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return new ApiResponse<RoleDto>(true, "更新成功", MapToDto(role));
            }
            catch (Exception ex)
            {
                return new ApiResponse<RoleDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
                if (role == null)
                    return new ApiResponse<bool>(false, "角色不存在", false);
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                return new ApiResponse<bool>(true, "删除成功", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }

        public async Task<ApiResponse<bool>> AssignPermissionsAsync(AssignPermissionsDto dto)
        {
            try
            {
                var role = await _context.Roles.Include(r => r.RolePermissions)
                                               .FirstOrDefaultAsync(r => r.Id == dto.RoleId);
                if (role == null)
                    return new ApiResponse<bool>(false, "角色不存在", false);

                _context.RolePermissions.RemoveRange(role.RolePermissions);
                foreach (var permId in dto.PermissionIds)
                    _context.RolePermissions.Add(new RolePermission { RoleId = dto.RoleId, PermissionId = permId });

                await _context.SaveChangesAsync();
                return new ApiResponse<bool>(true, "权限分配成功", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }

        public async Task<ApiResponse<IList<PermissionDto>>> GetAllPermissionsAsync()
        {
            try
            {
                var items = await _context.Permissions.ToListAsync();
                return new ApiResponse<IList<PermissionDto>>(true, "获取成功", items.Select(MapPermissionToDto).ToList());
            }
            catch (Exception ex)
            {
                return new ApiResponse<IList<PermissionDto>>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<PermissionDto>> AddPermissionAsync(PermissionDto dto)
        {
            try
            {
                if (await _context.Permissions.AnyAsync(p => p.Code == dto.Code))
                    return new ApiResponse<PermissionDto>(false, "权限代码已存在");

                var perm = new Permission
                {
                    Code = dto.Code,
                    Name = dto.Name,
                    Description = dto.Description,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                };
                _context.Permissions.Add(perm);
                await _context.SaveChangesAsync();
                return new ApiResponse<PermissionDto>(true, "添加成功", MapPermissionToDto(perm));
            }
            catch (Exception ex)
            {
                return new ApiResponse<PermissionDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<PermissionDto>> UpdatePermissionAsync(PermissionDto dto)
        {
            try
            {
                var perm = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == dto.Id);
                if (perm == null)
                    return new ApiResponse<PermissionDto>(false, "权限不存在");

                if (await _context.Permissions.AnyAsync(p => p.Code == dto.Code && p.Id != dto.Id))
                    return new ApiResponse<PermissionDto>(false, "权限代码已存在");

                perm.Code = dto.Code;
                perm.Name = dto.Name;
                perm.Description = dto.Description;
                perm.UpdateDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return new ApiResponse<PermissionDto>(true, "更新成功", MapPermissionToDto(perm));
            }
            catch (Exception ex)
            {
                return new ApiResponse<PermissionDto>(false, ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeletePermissionAsync(int id)
        {
            try
            {
                var perm = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == id);
                if (perm == null)
                    return new ApiResponse<bool>(false, "权限不存在", false);
                _context.Permissions.Remove(perm);
                await _context.SaveChangesAsync();
                return new ApiResponse<bool>(true, "删除成功", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }

        private static RoleDto MapToDto(Role r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            CreateDate = r.CreateDate,
            UpdateDate = r.UpdateDate,
            PermissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList(),
            PermissionNames = r.RolePermissions.Select(rp => rp.Permission?.Name ?? string.Empty).ToList()
        };

        private static PermissionDto MapPermissionToDto(Permission p) => new()
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            CreateDate = p.CreateDate,
            UpdateDate = p.UpdateDate
        };
    }
}
