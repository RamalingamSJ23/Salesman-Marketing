using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MarketingApi.Common;

/// <summary>
/// Translates unhandled domain, concurrency, and database constraint exceptions into RFC 7807 ProblemDetails responses.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var (statusCode, title, detail, field, code) = MapException(exception);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path.Value
        };

        if (!string.IsNullOrWhiteSpace(field))
        {
            problem.Extensions["field"] = field;
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            problem.Extensions["code"] = code;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title, string Detail, string? Field, string? Code) MapException(Exception exception)
    {
        switch (exception)
        {
            case KeyNotFoundException notFound:
                return (
                    StatusCodes.Status404NotFound,
                    "Resource Not Found",
                    notFound.Message,
                    null,
                    "NOT_FOUND");

            case DbUpdateConcurrencyException:
                return (
                    StatusCodes.Status409Conflict,
                    "Concurrency Conflict",
                    "The record was modified or processed by another user. Please refresh and try again.",
                    null,
                    "CONCURRENCY_CONFLICT");

            case DbUpdateException dbUpdate when dbUpdate.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation:
                return MapUniqueViolation(pg.ConstraintName);

            default:
                return (
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred while processing your request.",
                    null,
                    "INTERNAL_ERROR");
        }
    }

    private static (int StatusCode, string Title, string Detail, string? Field, string? Code) MapUniqueViolation(string? constraintName)
    {
        var name = constraintName?.ToLowerInvariant() ?? string.Empty;

        if (name.Contains("salesmancode"))
        {
            return (StatusCodes.Status409Conflict, "Duplicate Salesman Code", "Salesman code already exists.", "salesmancode", "DUPLICATE_SALESMAN_CODE");
        }

        if (name.Contains("mobileno"))
        {
            return (StatusCodes.Status409Conflict, "Duplicate Mobile Number", "Mobile number already exists.", "mobileno", "DUPLICATE_MOBILE");
        }

        if (name.Contains("email"))
        {
            return (StatusCodes.Status409Conflict, "Duplicate Email", "Email address already exists.", "email", "DUPLICATE_EMAIL");
        }

        if (name.Contains("teamcode"))
        {
            return (StatusCodes.Status409Conflict, "Duplicate Team Code", "Team code already exists.", "teamcode", "DUPLICATE_TEAM_CODE");
        }

        if (name.Contains("active_salesman") || name.Contains("marketingteamdetail"))
        {
            return (StatusCodes.Status409Conflict, "Duplicate Active Team Membership", "One or more salesmen are already active in another marketing team.", "members", "DUPLICATE_ACTIVE_MEMBERSHIP");
        }

        return (StatusCodes.Status409Conflict, "Conflict", "A record with the same unique value already exists.", null, "DUPLICATE_RECORD");
    }
}
