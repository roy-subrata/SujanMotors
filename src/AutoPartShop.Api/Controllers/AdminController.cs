using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AutoPartDbContext dbContext,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    #region User Management

    /// <summary>
    /// Get all users with their roles
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt
                })
                .ToListAsync();

            var usersWithRoles = new List<object>();
            foreach (var user in users)
            {
                var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
                var roles = await _userManager.GetRolesAsync(appUser!);

                usersWithRoles.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.IsActive,
                    user.CreatedAt,
                    user.LastLoginAt,
                    Roles = roles
                });
            }

            return Ok(usersWithRoles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                Roles = roles,
                Claims = claims.Select(c => new { c.Type, c.Value })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Create a new user (Admin function)
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (await _userManager.FindByEmailAsync(request.Email) != null)
            {
                return BadRequest(new { message = "Email already exists" });
            }

            if (await _userManager.FindByNameAsync(request.Username) != null)
            {
                return BadRequest(new { message = "Username already exists" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "Admin"
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "User creation failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            // Assign roles
            if (request.Roles != null && request.Roles.Any())
            {
                var validRoles = new List<string>();
                foreach (var role in request.Roles)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        validRoles.Add(role);
                    }
                }

                if (validRoles.Any())
                {
                    await _userManager.AddToRolesAsync(user, validRoles);
                }
            }

            return Ok(new
            {
                message = "User created successfully",
                userId = user.Id,
                username = user.UserName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update user information
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.IsActive = request.IsActive;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = User.Identity?.Name ?? "Admin";

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "User update failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new { message = "User updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Deactivate/Activate a user
    /// </summary>
    [HttpPatch("users/{id}/toggle-status")]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.IsActive = !user.IsActive;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = User.Identity?.Name ?? "Admin";

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully",
                isActive = user.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Password reset failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    #endregion

    #region Role Management

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        try
        {
            var roles = await _roleManager.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.IsActive,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            if (await _roleManager.RoleExistsAsync(request.Name))
            {
                return BadRequest(new { message = "Role already exists" });
            }

            var role = new ApplicationRole
            {
                Name = request.Name,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "Admin"
            };

            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Role creation failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                message = "Role created successfully",
                roleId = role.Id,
                roleName = role.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("roles/{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            role.Name = request.Name;
            role.Description = request.Description;
            role.IsActive = request.IsActive;
            role.ModifiedAt = DateTime.UtcNow;
            role.ModifiedBy = User.Identity?.Name ?? "Admin";

            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Role update failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new { message = "Role updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("roles/{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            // Check if any users have this role
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Any())
            {
                return BadRequest(new { message = $"Cannot delete role. {usersInRole.Count} user(s) are assigned to this role." });
            }

            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Role deletion failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new { message = "Role deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    #endregion

    #region User Role Assignment

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    [HttpPost("users/{userId}/roles")]
    public async Task<IActionResult> AssignRolesToUser(Guid userId, [FromBody] AssignRolesRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = request.Roles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(request.Roles).ToList();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            if (rolesToAdd.Any())
            {
                var validRoles = new List<string>();
                foreach (var role in rolesToAdd)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        validRoles.Add(role);
                    }
                }

                if (validRoles.Any())
                {
                    await _userManager.AddToRolesAsync(user, validRoles);
                }
            }

            return Ok(new { message = "Roles assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning roles");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get roles for a specific user
    /// </summary>
    [HttpGet("users/{userId}/roles")]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    #endregion

    #region Permission Management

    /// <summary>
    /// Get all permissions
    /// </summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> GetAllPermissions()
    {
        try
        {
            var permissions = await _dbContext.Permissions
                .Where(p => !p.Isdeleted)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.DisplayName,
                    p.Description,
                    p.Category,
                    p.IsActive
                })
                .ToListAsync();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Create a new permission
    /// </summary>
    [HttpPost("permissions")]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        try
        {
            var existingPermission = await _dbContext.Permissions
                .FirstOrDefaultAsync(p => p.Name == request.Name);

            if (existingPermission != null)
            {
                return BadRequest(new { message = "Permission already exists" });
            }

            var permission = Permission.Create(request.Name, request.DisplayName, request.Category, request.Description);
            permission.CreatedBy = User.Identity?.Name ?? "Admin";
            permission.ModifiedBy = User.Identity?.Name ?? "Admin";

            await _dbContext.Permissions.AddAsync(permission);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Permission created successfully",
                permissionId = permission.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Assign permissions to a role
    /// </summary>
    [HttpPost("roles/{roleId}/permissions")]
    public async Task<IActionResult> AssignPermissionsToRole(Guid roleId, [FromBody] AssignPermissionsRequest request)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            // Remove existing permissions
            var existingPermissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _dbContext.RolePermissions.RemoveRange(existingPermissions);

            // Add new permissions
            foreach (var permissionId in request.PermissionIds)
            {
                var permission = await _dbContext.Permissions.FindAsync(permissionId);
                if (permission != null && permission.IsActive)
                {
                    var rolePermission = RolePermission.Create(roleId, permissionId, User.Identity?.Name ?? "Admin");
                    await _dbContext.RolePermissions.AddAsync(rolePermission);
                }
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Permissions assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permissions");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get permissions for a specific role
    /// </summary>
    [HttpGet("roles/{roleId}/permissions")]
    public async Task<IActionResult> GetRolePermissions(Guid roleId)
    {
        try
        {
            var permissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .Select(rp => new
                {
                    rp.Permission.Id,
                    rp.Permission.Name,
                    rp.Permission.DisplayName,
                    rp.Permission.Category,
                    rp.Permission.Description
                })
                .ToListAsync();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    #endregion
}

// DTOs
public record CreateUserRequest
{
    public string Username { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public bool IsActive { get; init; } = true;
    public List<string>? Roles { get; init; }
}

public record UpdateUserRequest
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public bool IsActive { get; init; }
}

public record ResetPasswordRequest
{
    public string NewPassword { get; init; } = default!;
}

public record CreateRoleRequest
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
}

public record UpdateRoleRequest
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public record AssignRolesRequest
{
    public List<string> Roles { get; init; } = new();
}

public record CreatePermissionRequest
{
    public string Name { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string? Description { get; init; }
}

public record AssignPermissionsRequest
{
    public List<Guid> PermissionIds { get; init; } = new();
}
