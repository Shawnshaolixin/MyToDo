namespace MyToDo.Api.Entities
{
    public class Memo : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Status { get; set; }
    }
}
