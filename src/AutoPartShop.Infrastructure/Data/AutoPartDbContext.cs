

using System.Security.Claims;
using AutoPartShop.Domain.Entities;
using AutoPartsShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class AutoPartDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, IdentityUserClaim<Guid>, ApplicationUserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    // Optional so the design-time factory (migrations) can still construct the context with options only.
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<AutoPartDbContext>? _logger;

    public AutoPartDbContext(
        DbContextOptions<AutoPartDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger<AutoPartDbContext>? logger = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the authenticated username for audit stamping, falling back to "system" for
    /// background work or unauthenticated requests (e.g. seeding, migrations).
    /// </summary>
    private string ResolveCurrentUser()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return "system";

        return user.Identity!.Name
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? "system";
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
#pragma warning disable CS0618 // InvoicePayment is obsolete; DbSet retained for existing DB table
    public DbSet<InvoicePayment> InvoicePayments { get; set; }
#pragma warning restore CS0618
    public DbSet<SalesReturn> SalesReturns { get; set; }
    public DbSet<PurchaseReturn> PurchaseReturns { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerVehicle> CustomerVehicles { get; set; }
    public DbSet<PaymentProvider> PaymentProviders { get; set; }
    public DbSet<CustomerPayment> CustomerPayments { get; set; }
    public DbSet<SupplierPayment> SupplierPayments { get; set; }
    public DbSet<StockLot> StockLots { get; set; }
    public DbSet<StockLotMovement> StockLotMovements { get; set; }
    public DbSet<Technician> Technicians { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<PayrollRun> PayrollRuns { get; set; }
    public DbSet<Payslip> Payslips { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<SalaryAdvance> SalaryAdvances { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ProductLocation> ProductLocations { get; set; }
    public DbSet<DailyExpense> DailyExpenses { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<ApplicationSettings> ApplicationSettings { get; set; }
    public DbSet<WarrantyRegistration> WarrantyRegistrations { get; set; }
    public DbSet<WarrantyClaim> WarrantyClaims { get; set; }
    public DbSet<WarrantyClaimEvent> WarrantyClaimEvents { get; set; }
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

    // Uploaded binaries (product media, employee photos/documents)
    public DbSet<StoredFile> StoredFiles { get; set; }


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
        var currentUser = ResolveCurrentUser();
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

        // Persist the business changes first and return their result. Audit logging is a
        // deliberately DECOUPLED, best-effort second step: a failure to write the audit trail
        // (e.g. table missing during first-run setup, or a transient error) must never roll back
        // or fail the business operation. When this runs inside a caller's transaction the audit
        // rows still enlist in it, so they commit/rollback together with the business data.
        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditLogs.Count > 0)
        {
            try
            {
                await AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
                await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Swallow: audit logging is best-effort and must not break the business operation.
                if (_logger != null)
                    _logger.LogWarning(ex, "Audit logging failed for {Count} change(s); business operation already committed.", auditLogs.Count);
                else
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
