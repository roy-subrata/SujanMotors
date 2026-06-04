using AutoPartShop.Domain.Entities;
using AutoPartsShop.Domain.Entities;
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
            // Ensure database is created / migrated to the latest schema.
            await context.Database.MigrateAsync();

            // Essential bootstrap only — roles, the admin login, and permissions.
            // Demo/sample data (customers, e-commerce catalog, stock, vehicles,
            // compatibilities) is intentionally NOT seeded.
            await SeedRolesAsync(roleManager, logger);
            await SeedAdminUserAsync(userManager, logger);
            await SeedPermissionsAsync(context, logger);

            // Application configuration defaults (shop policies + business settings).
            await SeedShopPoliciesAsync(context, logger);
            await SeedBusinessSettingsAsync(context, logger);

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

    private static async Task SeedShopPoliciesAsync(AutoPartDbContext context, ILogger logger)
    {
        var keys = new[]
        {
            "SHOP_FREE_SHIPPING_ENABLED",
            "SHOP_FREE_SHIPPING_THRESHOLD",
            "SHOP_FREE_SHIPPING_CURRENCY",
            "SHOP_RETURN_POLICY_DAYS",
            "SHOP_RETURN_POLICY_TEXT"
        };

        if (await context.ApplicationSettings.AnyAsync(s => keys.Contains(s.Key)))
        {
            logger.LogInformation("Shop policy settings already exist, skipping seed");
            return;
        }

        var policies = new[]
        {
            ApplicationSettings.Create("SHOP_FREE_SHIPPING_ENABLED",  "true",                   "BOOL",    "SHOP", "Enable free shipping badge on product pages",     isSystemSetting: true),
            ApplicationSettings.Create("SHOP_FREE_SHIPPING_THRESHOLD", "5000",                  "DECIMAL", "SHOP", "Minimum order amount for free shipping (BDT)",    isSystemSetting: true),
            ApplicationSettings.Create("SHOP_FREE_SHIPPING_CURRENCY",  "BDT",                   "STRING",  "SHOP", "Currency code shown with free shipping threshold", isSystemSetting: true),
            ApplicationSettings.Create("SHOP_RETURN_POLICY_DAYS",      "30",                    "INT",     "SHOP", "Number of days in the store return policy",       isSystemSetting: true),
            ApplicationSettings.Create("SHOP_RETURN_POLICY_TEXT",      "30-day return policy",  "STRING",  "SHOP", "Return policy label shown on product pages",      isSystemSetting: true),
        };

        foreach (var s in policies) { s.CreatedBy = "System"; s.ModifiedBy = "System"; }
        context.ApplicationSettings.AddRange(policies);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded shop policy settings");
    }

    private static async Task SeedBusinessSettingsAsync(AutoPartDbContext context, ILogger logger)
    {
        // Upsert pattern — only inserts keys that don't already exist,
        // so new keys are added to existing installations without touching live values.
        var defaults = new (string Key, string Value, string Description)[]
        {
            ("SHOP_NAME",           "SujanMotors Auto Parts",   "Business name printed on all documents"),
            ("SHOP_ADDRESS",        "Dhaka, Bangladesh",         "Business address printed on all documents"),
            ("SHOP_PHONE",          "+880 1XXXXXXXXX",           "Contact phone printed on documents"),
            ("SHOP_EMAIL",          "info@sujanmotors.com",      "Business email printed on documents"),
            ("SHOP_TAX_NUMBER",     "",                          "Tax / VAT registration number"),
            ("SHOP_LOGO_URL",       "assets/logo.png",           "Logo URL (relative path or https:// URL)"),
            ("SHOP_TAGLINE",        "",                          "Optional tagline shown below the company name"),
            ("INVOICE_FOOTER_TEXT", "Thank you for your business!",
                                                                 "Footer message on every invoice"),
            ("CHALLAN_FOOTER_TEXT", "Goods once dispatched will not be accepted back without prior notice.",
                                                                 "Footer message on every delivery challan"),
        };

        var existingKeys = await context.ApplicationSettings
            .Where(s => !s.Isdeleted)
            .Select(s => s.Key)
            .ToListAsync();

        int added = 0;
        foreach (var (key, value, desc) in defaults)
        {
            if (existingKeys.Contains(key)) continue;
            var s = ApplicationSettings.Create(key, value, "STRING", "BUSINESS", desc, isSystemSetting: true);
            s.CreatedBy = "System";
            s.ModifiedBy = "System";
            context.ApplicationSettings.Add(s);
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} business setting(s)", added);
        }
        else
        {
            logger.LogInformation("Business settings already up to date");
        }
    }
}
