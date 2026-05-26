using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/customer-auth")]
[Produces("application/json")]
public class CustomerAuthController(
    UserManager<ApplicationUser> _userManager,
    SignInManager<ApplicationUser> _signInManager,
    RoleManager<ApplicationRole> _roleManager,
    ICustomerRepository _customerRepository,
    ICodeGenerateService _codeGenerateService,
    AutoPartDbContext _dbContext,
    IConfiguration _configuration,
    ILogger<CustomerAuthController> _logger) : ControllerBase
{
    /// <summary>Register a new online customer account.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CustomerRegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest(new { message = "First name is required" });

            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest(new { message = "Phone number is required" });

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Password is required" });

            var normalizedPhone = request.Phone.Trim();
            var hasRealEmail = !string.IsNullOrWhiteSpace(request.Email);
            // Use placeholder when no email provided — satisfies Identity's unique-email requirement
            var normalizedEmail = hasRealEmail
                ? request.Email!.Trim().ToLower()
                : $"{normalizedPhone}@customer.local";

            // Duplicate phone check (always)
            var phoneExists = await _dbContext.Customers
                .AnyAsync(c => c.Phone == normalizedPhone && !c.Isdeleted, cancellationToken);
            if (phoneExists)
                return Conflict(new { message = "An account with this phone number already exists" });

            // Duplicate email check (only when a real email was provided)
            if (hasRealEmail)
            {
                var emailExists = await _dbContext.Customers
                    .AnyAsync(c => c.Email == normalizedEmail && !c.Isdeleted, cancellationToken);
                if (emailExists)
                    return Conflict(new { message = "An account with this email already exists" });

                if (await _userManager.FindByEmailAsync(normalizedEmail) != null)
                    return Conflict(new { message = "An account with this email already exists" });
            }

            var lastName = string.IsNullOrWhiteSpace(request.LastName) ? "Customer" : request.LastName.Trim();
            var customerCode = await _codeGenerateService.GenerateAsync("CUST", cancellationToken);

            var customer = Customer.Create(
                customerCode,
                request.FirstName.Trim(),
                lastName,
                hasRealEmail ? normalizedEmail : string.Empty,
                normalizedPhone,
                companyName: string.Empty,
                billingAddress: request.Address ?? string.Empty,
                shippingAddress: request.Address ?? string.Empty,
                city: request.City ?? string.Empty,
                state: string.Empty,
                postalCode: string.Empty,
                country: "Bangladesh"
            );
            customer.CreatedBy = "ECOMMERCE";
            customer.ModifiedBy = "ECOMMERCE";

            await _customerRepository.AddAsync(customer, cancellationToken);

            // UserName = phone (unique, always available); Email = real or placeholder
            var user = new ApplicationUser
            {
                UserName = normalizedPhone,
                Email = normalizedEmail,
                PhoneNumber = normalizedPhone,
                FirstName = request.FirstName.Trim(),
                LastName = lastName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "ECOMMERCE",
                CustomerId = customer.Id
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                // Roll back the customer record we just saved
                _dbContext.Customers.Remove(customer);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return BadRequest(new
                {
                    message = "Account creation failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            if (await _roleManager.RoleExistsAsync("Customer"))
                await _userManager.AddToRoleAsync(user, "Customer");

            return Ok(new
            {
                message = "Account created successfully",
                customerId = customer.Id,
                customerCode = customer.CustomerCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>Login with email or phone number.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Identifier))
                return BadRequest(new { message = "Email or phone number is required" });

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Password is required" });

            var identifier = request.Identifier.Trim();

            // Try email first, then phone
            var user = await _userManager.FindByEmailAsync(identifier)
                    ?? await _dbContext.Users
                           .FirstOrDefaultAsync(u => u.PhoneNumber == identifier && u.CustomerId != null, cancellationToken);

            if (user == null || !user.IsActive || user.CustomerId == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    return Unauthorized(new { message = "Account is locked. Please try again later." });
                return Unauthorized(new { message = "Invalid credentials" });
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var customer = await _customerRepository.GetByIdAsync(user.CustomerId.Value, cancellationToken);
            if (customer == null)
                return Unauthorized(new { message = "Customer record not found" });

            var token = GenerateCustomerJwt(user, customer);

            return Ok(new CustomerLoginResponse
            {
                Token = token,
                CustomerId = customer.Id,
                CustomerCode = customer.CustomerCode,
                FullName = customer.GetFullName(),
                Email = customer.Email,
                Phone = customer.Phone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    private string GenerateCustomerJwt(ApplicationUser user, Customer customer)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
        var issuer = jwtSettings["Issuer"] ?? "AutoPartShopAPI";
        var audience = jwtSettings["Audience"] ?? "AutoPartShopClient";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("customerId", customer.Id.ToString()),
            new("customerCode", customer.CustomerCode),
            new("firstName", customer.FirstName),
            new("lastName", customer.LastName),
            new(ClaimTypes.Role, "Customer")
        };

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

public class CustomerRegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
}

public class CustomerLoginRequest
{
    /// <summary>Email address or phone number</summary>
    public string Identifier { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CustomerLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
