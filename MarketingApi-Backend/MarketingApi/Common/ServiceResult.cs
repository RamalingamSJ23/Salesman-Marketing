namespace MarketingApi.Common
{
    /// <summary>
    /// Wrapper for service responses with explicit success status, error messages, and HTTP status codes.
    /// Easy to understand and test for beginners and freshers.
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; } = 200;

        public static ServiceResult Ok() => new() { Success = true, StatusCode = 200 };
        public static ServiceResult BadRequest(string error) => new() { Success = false, ErrorMessage = error, StatusCode = 400 };
        public static ServiceResult NotFound(string error) => new() { Success = false, ErrorMessage = error, StatusCode = 404 };
        public static ServiceResult Conflict(string error) => new() { Success = false, ErrorMessage = error, StatusCode = 409 };
    }

    /// <summary>
    /// Generic wrapper for service responses that return data.
    /// </summary>
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; } = 200;

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data, StatusCode = 200 };
        public static ServiceResult<T> Created(T data) => new() { Success = true, Data = data, StatusCode = 201 };
        public static ServiceResult<T> BadRequest(string error) => new() { Success = false, ErrorMessage = error, StatusCode = 400 };
        public static ServiceResult<T> NotFound(string error) => new() { Success = false, ErrorMessage = error, StatusCode = 404 };
        public static ServiceResult<T> Conflict(string error) => new() { Success = false, ErrorMessage = error, StatusCode = 409 };
    }
}
