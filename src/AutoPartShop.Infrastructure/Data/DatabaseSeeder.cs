using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        try
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Seed Roles
            await SeedRolesAsync(roleManager, logger);

            // Seed Admin User
            await SeedAdminUserAsync(userManager, logger);

            // Seed Permissions
            await SeedPermissionsAsync(context, logger);

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

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminUsername = "admin";
        const string adminEmail = "admin@autopartshop.com";
        const string adminPassword = "Admin@1990";

        var adminUser = await userManager.FindByNameAsync(adminUsername);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminUsername,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                logger.LogInformation("Admin user created successfully");

                // Assign Admin role
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Admin role assigned to admin user successfully");
                }
                else
                {
                    logger.LogError("Failed to assign Admin role: {Errors}",
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Admin user already exists");
        }
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
