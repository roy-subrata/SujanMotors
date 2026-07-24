using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoPartShop.Api.Common;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AutoPartShop.Api.Controllers;

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

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        AutoPartDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// User login endpoint
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.Username)
                      ?? await _userManager.FindByEmailAsync(request.Username);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiError.Unauthorized("Invalid credentials or account is inactive", Request.Path));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return Unauthorized(ApiError.Unauthorized("Account is locked. Please try again later.", Request.Path));
                }
                return Unauthorized(ApiError.Unauthorized("Invalid credentials", Request.Path));
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await GetUserPermissionsAsync(roles.ToList());

            return Ok(new LoginResponse
            {
                Token = token,
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
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
            {
                return Unauthorized(ApiError.Unauthorized("Invalid token", Request.Path));
            }

            var username = principal.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(ApiError.Unauthorized("Invalid token", Request.Path));
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiError.Unauthorized("User not found or inactive", Request.Path));
            }

            var newToken = await GenerateJwtToken(user);

            return Ok(new { token = newToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
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

            return Ok(new { message = "Password changed successfully" });
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

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false // Don't validate lifetime for refresh
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
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
    public string Token { get; init; } = default!;
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
    public string Token { get; init; } = default!;
}

public record ChangePasswordRequest
{
    public string Username { get; init; } = default!;
    public string CurrentPassword { get; init; } = default!;
    public string NewPassword { get; init; } = default!;
}
