

using AutoPartShop.Domain.Entities;
using AutoPartsShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

public class AutoPartDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, IdentityUserClaim<Guid>, ApplicationUserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    public AutoPartDbContext(DbContextOptions<AutoPartDbContext> options)
        : base(options)
    {
    }

    public DbSet<CodeSequence> CodeSequences { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<UnitConversion> UnitConversions { get; set; }
    public DbSet<Product> Parts { get; set; }
    public DbSet<ProductCatalogEntry> ProductCatalogEntries { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<ProductAttributeGroup> ProductAttributeGroups { get; set; }
    public DbSet<ProductAttribute> ProductAttributes { get; set; }
    public DbSet<ProductAttributeOption> ProductAttributeOptions { get; set; }
    public DbSet<CategoryAttribute> CategoryAttributes { get; set; }
    public DbSet<VariantAttributeValue> VariantAttributeValues { get; set; }
    public DbSet<ProductMedia> ProductMedias { get; set; }
    public DbSet<CompatibilityRule> CompatibilityRules { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<PartVehicleCompatibility> PartVehicleCompatibilities { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<StockLevel> StockLevels { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public DbSet<SalesOrder> SalesOrders { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoicePayment> InvoicePayments { get; set; }
    public DbSet<SalesReturn> SalesReturns { get; set; }
    public DbSet<PurchaseReturn> PurchaseReturns { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<PaymentProvider> PaymentProviders { get; set; }
    public DbSet<CustomerPayment> CustomerPayments { get; set; }
    public DbSet<SupplierPayment> SupplierPayments { get; set; }
    public DbSet<StockLot> StockLots { get; set; }
    public DbSet<StockLotMovement> StockLotMovements { get; set; }
    public DbSet<Technician> Technicians { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ProductLocation> ProductLocations { get; set; }
    public DbSet<DailyExpense> DailyExpenses { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<ApplicationSettings> ApplicationSettings { get; set; }
    public DbSet<WarrantyRegistration> WarrantyRegistrations { get; set; }
    public DbSet<WarrantyClaim> WarrantyClaims { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentLine> ShipmentLines { get; set; }
    public DbSet<Challan> Challans { get; set; }
    public DbSet<ChallanLine> ChallanLines { get; set; }
    public DbSet<CartReservation> CartReservations { get; set; }
    public DbSet<SupplierPaymentAccount> SupplierPaymentAccounts { get; set; }
    public DbSet<CreditNote> CreditNotes { get; set; }
    public DbSet<CustomerCreditNote> CustomerCreditNotes { get; set; }

    // Discount system
    public DbSet<Discount> Discounts { get; set; }

    // Variant time-based pricing
    public DbSet<ProductVariantPriceHistory> ProductVariantPriceHistories { get; set; }

    // Product semantic-search embeddings (SQL Server 2025 native vector)
    public DbSet<ProductEmbedding> ProductEmbeddings { get; set; }

    // Notification audit trail
    public DbSet<NotificationLog> NotificationLogs { get; set; }


    // Identity and Permission tables
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations for each entity
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutoPartDbContext).Assembly);

        // Configure Identity tables
        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("Users");
            b.HasMany(e => e.UserRoles)
                .WithOne(e => e.User)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();
        });

        modelBuilder.Entity<ApplicationRole>(b =>
        {
            b.ToTable("Roles");
            b.HasMany(e => e.UserRoles)
                .WithOne(e => e.Role)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();
        });

        modelBuilder.Entity<ApplicationUserRole>(b =>
        {
            b.ToTable("UserRoles");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("UserClaims");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("UserLogins");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b =>
        {
            b.ToTable("RoleClaims");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("UserTokens");
        });

        // Configure Permission entity
        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("Permissions");
            b.HasIndex(p => p.Name).IsUnique();
        });

        // Configure RolePermission entity
        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("RolePermissions");
            b.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

            b.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Additional configurations can be added here if needed
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditLogs = new List<AuditLog>();
        var currentUser = "system"; // TODO: Get from IHttpContextAccessor or similar
        var currentTime = DateTime.UtcNow;

        // Track changes for audit logging
        var auditableEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in auditableEntries)
        {
            // Skip AuditLog entity itself to avoid infinite recursion
            if (entry.Entity is AuditLog)
                continue;

            var entityName = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);
            var action = entry.State switch
            {
                EntityState.Added => "INSERT",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "UNKNOWN"
            };

            // Update audit fields for AuditableEntity
            if (entry.Entity is AuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.CreatedDate = currentTime;
                    auditableEntity.ModifiedDate = currentTime;
                    auditableEntity.CreatedBy = currentUser;
                    auditableEntity.ModifiedBy = currentUser;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableEntity.ModifiedDate = currentTime;
                    auditableEntity.ModifiedBy = currentUser;
                }
            }

            // Capture property changes
            if (entry.State == EntityState.Added)
            {
                // For INSERT, log all non-null values
                foreach (var property in entry.CurrentValues.Properties)
                {
                    var currentValue = entry.CurrentValues[property];
                    if (currentValue != null)
                    {
                        auditLogs.Add(new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            EntityName = entityName,
                            EntityId = entityId,
                            Action = action,
                            PropertyName = property.Name,
                            OldValue = null,
                            NewValue = currentValue.ToString(),
                            PerformedBy = currentUser,
                            PerformedAt = currentTime
                        });
                    }
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // For UPDATE, log only changed properties
                foreach (var property in entry.Properties.Where(p => p.IsModified))
                {
                    var oldValue = entry.OriginalValues[property.Metadata.Name];
                    var newValue = entry.CurrentValues[property.Metadata.Name];

                    auditLogs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityName = entityName,
                        EntityId = entityId,
                        Action = action,
                        PropertyName = property.Metadata.Name,
                        OldValue = oldValue?.ToString(),
                        NewValue = newValue?.ToString(),
                        PerformedBy = currentUser,
                        PerformedAt = currentTime
                    });
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                // For DELETE, log all values
                foreach (var property in entry.OriginalValues.Properties)
                {
                    var originalValue = entry.OriginalValues[property];
                    if (originalValue != null)
                    {
                        auditLogs.Add(new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            EntityName = entityName,
                            EntityId = entityId,
                            Action = action,
                            PropertyName = property.Name,
                            OldValue = originalValue.ToString(),
                            NewValue = null,
                            PerformedBy = currentUser,
                            PerformedAt = currentTime
                        });
                    }
                }
            }
        }

        // Save changes first
        var result = await base.SaveChangesAsync(cancellationToken);

        // Add audit logs if any (skip during initial setup when AuditLogs table doesn't exist yet)
        if (auditLogs.Any())
        {
            try
            {
                await AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
                await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Log but don't fail the operation if audit logging fails
                Console.WriteLine($"Audit logging failed: {ex.Message}");
            }
        }

        return result;
    }

    private string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue?.ToString() ?? "Unknown";
    }
}
