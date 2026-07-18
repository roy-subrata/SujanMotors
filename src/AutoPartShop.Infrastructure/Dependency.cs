
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Events;
using AutoPartShop.Infrastructure.Events;
using AutoPartShop.Infrastructure.Services.Providers;
using AutoPartShop.Application.Brands;
using AutoPartShop.Application.Categories;
using AutoPartShop.Application.Catgories;
using AutoPartShop.Application.Catalog;
using AutoPartShop.Application.CustomerPayment;
using AutoPartShop.Application.Customers;
using AutoPartShop.Application.Parts;
using AutoPartShop.Application.PurchaseOrders;
using AutoPartShop.Application.Reports;
using AutoPartShop.Application.SaleOrders;
using AutoPartShop.Application.Services;
using AutoPartShop.Application.Stock;
using AutoPartShop.Application.Supplier;
using AutoPartShop.Application.Suppliers;
using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Technecians;
using AutoPartShop.Application.Warehouse;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
using AutoPartShop.Infrastructure.Services;
using AutoPartShop.Infrastructure.Services.Backup;
using AutoPartShop.Infrastructure.Services.Embedding;
using AutoPartShop.Infrastructure.Services.Storage;
using AutoPartsShop.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
public static class Dependency
{
    public static async Task<IApplicationBuilder> ApplyMigration(this IApplicationBuilder builder)
    {
        using (var scope = builder.ApplicationServices.CreateScope())
        {
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoPartDbContext>();
            if (env.IsDevelopment())
            {
                // In Development, apply any pending migrations automatically
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                // In Production, you might want to handle migrations differently
                // For example, log a warning or throw an exception
                // Here, we'll just ensure the database is created
                //   dbContext.Database.EnsureCreated();
            }
        }
        return builder;
    }
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddDbContext<AutoPartDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("AutoPartDb"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,          // Retry 5 times on transient failures
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null    // null = default transient errors
                    );
                });
            // Enable detailed logging in Development only — these expose query
            // parameter values (PII, payment amounts) and must never run in production.
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Register repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IUnitConversionRepository, UnitConversionRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IPartVehicleCompatibilityRepository, PartVehicleCompatibilityRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IStockLevelRepository, StockLevelRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IChallanRepository, ChallanRepository>();
        services.AddScoped<ISalesReturnRepository, SalesReturnRepository>();
        services.AddScoped<IPurchaseReturnRepository, PurchaseReturnRepository>();
        services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
        services.AddScoped<ICustomerCreditNoteRepository, CustomerCreditNoteRepository>();
        services.AddScoped<ICustomerDebitNoteRepository, CustomerDebitNoteRepository>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        services.AddScoped<IProformaInvoiceRepository, ProformaInvoiceRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerVehicleRepository, CustomerVehicleRepository>();
        services.AddScoped<IPaymentProviderRepository, PaymentProviderRepository>();
        services.AddScoped<ICustomerPaymentRepository, CustomerPaymentRepository>();
        services.AddScoped<ISupplierPaymentRepository, SupplierPaymentRepository>();
        services.AddScoped<ISupplierPaymentAccountRepository, SupplierPaymentAccountRepository>();
        services.AddScoped<IStockLotRepository, StockLotRepository>();
        services.AddScoped<IStockLotMovementRepository, StockLotMovementRepository>();
        services.AddScoped<ITechnicianRepository, TechnicianRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<IPayrollRepository, PayrollRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<ISalaryAdvanceRepository, SalaryAdvanceRepository>();
        services.AddScoped<IProductLocationRepository, ProductLocationRepository>();
        services.AddScoped<IDailyExpenseRepository, DailyExpenseRepository>();
        services.AddScoped<IProductMediaRepository, ProductMediaRepository>();
        services.AddScoped<IStoredFileRepository, StoredFileRepository>();

        // Uploaded file blobs (product media, employee photos/documents).
        // Local disk today; an S3-compatible implementation (Cloudflare R2 / MinIO)
        // can replace this via "FileStorage:Provider" without touching callers.
        services.AddSingleton<IFileStorageService, LocalDiskFileStorage>();

        // Multi-currency repositories
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<IApplicationSettingsRepository, ApplicationSettingsRepository>();

        // Warranty repositories
        services.AddScoped<IWarrantyRegistrationRepository, WarrantyRegistrationRepository>();
        services.AddScoped<IWarrantyClaimRepository, WarrantyClaimRepository>();
        services.AddScoped<IWarrantyClaimEventRepository, WarrantyClaimEventRepository>();

        // Discount repositories
        services.AddScoped<IDiscountRepository, DiscountRepository>();

        // Variant pricing repository
        services.AddScoped<IProductVariantPriceHistoryRepository, ProductVariantPriceHistoryRepository>();

        // Promotes future-dated scheduled prices into the denormalized SellingPrice columns once effective.
        services.AddHostedService<ScheduledPriceSyncService>();

        // Product semantic search: embedding store + embedding generator (OpenAI-compatible, JSON-configured)
        services.AddScoped<IProductEmbeddingRepository, ProductEmbeddingRepository>();
        services.AddHttpClient<IEmbeddingService, OpenAiCompatibleEmbeddingService>();


        //Application
        services.AddScoped<IPurchaseOrderReadRepository, PurchaseOrderReadRepository>();


        //Services
        services.AddTransient<ICodeGenerateService, CodeGenerateService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();

        // Notification providers — gracefully no-op when not configured.
        // SMS provider is selectable via "Sms:Provider" (smsnetbd | twilio); defaults to
        // smsnetbd when an "Sms:ApiKey" is present, otherwise Twilio.
        var smsProvider = (configuration["Sms:Provider"]
            ?? (!string.IsNullOrWhiteSpace(configuration["Sms:ApiKey"]) ? "smsnetbd" : "twilio"))
            .Trim().ToLowerInvariant();
        if (smsProvider == "smsnetbd")
            services.AddHttpClient<ISmsProvider, SmsNetBdSmsProvider>();
        else
            services.AddScoped<ISmsProvider, TwilioSmsProvider>();

        services.AddScoped<IWhatsAppProvider, TwilioWhatsAppProvider>();
        services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        services.AddScoped<INotificationService, NotificationService>();

        // Domain event infrastructure
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<SaleOrderConfirmedEvent>, SaleOrderConfirmedNotificationHandler>();
        services.AddScoped<IDomainEventHandler<CustomerPaymentDueEvent>, CustomerPaymentDueNotificationHandler>();

        //Read Repository
        services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();
        services.AddScoped<ICustomerPaymentReadRepository, CustomerPaymentReadRepository>();
        services.AddScoped<ICategoryReadRepository, CategoryReadRepository>();
        services.AddScoped<IBrandReadRepository, BrandReadRepository>();

        services.AddScoped<ISupplierReadRepository, SupplierReadRepository>();
        services.AddScoped<ISupplierPerformanceReadRepository, SupplierPerformanceReadRepository>();
        services.AddScoped<ISupplierPaymentReadRespository, SupplierPaymentReadRespository>();

        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddScoped<ICatalogReadRepository, CatalogReadRepository>();
        services.AddScoped<IPurchaseOrderReadRepository, PurchaseOrderReadRepository>();

        services.AddScoped<ISaleOrderReadRepository, SaleOrderReadRepository>();

        services.AddScoped<IStockLevelReadRepository, StockLevelReadRepository>();
        services.AddScoped<IStockMovementReadRepository, StockMovementReadRepository>();
        services.AddScoped<IStockLotReadRepository, StockLotReadRepository>();

        services.AddScoped<ITechnecianReadRepository, TechnecianReadRepository>();
        services.AddScoped<IEmployeeReadRepository, EmployeeReadRepository>();
        services.AddScoped<IAttendanceReadRepository, AttendanceReadRepository>();
        services.AddScoped<ILeaveRequestReadRepository, LeaveRequestReadRepository>();
        services.AddScoped<ISalaryAdvanceReadRepository, SalaryAdvanceReadRepository>();
        services.AddScoped<IHrSalesReadRepository, HrSalesReadRepository>();
        services.AddScoped<IWarehouseReadRepository, WarehouseReadRepository>();

        services.AddScoped<IReportReadRepository, ReportReadRepository>();

        // Database backup: native BACKUP/RESTORE + Google Drive upload
        services.AddScoped<IBackupRecordRepository, BackupRecordRepository>();
        services.AddSingleton<BackupCoordinator>();
        services.AddSingleton<IBackupStorage, GoogleDriveBackupStorage>();
        services.AddScoped<IBackupService, SqlServerBackupService>();

        return services;
    }
}
