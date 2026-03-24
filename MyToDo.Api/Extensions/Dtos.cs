namespace MyToDo.Api.Extensions
{
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
