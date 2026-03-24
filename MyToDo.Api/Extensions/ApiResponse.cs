namespace MyToDo.Api.Extensions
{
    public class ApiResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;

        public ApiResponse(bool status, string message)
        {
            Status = status;
            Message = message;
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Result { get; set; }

        public ApiResponse(bool status, string message, T? result = default)
            : base(status, message)
        {
            Result = result;
        }
    }
}
