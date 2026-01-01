namespace S3WebApi.Models.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public ApiResponse() { }

        public ApiResponse(T data, bool success = true, string message = null)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public static ApiResponse<T> Fail(string message)
        {
            return new ApiResponse<T>(default, false, message);
        }
    }
}