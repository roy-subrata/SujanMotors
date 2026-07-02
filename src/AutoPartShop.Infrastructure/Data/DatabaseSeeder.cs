using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartsShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Data;

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
            // Ensure database is created / migrated to the latest schema.
            await context.Database.MigrateAsync();

            // Essential bootstrap only — roles, permissions, and one login per role.
            // No application config (shop policies / business settings) and no
            // demo/sample data (customers, catalog, stock, vehicles) is seeded.
            await SeedRolesAsync(roleManager, logger);
            await SeedPermissionsAsync(context, logger);
            await SeedUsersAsync(userManager, logger, configuration, environment);
            await SeedWalkInCustomerAsync(customerRepository, logger);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            new { Name = "Admin", Description = "Full system access with all permissions" },
            new { Name = "Manager", Description = "Department manager with limited administrative access" },
            new { Name = "User", Description = "Standard user with basic access" },
            new { Name = "Viewer", Description = "Read-only access to the system" }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                var appRole = new ApplicationRole
                {
                    Name = role.Name,
                    Description = role.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                var result = await roleManager.CreateAsync(appRole);
                if (result.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully", role.Name);
                }
                else
                {
                    logger.LogError("Failed to create role '{RoleName}': {Errors}",
                        role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        Microsoft.Extensions.Hosting.IHostEnvironment environment)
    {
        // SECURITY: never seed demo logins (manager/user/viewer) or a hardcoded admin password in
        // production. Demo users are seeded only when explicitly enabled (default ON in Development,
        // OFF otherwise). The admin account is always ensured, but its password must come from
        // configuration (Seed:AdminPassword / Seed__AdminPassword) outside Development.
        var seedDemoUsers = bool.TryParse(configuration["Seed:DemoUsers"], out var demoFlag)
            ? demoFlag
            : environment.IsDevelopment();
        var configuredAdminPassword = configuration["Seed:AdminPassword"];
        var adminPassword = !string.IsNullOrWhiteSpace(configuredAdminPassword)
            ? configuredAdminPassword
            : (environment.IsDevelopment() ? "Admin@1990" : null);

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            // No admin yet and no password provided in a non-dev environment — skip rather than
            // create a guessable admin. Operator must set Seed:AdminPassword to bootstrap.
            if (await userManager.FindByNameAsync("admin") is null)
                logger.LogWarning("Admin user not seeded: set Seed:AdminPassword to bootstrap the first admin in this environment.");
            return;
        }

        var users = new List<(string Username, string Email, string Password, string FirstName, string LastName, string Role)>
        {
            ("admin", "admin@autopartshop.com", adminPassword, "System", "Administrator", "Admin")
        };

        if (seedDemoUsers)
        {
            users.Add(("manager", "manager@autopartshop.com", "Manager@1990", "Demo", "Manager", "Manager"));
            users.Add(("user",    "user@autopartshop.com",    "User@1990",    "Demo", "User",    "User"));
            users.Add(("viewer",  "viewer@autopartshop.com",  "Viewer@1990",  "Demo", "Viewer",  "Viewer"));
        }

        foreach (var u in users)
        {
            var existing = await userManager.FindByNameAsync(u.Username);
            if (existing != null)
            {
                logger.LogInformation("User '{Username}' already exists", u.Username);
                continue;
            }

            var appUser = new ApplicationUser
            {
                UserName = u.Username,
                Email = u.Email,
                EmailConfirmed = true,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            var result = await userManager.CreateAsync(appUser, u.Password);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create user '{Username}': {Errors}",
                    u.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            logger.LogInformation("User '{Username}' created successfully", u.Username);

            var roleResult = await userManager.AddToRoleAsync(appUser, u.Role);
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Role '{Role}' assigned to user '{Username}'", u.Role, u.Username);
            }
            else
            {
                logger.LogError("Failed to assign role '{Role}' to user '{Username}': {Errors}",
                    u.Role, u.Username, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedWalkInCustomerAsync(ICustomerRepository customerRepository, ILogger logger)
    {
        // Reserved "Walk-in" customer used for anonymous/cash counter sales. Business rule:
        // this customer must never carry a due/credit balance (enforced in SalesOrderController).
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

    private static async Task SeedPermissionsAsync(AutoPartDbContext context, ILogger logger)
    {
        if (!await context.Permissions.AnyAsync())
        {
            var permissions = new[]
            {
                // User Management
                Permission.Create("users.view", "View Users", "User Management", "Can view user list and details"),
                Permission.Create("users.create", "Create Users", "User Management", "Can create new users"),
                Permission.Create("users.edit", "Edit Users", "User Management", "Can edit existing users"),
                Permission.Create("users.delete", "Delete Users", "User Management", "Can delete users"),
                Permission.Create("users.assign-roles", "Assign Roles", "User Management", "Can assign roles to users"),

                // Role Management
                Permission.Create("roles.view", "View Roles", "Role Management", "Can view role list and details"),
                Permission.Create("roles.create", "Create Roles", "Role Management", "Can create new roles"),
                Permission.Create("roles.edit", "Edit Roles", "Role Management", "Can edit existing roles"),
                Permission.Create("roles.delete", "Delete Roles", "Role Management", "Can delete roles"),
                Permission.Create("roles.assign-permissions", "Assign Permissions", "Role Management", "Can assign permissions to roles"),

                // Inventory Management
                Permission.Create("inventory.view", "View Inventory", "Inventory", "Can view inventory items"),
                Permission.Create("inventory.create", "Create Inventory", "Inventory", "Can add new inventory items"),
                Permission.Create("inventory.edit", "Edit Inventory", "Inventory", "Can edit inventory items"),
                Permission.Create("inventory.delete", "Delete Inventory", "Inventory", "Can delete inventory items"),
                Permission.Create("inventory.adjust-stock", "Adjust Stock", "Inventory", "Can adjust stock levels"),

                // Sales Management
                Permission.Create("sales.view", "View Sales", "Sales", "Can view sales orders"),
                Permission.Create("sales.create", "Create Sales", "Sales", "Can create new sales orders"),
                Permission.Create("sales.edit", "Edit Sales", "Sales", "Can edit sales orders"),
                Permission.Create("sales.delete", "Delete Sales", "Sales", "Can delete sales orders"),
                Permission.Create("sales.process-payment", "Process Payment", "Sales", "Can process customer payments"),

                // Procurement Management
                Permission.Create("procurement.view", "View Procurement", "Procurement", "Can view purchase orders"),
                Permission.Create("procurement.create", "Create Procurement", "Procurement", "Can create new purchase orders"),
                Permission.Create("procurement.edit", "Edit Procurement", "Procurement", "Can edit purchase orders"),
                Permission.Create("procurement.delete", "Delete Procurement", "Procurement", "Can delete purchase orders"),
                Permission.Create("procurement.approve", "Approve Procurement", "Procurement", "Can approve purchase orders"),

                // Reports
                Permission.Create("reports.view", "View Reports", "Reports", "Can view all reports"),
                Permission.Create("reports.export", "Export Reports", "Reports", "Can export reports to various formats"),

                // Audit Logs
                Permission.Create("audit.view", "View Audit Logs", "Audit", "Can view system audit logs"),
            };

            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} permissions", permissions.Length);
        }
        else
        {
            logger.LogInformation("Permissions already exist, skipping seed");
        }
    }
}
