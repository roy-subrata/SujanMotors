using System.Security.Claims;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Programmatic permission checks for use inside controller/service logic (as opposed to the
/// declarative <c>[HasPermission(...)]</c> attribute gate on an entire action). Shares the same
/// resolution path — and the same 60s role-permission cache — as
/// <see cref="AutoPartShop.Api.Authorization.PermissionAuthorizationHandler"/>.
/// </summary>
public interface IPermissionCheckService
{
    /// <summary>
    /// True if any role held by <paramref name="user"/> grants <paramref name="permission"/>.
    /// The Admin role always passes (superuser), matching the attribute-based check.
    /// </summary>
    Task<bool> UserHasPermissionAsync(ClaimsPrincipal user, string permission, CancellationToken cancellationToken = default);
}
