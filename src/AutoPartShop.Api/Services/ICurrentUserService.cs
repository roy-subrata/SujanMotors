namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for accessing current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the username of the currently authenticated user
    /// </summary>
    /// <returns>Username or "System" if not authenticated</returns>
    string GetCurrentUsername();

    /// <summary>
    /// Gets the user ID of the currently authenticated user
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    string? GetCurrentUserId();

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    /// <returns>True if authenticated, false otherwise</returns>
    bool IsAuthenticated();
}
