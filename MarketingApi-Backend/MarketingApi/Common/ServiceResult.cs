using Microsoft.AspNetCore.Mvc;

namespace MarketingApi.Common;

/// <summary>
/// Standard service operation result without a payload.
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? Field { get; set; }
    public int StatusCode { get; set; } = StatusCodes.Status200OK;

    public static ServiceResult Ok() => new() { Success = true, StatusCode = StatusCodes.Status200OK };

    public static ServiceResult BadRequest(string error, string? field = null, string? code = null) =>
        new() { Success = false, ErrorMessage = error, Field = field, ErrorCode = code, StatusCode = StatusCodes.Status400BadRequest };

    public static ServiceResult NotFound(string error, string? code = null) =>
        new() { Success = false, ErrorMessage = error, ErrorCode = code, StatusCode = StatusCodes.Status404NotFound };

    public static ServiceResult Conflict(string error, string? field = null, string? code = null) =>
        new() { Success = false, ErrorMessage = error, Field = field, ErrorCode = code, StatusCode = StatusCodes.Status409Conflict };
}

/// <summary>
/// Standard service operation result carrying a typed payload.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? Field { get; set; }
    public int StatusCode { get; set; } = StatusCodes.Status200OK;

    public static ServiceResult<T> Ok(T data) =>
        new() { Success = true, Data = data, StatusCode = StatusCodes.Status200OK };

    public static ServiceResult<T> Created(T data) =>
        new() { Success = true, Data = data, StatusCode = StatusCodes.Status201Created };

    public static ServiceResult<T> BadRequest(string error, string? field = null, string? code = null) =>
        new() { Success = false, ErrorMessage = error, Field = field, ErrorCode = code, StatusCode = StatusCodes.Status400BadRequest };

    public static ServiceResult<T> NotFound(string error, string? code = null) =>
        new() { Success = false, ErrorMessage = error, ErrorCode = code, StatusCode = StatusCodes.Status404NotFound };

    public static ServiceResult<T> Conflict(string error, string? field = null, string? code = null) =>
        new() { Success = false, ErrorMessage = error, Field = field, ErrorCode = code, StatusCode = StatusCodes.Status409Conflict };
}

/// <summary>
/// Converts <see cref="ServiceResult"/> and <see cref="ServiceResult{T}"/> into consistent ProblemDetails / ActionResult responses.
/// </summary>
public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult(this ServiceResult result, ControllerBase controller)
    {
        if (result.Success)
        {
            return controller.Ok();
        }

        return BuildProblemResult(controller, result.StatusCode, result.ErrorMessage, result.Field, result.ErrorCode);
    }

    public static IActionResult ToActionResult<T>(
        this ServiceResult<T> result,
        ControllerBase controller,
        string? createdActionName = null,
        Func<T, object?>? routeValuesFactory = null)
    {
        if (!result.Success)
        {
            return BuildProblemResult(controller, result.StatusCode, result.ErrorMessage, result.Field, result.ErrorCode);
        }

        if (result.StatusCode == StatusCodes.Status201Created && createdActionName != null && result.Data != null)
        {
            return controller.CreatedAtAction(createdActionName, routeValuesFactory?.Invoke(result.Data), result.Data);
        }

        return controller.Ok(result.Data);
    }

    private static ObjectResult BuildProblemResult(
        ControllerBase controller,
        int statusCode,
        string? detail,
        string? field,
        string? errorCode)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Validation or Business Rule Error",
                StatusCodes.Status404NotFound => "Resource Not Found",
                StatusCodes.Status409Conflict => "Conflict",
                _ => "Request Failed"
            },
            Detail = detail ?? "The request could not be completed.",
            Instance = controller.HttpContext?.Request.Path.Value
        };

        if (!string.IsNullOrWhiteSpace(field))
        {
            problem.Extensions["field"] = field;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            problem.Extensions["code"] = errorCode;
        }

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
