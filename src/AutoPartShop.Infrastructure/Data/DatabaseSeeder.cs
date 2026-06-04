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
        var codeService = scope.ServiceProvider.GetRequiredService<ICodeGenerateService>();

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

            await SeedCustomerAsync(context, codeService, logger);
            await SeedEcommerceCatalogAsync(context, logger);
            await SeedShopPoliciesAsync(context, logger);
            await SeedBusinessSettingsAsync(context, logger);
            await EnsureDemoStockAsync(context, logger);
            await SeedDemoVehiclesAsync(context, logger);
            await SeedDemoCompatibilitiesAsync(context, logger);

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

    private static async Task SeedCustomerAsync(AutoPartDbContext context, ICodeGenerateService codeGenerate, ILogger logger)
    {
        // Check if customers have already been seeded
        var existingCustomersCount = await context.Customers.CountAsync();
        if (existingCustomersCount > 0)
        {
            logger.LogInformation("Customers already exist ({Count}), skipping seed", existingCustomersCount);
            return;
        }

        try
        {
            var customersToAdd = new List<Customer>();
            
            // Generate 20 customers with unique codes
            for (int i = 1; i <= 20; i++)
            {
                var code = await codeGenerate.GenerateAsync("CUST");
                var customer = Customer.Create(
                    code, 
                    $"FirstName-{i}", 
                    $"LastName-{i}",
                    $"customer{i}@example.com", 
                    $"0909{i:D6}", 
                    $"Company-{i}", 
                    $"City-{i}", 
                    $"State-{i}", 
                    $"Address-{i}", 
                    $"PostalCode-{i}", 
                    $"Country-{i}", 
                    $"Notes-{i}"
                );
                customersToAdd.Add(customer);
            }

            // Add all customers to context
            foreach (var customer in customersToAdd)
            {
                context.Customers.Add(customer);
            }

            // Save changes
            await context.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {Count} customers", customersToAdd.Count);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            logger.LogWarning(ex, "Duplicate customer codes detected during seeding. Database may have been seeded previously.");
            // Don't throw - allow application to continue if seed data already exists
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while seeding customers");
            throw;
        }
    }

    private static async Task SeedEcommerceCatalogAsync(AutoPartDbContext context, ILogger logger)
    {
        // Skip only if our demo catalog entries already exist
        if (await context.ProductCatalogEntries.AnyAsync())
        {
            logger.LogInformation("E-commerce catalog entries already exist, skipping seed");
            return;
        }

        logger.LogInformation("Seeding e-commerce catalog...");

        // ── Units (load existing by code, insert only if missing) ────────────
        async Task<Unit> EnsureUnit(string name, string code, string symbol)
        {
            var existing = await context.Units.FirstOrDefaultAsync(u => u.Code == code);
            if (existing != null) return existing;
            var u = Unit.Create(name, code, symbol);
            u.CreatedBy = "System"; u.ModifiedBy = "System";
            context.Units.Add(u);
            await context.SaveChangesAsync();
            return u;
        }
        var unitPcs  = await EnsureUnit("Piece", "PCS",  "pcs");
        var unitLtr  = await EnsureUnit("Litre", "LTR",  "L");
        var unitSet  = await EnsureUnit("Set",   "SET",  "set");
        var unitPair = await EnsureUnit("Pair",  "PAIR", "pair");

        // ── Warehouse ─────────────────────────────────────────────────────────
        var wh = await context.Warehouses.FirstOrDefaultAsync(w => w.Code == "WH-MAIN");
        if (wh is null)
        {
            wh = Warehouse.Create("Main Warehouse", "WH-MAIN", "123 Industrial Area", "Dhaka",
                "Dhaka Division", "Bangladesh", "1200", "System Admin");
            wh.CreatedBy = "System"; wh.ModifiedBy = "System";
            context.Warehouses.Add(wh);
            await context.SaveChangesAsync();
        }

        // ── Brands (load existing by code, insert only if missing) ────────────
        async Task<Brand> EnsureBrand(string name, string code, string desc, string country)
        {
            var existing = await context.Brands.FirstOrDefaultAsync(b => b.Code == code);
            if (existing != null) return existing;
            var b = Brand.Create(name, code, desc, country);
            b.CreatedBy = "System"; b.ModifiedBy = "System";
            context.Brands.Add(b);
            await context.SaveChangesAsync();
            return b;
        }
        var bosch   = await EnsureBrand("Bosch",   "BOSCH",   "German engineering excellence",        "Germany");
        var denso   = await EnsureBrand("Denso",   "DENSO",   "Trusted Japanese OEM supplier",        "Japan");
        var ngk     = await EnsureBrand("NGK",     "NGK",     "World's #1 spark plug brand",          "Japan");
        var brembo  = await EnsureBrand("Brembo",  "BREMBO",  "Premium braking systems",              "Italy");
        var monroe  = await EnsureBrand("Monroe",  "MONROE",  "Ride comfort and handling",            "USA");
        var mann    = await EnsureBrand("Mann",    "MANN",    "Filtration specialists",               "Germany");
        var castrol = await EnsureBrand("Castrol", "CASTROL", "Premium engine lubricants",            "UK");
        var hella   = await EnsureBrand("Hella",   "HELLA",   "Automotive lighting & electronics",    "Germany");

        // ── Categories (load existing by code, insert only if missing) ─────────
        async Task<Category> EnsureCategory(string name, string desc, string code, int order, string breadcrumb)
        {
            var existing = await context.Categories.FirstOrDefaultAsync(c => c.Code == code);
            if (existing != null) return existing;
            var c = Category.Create(name, desc, code, order, null, breadcrumb, 0);
            c.CreatedBy = "System"; c.ModifiedBy = "System";
            context.Categories.Add(c);
            await context.SaveChangesAsync();
            return c;
        }
        var catEngine  = await EnsureCategory("Engine Parts",   "Engine components and gaskets",  "CAT-ENG", 1, "Engine Parts");
        var catBrake   = await EnsureCategory("Brake System",   "Brake pads, rotors, calipers",   "CAT-BRK", 2, "Brake System");
        var catSuspend = await EnsureCategory("Suspension",     "Shocks, struts, control arms",   "CAT-SUS", 3, "Suspension");
        var catElec    = await EnsureCategory("Electrical",     "Batteries, lighting, ignition",  "CAT-ELC", 4, "Electrical");
        var catBody    = await EnsureCategory("Body Parts",     "Mirrors, bumpers, panels",       "CAT-BDY", 5, "Body Parts");
        var catOils    = await EnsureCategory("Oils & Fluids",  "Engine oil, coolant, fluids",    "CAT-OIL", 6, "Oils & Fluids");
        var catFilters = await EnsureCategory("Filters",        "Oil, air, fuel, cabin filters",  "CAT-FLT", 7, "Filters");
        var catTires   = await EnsureCategory("Tires & Wheels", "Alloy wheels and tires",         "CAT-TYR", 8, "Tires & Wheels");

        // ── Attribute Groups (load existing by name, insert only if missing) ────
        async Task<ProductAttributeGroup> EnsureAttrGroup(string name, int order)
        {
            var existing = await context.ProductAttributeGroups.FirstOrDefaultAsync(g => g.Name == name);
            if (existing != null) return existing;
            var g = ProductAttributeGroup.Create(name, order);
            g.CreatedBy = "System"; g.ModifiedBy = "System";
            context.ProductAttributeGroups.Add(g);
            await context.SaveChangesAsync();
            return g;
        }
        var grpGeneral = await EnsureAttrGroup("General",     1);
        var grpPerf    = await EnsureAttrGroup("Performance", 2);
        var grpCompat  = await EnsureAttrGroup("Fitment",     3);

        // ── Attributes (load existing by code, insert only if missing) ────────
        async Task<ProductAttribute> EnsureAttr(Guid groupId, string name, string code, string dataType, string? unit = null)
        {
            var existing = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Code == code);
            if (existing != null) return existing;
            var a = unit != null
                ? ProductAttribute.Create(groupId, name, code, dataType, unit)
                : ProductAttribute.Create(groupId, name, code, dataType);
            a.CreatedBy = "System"; a.ModifiedBy = "System";
            context.ProductAttributes.Add(a);
            await context.SaveChangesAsync();
            return a;
        }
        var attrGrade     = await EnsureAttr(grpGeneral.Id, "Grade",      "GRADE",    "option");
        var attrPos       = await EnsureAttr(grpCompat.Id,  "Position",   "POSITION", "option");
        var attrBrakeTyp  = await EnsureAttr(grpPerf.Id,    "Brake Type", "BRAKE_TYP","option");
        var attrViscosity = await EnsureAttr(grpGeneral.Id, "Viscosity",  "VISCOSITY","option");
        var attrVolume    = await EnsureAttr(grpGeneral.Id, "Volume",     "VOLUME",   "option", "L");
        var attrMaterial  = await EnsureAttr(grpPerf.Id,    "Material",   "MATERIAL", "option");

        // ── Attribute Options (load existing by attrId+value, insert if missing)
        async Task<ProductAttributeOption> EnsureOpt(Guid attrId, string value, int order)
        {
            var existing = await context.ProductAttributeOptions
                .FirstOrDefaultAsync(o => o.AttributeId == attrId && o.Value == value);
            if (existing != null) return existing;
            var o = ProductAttributeOption.Create(attrId, value, order);
            o.CreatedBy = "System"; o.ModifiedBy = "System";
            context.ProductAttributeOptions.Add(o);
            await context.SaveChangesAsync();
            return o;
        }
        var optStandard     = await EnsureOpt(attrGrade.Id,    "Standard",     1);
        var optPremium      = await EnsureOpt(attrGrade.Id,    "Premium",      2);
        var optFront        = await EnsureOpt(attrPos.Id,      "Front",        1);
        var optRear         = await EnsureOpt(attrPos.Id,      "Rear",         2);
        var optCeramic      = await EnsureOpt(attrBrakeTyp.Id, "Ceramic",      1);
        var optSemiMetallic = await EnsureOpt(attrBrakeTyp.Id, "Semi-Metallic",2);
        var opt5W30         = await EnsureOpt(attrViscosity.Id,"5W-30",        1);
        var opt10W40        = await EnsureOpt(attrViscosity.Id,"10W-40",       2);
        var opt0W20         = await EnsureOpt(attrViscosity.Id,"0W-20",        3);
        var opt1L           = await EnsureOpt(attrVolume.Id,   "1L",           1);
        var opt4L           = await EnsureOpt(attrVolume.Id,   "4L",           2);
        var opt5L           = await EnsureOpt(attrVolume.Id,   "5L",           3);
        var optSteel        = await EnsureOpt(attrMaterial.Id, "Steel",        1);
        var optAluminium    = await EnsureOpt(attrMaterial.Id, "Aluminium",    2);
        var optCarbon       = await EnsureOpt(attrMaterial.Id, "Carbon",       3);

        // Keep a local lookup so AddPart helpers can still call OptId(value)
        var opts = new[] {
            optStandard, optPremium, optFront, optRear, optCeramic, optSemiMetallic,
            opt5W30, opt10W40, opt0W20, opt1L, opt4L, opt5L, optSteel, optAluminium, optCarbon
        };

        // ── Category ↔ Attribute links (skip if already linked) ───────────────
        async Task LinkCatAttr(Guid catId, Guid attrId, string filterType = "select", int sort = 1)
        {
            if (await context.CategoryAttributes.AnyAsync(ca => ca.CategoryId == catId && ca.AttributeId == attrId))
                return;
            var ca = CategoryAttribute.Create(catId, attrId, isFilterable: true, filterType: filterType, sortOrder: sort);
            ca.CreatedBy = "System"; ca.ModifiedBy = "System";
            context.CategoryAttributes.Add(ca);
        }
        await LinkCatAttr(catEngine.Id,  attrGrade.Id,     "select", 1);
        await LinkCatAttr(catEngine.Id,  attrMaterial.Id,  "select", 2);
        await LinkCatAttr(catBrake.Id,   attrGrade.Id,     "select", 1);
        await LinkCatAttr(catBrake.Id,   attrBrakeTyp.Id,  "select", 2);
        await LinkCatAttr(catBrake.Id,   attrPos.Id,       "select", 3);
        await LinkCatAttr(catSuspend.Id, attrGrade.Id,     "select", 1);
        await LinkCatAttr(catSuspend.Id, attrPos.Id,       "select", 2);
        await LinkCatAttr(catElec.Id,    attrGrade.Id,     "select", 1);
        await LinkCatAttr(catOils.Id,    attrViscosity.Id, "select", 1);
        await LinkCatAttr(catOils.Id,    attrVolume.Id,    "select", 2);
        await LinkCatAttr(catFilters.Id, attrGrade.Id,     "select", 1);
        await LinkCatAttr(catBody.Id,    attrPos.Id,       "select", 1);
        await context.SaveChangesAsync();
        await context.SaveChangesAsync();

        // Get existing SKUs to avoid duplicates
        var existingSkus = await context.Parts.Select(p => p.SKU).ToHashSetAsync();

        // Helper to create product + catalog entry + stock + optional discount
        // Lookup helpers for attribute options
        Guid OptId(string val) => opts.First(o => o.Value == val).Id;
        Guid AttrId(ProductAttribute a) => a.Id;

        async Task<Product?> AddPart(
            string name, string sku, string pn, Guid catId, Guid? brandId,
            decimal cost, decimal sell, string desc, string shortDesc, string img,
            bool featured = false, int rank = 0, decimal pop = 0,
            bool warranty = false, int wMonths = 0, string? wType = null,
            int stock = 50, bool onSale = false, decimal disc = 0, string tags = "",
            // Variant: grade options to create (e.g. ["Standard","Premium"])
            string[]? grades = null,
            // Extra attribute: position ("Front","Rear", etc.)
            string? position = null,
            // Extra: viscosity for oils
            string? viscosity = null,
            // Extra: volume for fluids
            string? volume = null,
            // Extra: brake type
            string? brakeType = null)
        {
            if (existingSkus.Contains(sku)) return null;

            var part = Product.Create(name, PartNumber.Create(pn), sku, catId,
                brandId: brandId, baseUnitId: unitPcs.Id, unitId: unitPcs.Id,
                description: desc, costPrice: cost, sellingPrice: sell,
                hasWarranty: warranty, warrantyPeriodMonths: wMonths,
                warrantyType: wType, tags: tags);
            part.CreatedBy = "System"; part.ModifiedBy = "System";
            context.Parts.Add(part);
            await context.SaveChangesAsync();

            // Catalog entry
            var slug = name.ToLowerInvariant()
                .Replace(" ", "-").Replace("/", "-").Replace("&", "and")
                .Replace(",", "").Replace("(", "").Replace(")", "").Replace(".", "");
            var entry = ProductCatalogEntry.Create(part.Id, slug, shortDesc,
                isPublished: true, publishedAt: DateTime.UtcNow.AddDays(-30),
                isFeatured: featured, featuredRank: rank, popularityScore: pop,
                primaryImageUrl: img);
            entry.CreatedBy = "System"; entry.ModifiedBy = "System";
            context.ProductCatalogEntries.Add(entry);

            // Stock level (part-level) — starts at 0; real stock enters via Goods Receipts
            var sl = StockLevel.Create(part.Id, wh.Id, reorderLevel: 5, reorderQuantity: 20, unitId: unitPcs.Id);
            sl.CreatedBy = "System"; sl.ModifiedBy = "System";
            context.StockLevels.Add(sl);
            await context.SaveChangesAsync();

            // Variants — create one per grade (Standard / Premium)
            var gradeList = grades ?? ["Standard"];
            int variantNum = 1;
            foreach (var grade in gradeList)
            {
                var priceMultiplier = grade == "Premium" ? 1.35m : 1m;
                var variantSell = Math.Round(sell * priceMultiplier, 0);
                var variantCost = Math.Round(cost * priceMultiplier, 0);
                var vCode = $"{sku}-{grade.ToUpper()[..3]}";
                var vSku  = $"SKU-{sku}-{variantNum++}";

                var variant = ProductVariant.Create(
                    part.Id, $"{name} — {grade}", vCode,
                    costPrice: variantCost, sellingPrice: variantSell,
                    sku: vSku, isActive: true);
                variant.CreatedBy = "System"; variant.ModifiedBy = "System";
                context.ProductVariants.Add(variant);
                await context.SaveChangesAsync();

                // Variant attribute values
                void AddAttrVal(Guid attId, Guid optId)
                {
                    var av = VariantAttributeValue.Create(variant.Id, attId, optionId: optId);
                    av.CreatedBy = "System"; av.ModifiedBy = "System";
                    context.VariantAttributeValues.Add(av);
                }

                try { AddAttrVal(AttrId(attrGrade), OptId(grade)); } catch { /* option missing */ }
                if (position != null) try { AddAttrVal(AttrId(attrPos), OptId(position)); } catch { }
                if (viscosity != null) try { AddAttrVal(AttrId(attrViscosity), OptId(viscosity)); } catch { }
                if (volume != null) try { AddAttrVal(AttrId(attrVolume), OptId(volume)); } catch { }
                if (brakeType != null) try { AddAttrVal(AttrId(attrBrakeTyp), OptId(brakeType)); } catch { }

                await context.SaveChangesAsync();

                // Variant-scoped stock level — starts at 0; real stock enters via Goods Receipts
                var vsl = StockLevel.Create(part.Id, wh.Id, reorderLevel: 5, reorderQuantity: 20, unitId: unitPcs.Id, variantId: variant.Id);
                vsl.CreatedBy = "System"; vsl.ModifiedBy = "System";
                context.StockLevels.Add(vsl);
                await context.SaveChangesAsync();
            }

            // Discount
            if (onSale && disc > 0)
            {
                var d = Discount.Create($"Sale on {name}", "PERCENTAGE", disc,
                    DateTime.UtcNow.AddDays(-7), partId: part.Id,
                    endDate: DateTime.UtcNow.AddDays(21),
                    description: $"{disc}% limited time offer");
                d.CreatedBy = "System"; d.ModifiedBy = "System";
                context.Discounts.Add(d);
                await context.SaveChangesAsync();
            }
            return part;
        }

        // ── ENGINE PARTS ───────────────────────────────────────────────────────
        // ENGINE
        await AddPart("Bosch Timing Belt Kit", "TBK-001", "PN-ENG-001", catEngine.Id, bosch.Id,
            1800, 2800, "Complete timing belt kit with tensioner and idler pulley. Fits 1.6L–2.0L petrol engines. Replace every 60,000 km.",
            "Complete timing belt kit with tensioner — OEM quality",
            "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            featured: true, rank: 1, pop: 95, warranty: true, wMonths: 12, wType: "SELLER", stock: 35, tags: "timing belt engine",
            grades: ["Standard", "Premium"]);

        await AddPart("NGK Iridium Spark Plug Set (4pcs)", "SPK-001", "PN-ENG-002", catEngine.Id, ngk.Id,
            380, 650, "Set of 4 NGK iridium spark plugs. 100,000 km service life.",
            "NGK iridium spark plugs × 4 — 100,000 km life",
            "https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=640&q=80",
            featured: true, rank: 2, pop: 88, stock: 80, onSale: true, disc: 15, tags: "spark plug ignition",
            grades: ["Standard", "Premium"]);

        await AddPart("Bosch Fuel Injector", "FI-001", "PN-ENG-003", catEngine.Id, bosch.Id,
            2200, 3500, "High-precision petrol fuel injector. Optimal fuel atomisation, improved combustion efficiency.",
            "High-precision fuel injector — improved efficiency",
            "https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?w=640&q=80",
            pop: 72, warranty: true, wMonths: 6, wType: "MANUFACTURER", stock: 20, tags: "fuel injector engine",
            grades: ["Standard"]);

        await AddPart("Water Pump with Gasket", "WP-001", "PN-ENG-004", catEngine.Id, null,
            900, 1600, "OEM-spec water pump with new gasket. Reliable engine cooling. Fits Toyota Corolla, Honda Civic.",
            "OEM water pump with gasket — engine cooling",
            "https://images.unsplash.com/photo-1632912515069-45e9f2bf0b36?w=640&q=80",
            featured: true, rank: 4, pop: 65, warranty: true, wMonths: 12, wType: "SELLER", stock: 15, tags: "water pump cooling",
            grades: ["Standard"]);

        // BRAKE
        await AddPart("Brembo Front Brake Pads", "BP-F-001", "PN-BRK-001", catBrake.Id, brembo.Id,
            950, 1800, "Brembo ceramic front brake pads. Low dust, low noise. Exceptional stopping power.",
            "Brembo ceramic front brake pads — superior stopping power",
            "https://images.unsplash.com/photo-1621252179027-94459d278660?w=640&q=80",
            featured: true, rank: 3, pop: 92, warranty: true, wMonths: 6, wType: "MANUFACTURER", stock: 45, onSale: true, disc: 20,
            tags: "brake pads front brembo", grades: ["Standard", "Premium"], position: "Front", brakeType: "Ceramic");

        await AddPart("Vented Front Brake Rotors (Pair)", "BDR-001", "PN-BRK-002", catBrake.Id, brembo.Id,
            1400, 2600, "Precision-balanced vented front brake disc rotors, pair. High carbon steel.",
            "Vented front brake rotors pair — high carbon steel",
            "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            pop: 78, warranty: true, wMonths: 12, wType: "MANUFACTURER", stock: 28, tags: "brake rotor disc front",
            grades: ["Standard"], position: "Front");

        await AddPart("DOT4 Brake Fluid 500ml", "BF-001", "PN-BRK-003", catBrake.Id, bosch.Id,
            120, 240, "Premium DOT4 brake fluid. High boiling point for fade-free braking.",
            "DOT4 brake fluid 500ml — high boiling point",
            "https://images.unsplash.com/photo-1629451399979-59fb59ebeef3?w=640&q=80",
            pop: 80, stock: 90, onSale: true, disc: 10, tags: "brake fluid dot4", grades: ["Standard"]);

        // SUSPENSION
        await AddPart("Monroe Front Shock Absorbers (Pair)", "SA-F-001", "PN-SUS-001", catSuspend.Id, monroe.Id,
            2800, 4800, "Monroe OESpectrum front shock absorbers, pair. Restores original ride comfort.",
            "Monroe front shock absorbers pair — restores ride comfort",
            "https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?w=640&q=80",
            featured: true, rank: 5, pop: 83, warranty: true, wMonths: 24, wType: "MANUFACTURER", stock: 18, onSale: true, disc: 10,
            tags: "shock absorber suspension front", grades: ["Standard", "Premium"], position: "Front");

        await AddPart("Lower Control Arm with Ball Joint (Left)", "CAL-001", "PN-SUS-002", catSuspend.Id, null,
            1200, 2200, "Heavy-duty lower control arm with pre-installed ball joint and bushing, left side.",
            "Lower control arm with ball joint — left side",
            "https://images.unsplash.com/photo-1632912515069-45e9f2bf0b36?w=640&q=80",
            pop: 60, warranty: true, wMonths: 12, wType: "SELLER", stock: 22, tags: "control arm suspension",
            grades: ["Standard"]);

        // ELECTRICAL
        await AddPart("Bosch Alternator 65A", "ALT-001", "PN-ELC-001", catElec.Id, bosch.Id,
            5500, 8500, "Remanufactured Bosch 65A alternator. Fully tested. New voltage regulator.",
            "65A Bosch alternator — tested — new voltage regulator",
            "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            pop: 70, warranty: true, wMonths: 12, wType: "MANUFACTURER", stock: 8, tags: "alternator electrical",
            grades: ["Standard"]);

        await AddPart("Hella H7 Halogen Bulbs (Pack of 2)", "HLB-001", "PN-ELC-002", catElec.Id, hella.Id,
            220, 450, "H7 12V 55W halogen headlight bulbs, 2-pack. ECE approved.",
            "H7 halogen headlight bulbs × 2 — ECE approved",
            "https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=640&q=80",
            featured: true, rank: 6, pop: 90, stock: 100, onSale: true, disc: 25, tags: "bulb headlight halogen",
            grades: ["Standard", "Premium"]);

        await AddPart("Denso Starter Motor", "STR-001", "PN-ELC-003", catElec.Id, denso.Id,
            4200, 6800, "Remanufactured Denso starter motor. High-torque for reliable cold starts.",
            "Denso starter motor — high-torque cold start",
            "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            pop: 62, warranty: true, wMonths: 12, wType: "MANUFACTURER", stock: 10, tags: "starter motor electrical",
            grades: ["Standard"]);

        // OILS
        await AddPart("Castrol GTX 5W-30 Engine Oil 4L", "OIL-5W30-4L", "PN-OIL-001", catOils.Id, castrol.Id,
            680, 1100, "Castrol GTX 5W-30 fully synthetic engine oil, 4L. ACEA A3/B4. Superior wear protection.",
            "Castrol GTX 5W-30 fully synthetic — 4L",
            "https://images.unsplash.com/photo-1629451399979-59fb59ebeef3?w=640&q=80",
            featured: true, rank: 7, pop: 98, stock: 150, onSale: true, disc: 12, tags: "engine oil castrol",
            grades: ["Standard"], viscosity: "5W-30", volume: "4L");

        await AddPart("Castrol GTX 10W-40 Engine Oil 4L", "OIL-10W40-4L", "PN-OIL-002", catOils.Id, castrol.Id,
            620, 980, "Castrol GTX 10W-40 semi-synthetic engine oil, 4L. Ideal for older engines.",
            "Castrol GTX 10W-40 semi-synthetic — 4L",
            "https://images.unsplash.com/photo-1629451399979-59fb59ebeef3?w=640&q=80",
            pop: 85, stock: 120, tags: "engine oil castrol", grades: ["Standard"], viscosity: "10W-40", volume: "4L");

        await AddPart("OAT Engine Coolant Concentrate 1L", "CLT-001", "PN-OIL-003", catOils.Id, null,
            180, 320, "OAT coolant concentrate, 1L. Mix 1:1 with distilled water. All engine types.",
            "OAT coolant concentrate 1L — freeze & corrosion protection",
            "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            pop: 75, stock: 60, tags: "coolant", grades: ["Standard"], volume: "1L");

        // FILTERS
        await AddPart("Mann Premium Oil Filter", "OF-001", "PN-FLT-001", catFilters.Id, mann.Id,
            180, 320, "Mann+Hummel oil filter. 20-micron filtration. Reliable bypass valve.",
            "Mann premium oil filter — 20-micron filtration",
            "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            featured: true, rank: 8, pop: 93, stock: 120, onSale: true, disc: 18, tags: "oil filter mann",
            grades: ["Standard", "Premium"]);

        await AddPart("Bosch High-Flow Air Filter", "AF-001", "PN-FLT-002", catFilters.Id, bosch.Id,
            220, 420, "Bosch OEM air filter. High-flow design. Blocks dust and contaminants.",
            "Bosch high-flow air filter — OEM quality",
            "https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=640&q=80",
            pop: 80, stock: 90, tags: "air filter bosch", grades: ["Standard", "Premium"]);

        await AddPart("Activated Carbon Cabin Air Filter", "CAF-001", "PN-FLT-003", catFilters.Id, mann.Id,
            280, 520, "Activated carbon cabin/pollen filter. Removes pollen, dust, bacteria and odours.",
            "Activated carbon cabin/pollen air filter",
            "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            pop: 70, stock: 70, tags: "cabin filter", grades: ["Standard"]);

        // BODY
        await AddPart("Universal Side Mirror Left", "MIR-L-001", "PN-BDY-001", catBody.Id, null,
            1200, 2100, "Universal-fit side mirror, driver side (left). Manual adjustment. Includes hardware.",
            "Universal left side mirror — includes mounting hardware",
            "https://images.unsplash.com/photo-1632912515069-45e9f2bf0b36?w=640&q=80",
            pop: 55, stock: 25, tags: "mirror side body", grades: ["Standard"], position: "Front");

        await context.SaveChangesAsync();

        var total = await context.Parts.CountAsync();
        logger.LogInformation("E-commerce catalog seeded: {Count} products", total);
    }

    // Runs on every startup — tops up stock to demo levels if all levels are currently 0.
    // This lets developers reset to 0 via SQL and the next restart will restore demo stock.
    private static async Task SeedDemoVehiclesAsync(AutoPartDbContext context, ILogger logger)
    {
        if (await context.Vehicles.AnyAsync())
        {
            logger.LogInformation("Vehicles already exist, skipping seed");
            return;
        }

        var vehicles = new[]
        {
            Vehicle.Create("Toyota",  "Corolla",  2019, "Petrol",  "Toyota Corolla 2019 1.6L Petrol"),
            Vehicle.Create("Toyota",  "Corolla",  2021, "Hybrid",  "Toyota Corolla 2021 1.8L Hybrid"),
            Vehicle.Create("Toyota",  "Hilux",    2020, "Diesel",  "Toyota Hilux 2020 2.4L Diesel"),
            Vehicle.Create("Toyota",  "Prado",    2022, "Diesel",  "Toyota Prado 2022 2.8L Diesel"),
            Vehicle.Create("Honda",   "Civic",    2018, "Petrol",  "Honda Civic 2018 1.5T Petrol"),
            Vehicle.Create("Honda",   "Civic",    2022, "Petrol",  "Honda Civic 2022 1.5T Petrol"),
            Vehicle.Create("Honda",   "CR-V",     2020, "Hybrid",  "Honda CR-V 2020 2.0L Hybrid"),
            Vehicle.Create("Suzuki",  "Swift",    2019, "Petrol",  "Suzuki Swift 2019 1.2L Petrol"),
            Vehicle.Create("Suzuki",  "Vitara",   2021, "Petrol",  "Suzuki Vitara 2021 1.4T Petrol"),
            Vehicle.Create("Nissan",  "X-Trail",  2020, "Diesel",  "Nissan X-Trail 2020 2.0L Diesel"),
            Vehicle.Create("Nissan",  "Navara",   2021, "Diesel",  "Nissan Navara 2021 2.5L Diesel"),
            Vehicle.Create("Mitsubishi", "L200",  2020, "Diesel",  "Mitsubishi L200 2020 2.4L Diesel"),
            Vehicle.Create("Ford",    "Ranger",   2021, "Diesel",  "Ford Ranger 2021 2.0L Diesel"),
            Vehicle.Create("BMW",     "3 Series", 2020, "Petrol",  "BMW 3 Series 2020 2.0L Petrol"),
            Vehicle.Create("Mercedes","C-Class",  2021, "Petrol",  "Mercedes C-Class 2021 2.0L Petrol"),
        };

        foreach (var v in vehicles) { v.CreatedBy = "System"; v.ModifiedBy = "System"; }
        context.Vehicles.AddRange(vehicles);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} demo vehicles", vehicles.Length);
    }

    private static async Task SeedDemoCompatibilitiesAsync(AutoPartDbContext context, ILogger logger)
    {
        if (await context.PartVehicleCompatibilities.AnyAsync())
        {
            logger.LogInformation("Part-vehicle compatibilities already exist, skipping seed");
            return;
        }

        var vehicles = await context.Vehicles
            .Where(v => v.IsActive && !v.Isdeleted)
            .ToListAsync();

        var parts = await context.Parts
            .Where(p => p.IsActive && !p.Isdeleted)
            .Take(30)
            .ToListAsync();

        if (!vehicles.Any() || !parts.Any())
        {
            logger.LogInformation("No vehicles or parts found — skipping compatibility seed");
            return;
        }

        // Link every seeded part to a relevant subset of vehicles
        var compatibilities = new List<PartVehicleCompatibility>();
        var rng = new Random(42); // fixed seed for deterministic output

        foreach (var part in parts)
        {
            // Each part is compatible with 3-8 random vehicles (realistic: parts don't fit all vehicles)
            var count = rng.Next(3, Math.Min(9, vehicles.Count + 1));
            var selected = vehicles.OrderBy(_ => rng.Next()).Take(count);

            foreach (var v in selected)
            {
                var compat = PartVehicleCompatibility.Create(part.Id, v.Id, isCompatible: true);
                compat.CreatedBy = "System";
                compat.ModifiedBy = "System";
                compatibilities.Add(compat);
            }
        }

        context.PartVehicleCompatibilities.AddRange(compatibilities);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} demo part-vehicle compatibilities", compatibilities.Count);
    }

    private static async Task EnsureDemoStockAsync(AutoPartDbContext context, ILogger logger)
    {
        var anyPartStock = await context.StockLevels
            .AnyAsync(sl => !sl.Isdeleted && sl.IsActive && sl.QuantityOnHand > 0);

        if (anyPartStock)
        {
            logger.LogInformation("Demo stock already present, skipping top-up");
            return;
        }

        logger.LogInformation("All stock levels are 0 — adding demo stock for development");

        // All stock now lives in StockLevels; variant-scoped rows have VariantId set.
        var allLevels = await context.StockLevels
            .Where(sl => !sl.Isdeleted && sl.IsActive)
            .ToListAsync();

        // Variant-scoped levels get 20; part-level (non-variant) levels get 50.
        var variantLevels = allLevels.Where(sl => sl.VariantId != null).ToList();
        var partLevels = allLevels.Where(sl => sl.VariantId == null).ToList();

        foreach (var sl in variantLevels)
            sl.AddStock(20);
        foreach (var sl in partLevels)
            sl.AddStock(50);

        if (allLevels.Any())
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Demo stock topped up: {V} variant levels, {P} part levels",
                variantLevels.Count, partLevels.Count);
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

