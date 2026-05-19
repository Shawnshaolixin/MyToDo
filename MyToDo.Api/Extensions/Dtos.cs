namespace MyToDo.Api.Extensions
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Status { get; set; } = 1;
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public List<int> RoleIds { get; set; } = new();
        public List<string> RoleNames { get; set; } = new();
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public List<int> PermissionIds { get; set; } = new();
        public List<string> PermissionNames { get; set; } = new();
    }

    public class PermissionDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
    }

    public class AssignRolesDto
    {
        public int UserId { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }

    public class AssignPermissionsDto
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new();
    }

    public class ToDoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
    }

    public class MemoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
    }

    public class SummaryDto
    {
        public int ToDoCount { get; set; }
        public int CompletedCount { get; set; }
        public double CompletedRatio { get; set; }
        public int MemoCount { get; set; }
        public IList<ToDoDto> ToDoList { get; set; } = new List<ToDoDto>();
        public IList<MemoDto> MemoList { get; set; } = new List<MemoDto>();
    }
}
