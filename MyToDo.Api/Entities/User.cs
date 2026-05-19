namespace MyToDo.Api.Entities
{
    public class User : BaseEntity
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Status { get; set; } = 1;

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
