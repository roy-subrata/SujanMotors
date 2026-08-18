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
/// Creates: Admin role, admin user, Manager/User roles (with their permission sets),
/// the permission catalog, and walk-in customer.
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

            // Create non-admin roles (Manager, User) with their permission sets.
            // Requires the permission catalog, so it runs before creating users.
            await SeedNonAdminRolesAsync(context, roleManager, logger);

            // Create admin user (the only user seeded)
            await SeedAdminUserAsync(userManager, logger, configuration, environment);

            // Reserved walk-in customer for anonymous/cash sales
            await SeedWalkInCustomerAsync(customerRepository, logger);

            // Default database-backup schedule settings (admin-editable from the UI)
            var settingsRepository = scope.ServiceProvider.GetRequiredService<IApplicationSettingsRepository>();
            await SeedBackupSettingsAsync(settingsRepository, logger);
            await SeedShopProfileSettingsAsync(settingsRepository, logger);

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
    /// The known permission names, kept in sync with <c>Permissions</c> in AutoPartShop.Api.
    /// Seeded so role-permission management works out of the box on fresh installs.
    /// </summary>
    private static readonly (string Name, string DisplayName, string Category, string Description)[] PermissionCatalog =
    [
        // User Management
        ("users.view", "View Users", "User Management", "View users list and details"),
        ("users.create", "Create Users", "User Management", "Create new users"),
        ("users.edit", "Edit Users", "User Management", "Edit users and activate/deactivate them"),
        ("users.delete", "Delete Users", "User Management", "Delete users"),
        ("users.assign-roles", "Assign Roles to Users", "User Management", "Assign roles to users"),

        // Role Management
        ("roles.view", "View Roles", "Role Management", "View roles and their permissions"),
        ("roles.create", "Create Roles", "Role Management", "Create new roles"),
        ("roles.edit", "Edit Roles", "Role Management", "Edit roles"),
        ("roles.delete", "Delete Roles", "Role Management", "Delete roles"),
        ("roles.assign-permissions", "Assign Role Permissions", "Role Management", "Assign permissions to roles"),

        // Inventory
        ("inventory.view", "View Inventory", "Inventory", "View parts, categories, brands and stock levels"),
        ("inventory.create", "Create Inventory", "Inventory", "Create parts, categories and brands"),
        ("inventory.edit", "Edit Inventory", "Inventory", "Edit parts, categories and brands"),
        ("inventory.delete", "Delete Inventory", "Inventory", "Delete parts, categories and brands"),
        ("inventory.adjust-stock", "Adjust Stock", "Inventory", "Perform stock adjustments and count corrections"),

        // Sales
        ("sales.view", "View Sales", "Sales", "View sales orders, invoices, customers and returns"),
        ("sales.create", "Create Sales", "Sales", "Create sales orders and quick sales"),
        ("sales.edit", "Edit Sales", "Sales", "Edit sales orders and process returns"),
        ("sales.delete", "Delete Sales", "Sales", "Delete sales orders"),
        ("sales.process-payment", "Process Sales Payments", "Sales", "Record and process customer payments"),
        ("sales.require-till-session", "Require Till Session", "Sales", "Require an open till session to complete quick sales"),

        // Procurement
        ("procurement.view", "View Procurement", "Procurement", "View purchase orders, suppliers and supplier payments"),
        ("procurement.create", "Create Procurement", "Procurement", "Create purchase orders and suppliers"),
        ("procurement.edit", "Edit Procurement", "Procurement", "Edit purchase orders and suppliers"),
        ("procurement.delete", "Delete Procurement", "Procurement", "Delete purchase orders and suppliers"),
        ("procurement.approve", "Approve Procurement", "Procurement", "Approve purchase orders"),

        // Reports
        ("reports.view", "View Reports", "Reports", "View dashboard reports and analytics"),
        ("reports.export", "Export Reports", "Reports", "Export reports to CSV/Excel"),

        // Audit
        ("audit.view", "View Audit Trail", "Audit", "View the audit trail log"),

        // Backups
        ("backups.manage", "Manage Backups", "Backups", "Configure, run and restore database backups")
    ];

    /// <summary>
    /// Seeds the Manager and User roles (with default permission sets) if they don't exist.
    /// Existing roles that already have permissions are left untouched so admin edits survive restarts.
    /// </summary>
    private static async Task SeedNonAdminRolesAsync(AutoPartDbContext context, RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        await SeedPermissionCatalogAsync(context, logger);

        var roleDefinitions = new (string Name, string Description, string[] Permissions)[]
        {
            (
                "Manager",
                "Operational manager with full inventory, sales and procurement control (no user/role administration)",
                [
                    "inventory.view", "inventory.create", "inventory.edit", "inventory.adjust-stock",
                    "sales.view", "sales.create", "sales.edit", "sales.process-payment",
                    "procurement.view", "procurement.create", "procurement.edit", "procurement.approve",
                    "reports.view", "reports.export",
                    "audit.view"
                ]
            ),
            (
                "User",
                "Cashier / salesperson with sales-only access (no inventory changes, no returns, no reports export)",
                [
                    "inventory.view",
                    "sales.view", "sales.create", "sales.process-payment",
                    "reports.view"
                ]
            )
        };

        foreach (var (name, description, permissions) in roleDefinitions)
        {
            await SeedRoleWithPermissionsAsync(context, roleManager, logger, name, description, permissions);
        }
    }

    private static async Task SeedPermissionCatalogAsync(AutoPartDbContext context, ILogger logger)
    {
        var existingNames = await context.Permissions
            .Where(p => !p.Isdeleted)
            .Select(p => p.Name)
            .ToHashSetAsync();

        var added = 0;
        foreach (var (name, displayName, category, description) in PermissionCatalog)
        {
            if (existingNames.Contains(name))
                continue;

            var permission = Permission.Create(name, displayName, category, description);
            permission.CreatedDate = DateTime.UtcNow;
            permission.ModifiedDate = DateTime.UtcNow;
            permission.CreatedBy = "System";
            permission.ModifiedBy = "System";
            context.Permissions.Add(permission);
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} permissions", added);
        }
    }

    private static async Task SeedRoleWithPermissionsAsync(
        AutoPartDbContext context,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger,
        string roleName,
        string description,
        IEnumerable<string> permissionNames)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role != null)
        {
            // Respect admin edits: only seed permissions for a role that has none yet.
            var hasPermissions = await context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id);
            if (hasPermissions)
            {
                logger.LogInformation("{Role} role already exists with permissions, skipping", roleName);
                return;
            }
        }
        else
        {
            role = new ApplicationRole
            {
                Name = roleName,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            var createResult = await roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create {Role} role: {Errors}",
                    roleName, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        var permissionIds = await context.Permissions
            .Where(p => permissionNames.Contains(p.Name) && p.IsActive && !p.Isdeleted)
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var permissionId in permissionIds)
        {
            context.RolePermissions.Add(RolePermission.Create(role.Id, permissionId, "System"));
        }

        await context.SaveChangesAsync();
        logger.LogInformation("{Role} role seeded with {Count} permissions", roleName, permissionIds.Count);
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

    /// <summary>
    /// Seeds the BUSINESS and BRANDING setting rows the company profile and every printed document
    /// read from. Values are intentionally blank — this seeds the shape, not the shop's details,
    /// which an admin fills in from Company Profile.
    ///
    /// Without the rows the categories did not exist at all: /ApplicationSettings/categories
    /// listed only BACKUP and CURRENCY, and /public/shop returned empty strings for every field,
    /// so invoices and challans rendered blank headers with nothing in the UI to explain why.
    /// </summary>
    private static async Task SeedShopProfileSettingsAsync(IApplicationSettingsRepository settingsRepository, ILogger logger)
    {
        var defaults = new (string Key, string Value, string DataType, string Category, string Description)[]
        {
            ("SHOP_NAME", "", "STRING", "BUSINESS", "Trading name shown on invoices, challans and the storefront"),
            ("SHOP_ADDRESS", "", "STRING", "BUSINESS", "Street address printed in document headers"),
            ("SHOP_PHONE", "", "STRING", "BUSINESS", "Contact phone printed in document headers"),
            ("SHOP_EMAIL", "", "STRING", "BUSINESS", "Contact email printed in document headers"),
            ("SHOP_TAX_NUMBER", "", "STRING", "BUSINESS", "VAT/BIN registration number printed on tax documents"),
            ("SHOP_TAGLINE", "", "STRING", "BUSINESS", "Optional strapline under the shop name"),
            ("INVOICE_FOOTER_TEXT", "", "STRING", "BUSINESS", "Free text printed at the foot of every invoice"),
            ("CHALLAN_FOOTER_TEXT", "", "STRING", "BUSINESS", "Free text printed at the foot of every challan"),
            ("SHOP_LOGO_URL", "", "STRING", "BRANDING", "Logo used on printed documents"),
            ("APP_NAME", "", "STRING", "BRANDING", "Application name shown in the web app shell"),
            ("APP_LOGO_URL", "", "STRING", "BRANDING", "Logo shown in the web app shell")
        };

        foreach (var (key, value, dataType, category, description) in defaults)
        {
            if (await settingsRepository.ExistsByKeyAsync(key))
                continue;

            await settingsRepository.SetValueAsync(key, value, dataType, category, description, isSystemSetting: false);
            logger.LogInformation("Seeded shop profile setting {Key}", key);
        }
    }

    /// <summary>
    /// Seeds default BACKUP category settings if missing. Values are edited by the admin
    /// from the Backups page; the backup scheduler re-reads them every poll cycle.
    /// </summary>
    private static async Task SeedBackupSettingsAsync(IApplicationSettingsRepository settingsRepository, ILogger logger)
    {
        var defaults = new (string Key, string Value, string DataType, string Description)[]
        {
            ("BACKUP:ENABLED", "false", "BOOL", "Whether scheduled daily database backups are enabled"),
            ("BACKUP:LOCAL_TIME", "02:00", "STRING", "Shop-local time of day (HH:mm) to run the daily backup"),
            ("BACKUP:TZ_OFFSET_MINUTES", "360", "INT", "Minutes to shift UTC to the shop's local clock (360 = UTC+6)"),
            ("BACKUP:RETENTION_COUNT", "14", "INT", "Number of most recent backups to keep locally and on Google Drive"),
            ("BACKUP:GDRIVE_FOLDER_ID", "", "STRING", "Google Drive folder id shared with the service account; empty = skip upload")
        };

        foreach (var (key, value, dataType, description) in defaults)
        {
            if (await settingsRepository.ExistsByKeyAsync(key))
                continue;

            await settingsRepository.SetValueAsync(key, value, dataType, "BACKUP", description, isSystemSetting: true);
            logger.LogInformation("Seeded backup setting {Key}", key);
        }
    }
}
