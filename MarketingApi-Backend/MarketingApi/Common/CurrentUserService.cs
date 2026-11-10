using System.Security.Claims;

namespace MarketingApi.Common;

/// <summary>
/// Provides the authenticated user identity for audit tracking and business operations.
/// </summary>
public interface ICurrentUserService
{
    string? Username { get; }
    string GetRequiredUsername();
}

/// <summary>
/// Reads the authenticated user's username from the current HTTP context claims.
/// </summary>
public sealed class HttpCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Username
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirstValue(ClaimTypes.Name) ?? user.Identity?.Name;
        }
    }

    public string GetRequiredUsername()
    {
        return Username ?? throw new InvalidOperationException("Authenticated username claim is missing.");
    }
}
