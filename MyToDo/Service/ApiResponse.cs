using MyToDo.Common.Models;

namespace MyToDo.Service
{
    public class ApiResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Result { get; set; }
    }

    public class SummaryDto
    {
        public int ToDoCount { get; set; }
        public int CompletedCount { get; set; }
        public double CompletedRatio { get; set; }
        public int MemoCount { get; set; }
        public List<ToDoDto> ToDoList { get; set; } = new();
        public List<MemoDto> MemoList { get; set; } = new();
    }
}
