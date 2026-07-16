using System.Security.Claims;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for accessing current authenticated user information from HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the username of the currently authenticated user
    /// </summary>
    /// <returns>Username or "System" if not authenticated</returns>
    public string GetCurrentUsername()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
    }

    /// <summary>
    /// Gets the user ID of the currently authenticated user from claims
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Gets the user ID of the currently authenticated user as a Guid, for FK attribution.
    /// </summary>
    /// <returns>User Guid, or null if not authenticated or the claim isn't a valid Guid.</returns>
    public Guid? GetCurrentUserGuid()
    {
        return Guid.TryParse(GetCurrentUserId(), out var id) ? id : null;
    }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    /// <returns>True if authenticated, false otherwise</returns>
    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
