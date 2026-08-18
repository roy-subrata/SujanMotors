using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Middleware;
using AutoPartShop.Api.Services;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Credential endpoints.
///
/// Login carries the strict <c>auth</c> rate-limit policy: Identity lockout caps guesses against
/// a single account, while this caps spraying across many. Refresh and logout use the roomier
/// <c>session</c> policy — their credential is a 256-bit random token rather than a guessable
/// password, and a whole shop behind one IP renews in bursts. Register and change-password are
/// authenticated and stay on the generous global limiter, so an admin provisioning a batch of
/// staff accounts is not throttled.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly AutoPartDbContext _dbContext;
    private readonly IRefreshTokenService _refreshTokens;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        AutoPartDbContext dbContext,
        IRefreshTokenService refreshTokens)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
        _refreshTokens = refreshTokens;
    }

    /// <summary>Best-effort client IP for the refresh-token audit trail.</summary>
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// User login endpoint
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimiting.AuthPolicy)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.Username)
                      ?? await _userManager.FindByEmailAsync(request.Username);

            // One message for every failure mode below, so a caller cannot tell an unknown
            // username from a known one with the wrong password. Distinct wording used to make
            // /login a working account-enumeration oracle.
            const string InvalidCredentials = "Invalid credentials";

            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiError.Unauthorized(InvalidCredentials, Request.Path));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    // 429 rather than 401: the credentials are not the problem, the attempt rate
                    // is, and this response carries a Retry-After the client can act on. Lockout
                    // is the one case that necessarily reveals the account exists — the message
                    // has to tell the user to wait rather than keep guessing.
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    if (lockoutEnd.HasValue)
                    {
                        var retryAfter = (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalSeconds);
                        if (retryAfter > 0)
                            Response.Headers.RetryAfter = retryAfter.ToString();
                    }

                    return StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        ApiError.TooManyRequests("Account is locked after too many failed sign-in attempts. Please try again later.", Request.Path));
                }
                return Unauthorized(ApiError.Unauthorized(InvalidCredentials, Request.Path));
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var token = await GenerateJwtToken(user);
            var (refreshToken, refreshExpiresAt) = await _refreshTokens.IssueAsync(user, ClientIp);
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await GetUserPermissionsAsync(roles.ToList());

            return Ok(new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExpiresAt,
                Username = user.UserName!,
                Email = user.Email!,
                FullName = user.FullName,
                Roles = roles.ToList(),
                Permissions = permissions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>
    /// Register a new user. Admin-only: staff accounts are provisioned by an administrator.
    /// The very first admin is created by the database seeder, not through this endpoint.
    /// Anonymous access here would allow anyone to self-provision an account — and, via
    /// DefaultRole, grant themselves Admin — so it must stay behind an Admin authorization check.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (await _userManager.FindByEmailAsync(request.Email) != null)
            {
                return BadRequest(ApiError.Validation("Email already exists", instance: Request.Path));
            }

            if (await _userManager.FindByNameAsync(request.Username) != null)
            {
                return BadRequest(ApiError.Validation("Username already exists", instance: Request.Path));
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true, // Auto-confirm for now
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Self-Registration"
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(ApiError.Validation(
                    "User creation failed",
                    errors: new Dictionary<string, string[]>
                    {
                        ["password"] = result.Errors.Select(e => e.Description).ToArray()
                    },
                    instance: Request.Path));
            }

            // Assign default role if specified
            if (!string.IsNullOrEmpty(request.DefaultRole))
            {
                if (await _roleManager.RoleExistsAsync(request.DefaultRole))
                {
                    await _userManager.AddToRoleAsync(user, request.DefaultRole);
                }
            }

            return Ok(new
            {
                message = "User registered successfully",
                userId = user.Id,
                username = user.UserName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>
    /// Exchanges a refresh token for a new access token and a new refresh token.
    ///
    /// Anonymous by design — the caller's access token has usually expired by this point, which
    /// is the whole reason for the call. Authority comes from the refresh token itself: it is a
    /// 256-bit random value stored only as a hash, single-use, and bounded by its family's
    /// absolute expiry. Presenting a spent token revokes the entire session (reuse detection).
    /// </summary>
    [HttpPost("refresh-token")]
    [EnableRateLimiting(RateLimiting.SessionPolicy)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _refreshTokens.RotateAsync(request.RefreshToken, ClientIp, cancellationToken);

            if (!result.Succeeded)
            {
                // Deliberately uniform: distinguishing "unknown" from "expired" from
                // "reuse-detected" would let a caller probe the token store.
                _logger.LogInformation("Refresh token rejected: {Reason}", result.FailureReason);
                return Unauthorized(ApiError.Unauthorized("Invalid or expired refresh token", Request.Path));
            }

            var user = result.User!;
            var newToken = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await GetUserPermissionsAsync(roles.ToList());

            return Ok(new RefreshTokenResponse
            {
                Token = newToken,
                RefreshToken = result.RefreshToken!,
                RefreshTokenExpiresAt = result.ExpiresAt!.Value,
                Roles = roles.ToList(),
                Permissions = permissions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>
    /// Ends the session tied to the supplied refresh token.
    ///
    /// Anonymous so that logging out still works once the access token has expired — the refresh
    /// token is the credential being surrendered. Revoking an unknown token is a silent no-op,
    /// so this cannot be used to probe for valid tokens.
    /// </summary>
    [HttpPost("logout")]
    [EnableRateLimiting(RateLimiting.SessionPolicy)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _refreshTokens.RevokeAsync(request.RefreshToken, "logout", cancellationToken);
            return Ok(new { message = "Logged out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // Bind to the authenticated principal — never trust a username from the body.
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(ApiError.Unauthorized("User not found", Request.Path));
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(ApiError.Validation(
                    "Password change failed",
                    errors: new Dictionary<string, string[]>
                    {
                        ["password"] = result.Errors.Select(e => e.Description).ToArray()
                    },
                    instance: Request.Path));
            }

            // A password change must end every existing session — that is the action a user
            // takes when they believe their credentials are compromised. The caller's own
            // access token stays valid until it expires (at most JwtSettings:ExpiryInMinutes),
            // but it can no longer be renewed, so all sessions die within that window.
            await _refreshTokens.RevokeAllForUserAsync(user.Id, "password-change");

            return Ok(new { message = "Password changed successfully. Please sign in again on your other devices." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    private async Task<List<string>> GetUserPermissionsAsync(List<string> roleNames)
    {
        var permissions = new List<string>();

        // Get all role IDs for the user's roles
        var roleIds = await _dbContext.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        // Get all permission names for those roles
        var permissionNames = await _dbContext.Set<RolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_dbContext.Set<Permission>(),
                rp => rp.PermissionId,
                p => p.Id,
                (rp, p) => p.Name)
            .Distinct()
            .ToListAsync();

        return permissionNames;
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
        var issuer = jwtSettings["Issuer"] ?? "AutoPartShopAPI";
        var audience = jwtSettings["Audience"] ?? "AutoPartShopClient";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");

        var userRoles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("fullName", user.FullName)
        };

        // Add role claims
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Add user claims
        claims.AddRange(userClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}

// DTOs
public record LoginRequest
{
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
}

public record LoginResponse
{
    /// <summary>Short-lived access JWT (JwtSettings:ExpiryInMinutes).</summary>
    public string Token { get; init; } = default!;

    /// <summary>
    /// Single-use refresh token. Store it as securely as the access token — it is a
    /// bearer credential for the whole session.
    /// </summary>
    public string RefreshToken { get; init; } = default!;

    /// <summary>Absolute expiry of the session; rotation does not extend it.</summary>
    public DateTime RefreshTokenExpiresAt { get; init; }

    public string Username { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
}

public record RegisterRequest
{
    public string Username { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string? DefaultRole { get; init; }
}

public record RefreshTokenRequest
{
    /// <summary>
    /// The refresh token issued at login or by the previous refresh — NOT the access JWT.
    /// </summary>
    public string RefreshToken { get; init; } = default!;
}

public record RefreshTokenResponse
{
    public string Token { get; init; } = default!;

    /// <summary>The successor token. The one just presented is now spent — replace it.</summary>
    public string RefreshToken { get; init; } = default!;

    public DateTime RefreshTokenExpiresAt { get; init; }

    /// <summary>Re-sent on every refresh so role/permission changes take effect without re-login.</summary>
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
}

public record ChangePasswordRequest
{
    public string Username { get; init; } = default!;
    public string CurrentPassword { get; init; } = default!;
    public string NewPassword { get; init; } = default!;
}
