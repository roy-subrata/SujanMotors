using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartsShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Data;

/// <summary>
/// Seeds the database on application startup. Idempotent — safe to run on every boot.
/// Creates: Admin role, admin user, and walk-in customer.
/// Admin is treated as superuser bypass (no permission rows needed).
/// </summary>
public class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AutoPartDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
        var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

        try
        {
            // Ensure database schema is up to date
            await context.Database.MigrateAsync();

            // Create Admin role (required for admin user assignment)
            await SeedAdminRoleAsync(roleManager, logger);

            // Create admin user (the only user seeded)
            await SeedAdminUserAsync(userManager, logger, configuration, environment);

            // Reserved walk-in customer for anonymous/cash sales
            await SeedWalkInCustomerAsync(customerRepository, logger);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Creates the Admin role if it doesn't exist.
    /// Admin is treated as superuser bypass in the authorization handler —
    /// no permission rows are needed for Admin.
    /// </summary>
    private static async Task SeedAdminRoleAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        if (await roleManager.RoleExistsAsync("Admin"))
        {
            logger.LogInformation("Admin role already exists, skipping");
            return;
        }

        var adminRole = new ApplicationRole
        {
            Name = "Admin",
            Description = "Full system access with all permissions",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        var result = await roleManager.CreateAsync(adminRole);
        if (result.Succeeded)
        {
            logger.LogInformation("Admin role created successfully");
        }
        else
        {
            logger.LogError("Failed to create Admin role: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Creates the admin user. Password comes from Seed:AdminPassword config.
    /// In Development, falls back to "Admin@1990" if not configured.
    /// In other environments, skips if no password is set (operator must configure it).
    /// </summary>
    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        Microsoft.Extensions.Hosting.IHostEnvironment environment)
    {
        // Check if admin already exists
        if (await userManager.FindByNameAsync("admin") != null)
        {
            logger.LogInformation("Admin user already exists, skipping");
            return;
        }

        // Get admin password from configuration
        var configuredPassword = configuration["Seed:AdminPassword"];
        var adminPassword = !string.IsNullOrWhiteSpace(configuredPassword)
            ? configuredPassword
            : (environment.IsDevelopment() ? "Admin@1990" : null);

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Admin user not seeded: set Seed:AdminPassword (env: Seed__AdminPassword) to bootstrap the first admin.");
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@autopartshop.com",
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Admin user created successfully");

        var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (roleResult.Succeeded)
        {
            logger.LogInformation("Admin role assigned to admin user");
        }
        else
        {
            logger.LogError("Failed to assign Admin role: {Errors}",
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Reserved "Walk-in" customer for anonymous/cash counter sales.
    /// Must never carry a due/credit balance (enforced in SalesOrderController).
    /// </summary>
    private static async Task SeedWalkInCustomerAsync(ICustomerRepository customerRepository, ILogger logger)
    {
        const string walkInCode = "WALKIN";

        var existing = await customerRepository.GetByCodeAsync(walkInCode);
        if (existing != null)
        {
            logger.LogInformation("Walk-in customer already exists");
            return;
        }

        var walkInCustomer = Customer.Create(
            customerCode: walkInCode,
            firstName: "Walk-in",
            lastName: "Customer",
            email: "",
            phone: "0000000000",
            companyName: "",
            billingAddress: "",
            shippingAddress: "",
            city: "",
            state: "",
            postalCode: "",
            country: "",
            customerType: "RETAIL");

        walkInCustomer.CreatedBy = "System";
        walkInCustomer.ModifiedBy = "System";

        await customerRepository.AddAsync(walkInCustomer);

        logger.LogInformation("Walk-in customer seeded successfully");
    }
}
