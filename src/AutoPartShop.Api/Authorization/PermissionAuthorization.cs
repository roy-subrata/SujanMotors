using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AutoPartShop.Api.Authorization;

/// <summary>
/// Gates an endpoint behind one of the seeded permission names (see <see cref="Permissions"/>).
/// Admins bypass permission checks entirely; other roles need the permission assigned via
/// the admin panel's role-permission management (RolePermissions table).
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "permission:";

    public HasPermissionAttribute(string permission) => Policy = $"{PolicyPrefix}{permission}";
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

/// <summary>
/// Builds "permission:xxx" policies on demand so every permission name doesn't have to be
/// registered up-front; everything else falls through to the default provider.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}

/// <summary>
/// Resolves the user's permissions from their roles (RolePermissions table), cached per
/// role for 60s so permission changes in the admin panel apply within a minute without
/// a database hit on every request. The Admin role is a superuser and always passes.
/// </summary>
public sealed class PermissionAuthorizationHandler(
    AutoPartDbContext dbContext,
    IMemoryCache cache) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct();

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
                    .ToHashSetAsync();
            });

            if (permissions is not null && permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
