using System.Security.Claims;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Resolves permissions from the user's roles (RolePermissions table), cached per role for 60s —
/// the same cache entries and query shape as <see cref="AutoPartShop.Api.Authorization.PermissionAuthorizationHandler"/>,
/// so there's a single source of truth for "does role X have permission Y".
/// </summary>
public sealed class PermissionCheckService(
    AutoPartDbContext dbContext,
    IMemoryCache cache) : IPermissionCheckService
{
    public async Task<bool> UserHasPermissionAsync(ClaimsPrincipal user, string permission, CancellationToken cancellationToken = default)
    {
        if (user.IsInRole("Admin"))
            return true;

        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct();

        foreach (var role in roles)
        {
            var permissions = await cache.GetOrCreateAsync($"role-perms:{role}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                return await (
                    from rp in dbContext.RolePermissions
                    join r in dbContext.Roles on rp.RoleId equals r.Id
                    join p in dbContext.Permissions on rp.PermissionId equals p.Id
                    where r.Name == role && p.IsActive && !p.Isdeleted
                    select p.Name)
                    .ToHashSetAsync(cancellationToken);
            });

            if (permissions is not null && permissions.Contains(permission))
                return true;
        }

        return false;
    }
}
