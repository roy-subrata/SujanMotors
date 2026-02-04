
using AutoPartShop.Application.Categories;
using AutoPartShop.Application.Catgories;
using AutoPartShop.Application.CustomerPayment;
using AutoPartShop.Application.Customers;
using AutoPartShop.Application.Parts;
using AutoPartShop.Application.PurchaseOrders;
using AutoPartShop.Application.SaleOrders;
using AutoPartShop.Application.Services;
using AutoPartShop.Application.Stock;
using AutoPartShop.Application.Supplier;
using AutoPartShop.Application.Suppliers;
using AutoPartShop.Application.Technecians;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
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
            // Enable detailed logging in Development only
            options.EnableSensitiveDataLogging();   // Only in dev
            options.EnableDetailedErrors();         // Useful for debugging

        });

        // Register repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IUnitConversionRepository, UnitConversionRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
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
        services.AddScoped<ISalesReturnRepository, SalesReturnRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
        services.AddScoped<IPurchaseReturnRepository, PurchaseReturnRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPaymentProviderRepository, PaymentProviderRepository>();
        services.AddScoped<ICustomerPaymentRepository, CustomerPaymentRepository>();
        services.AddScoped<ISupplierPaymentRepository, SupplierPaymentRepository>();
        services.AddScoped<ISupplierPaymentAccountRepository, SupplierPaymentAccountRepository>();
        services.AddScoped<IStockLotRepository, StockLotRepository>();
        services.AddScoped<IStockLotMovementRepository, StockLotMovementRepository>();
        services.AddScoped<ITechnicianRepository, TechnicianRepository>();
        services.AddScoped<IProductLocationRepository, ProductLocationRepository>();
        services.AddScoped<IDailyExpenseRepository, DailyExpenseRepository>();

        // Multi-currency repositories
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<IApplicationSettingsRepository, ApplicationSettingsRepository>();

        // Warranty repositories
        services.AddScoped<IWarrantyRegistrationRepository, WarrantyRegistrationRepository>();
        services.AddScoped<IWarrantyClaimRepository, WarrantyClaimRepository>();

        //Application
        services.AddScoped<IPurchaseOrderReadRepository, PurchaseOrderReadRepository>();


        //Services
        services.AddTransient<ICodeGenerateService, CodeGenerateService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        //Read Repository
        services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();
        services.AddScoped<ICustomerPaymentReadRepository, CustomerPaymentReadRepository>();
        services.AddScoped<ICategoryReadRepository, CategoryReadRepository>();

        services.AddScoped<ISupplierReadRepository, SupplierReadRepository>();
        services.AddScoped<ISupplierPaymentReadRespository, SupplierPaymentReadRespository>();

        services.AddScoped<IPartReadRepository, PartReadRepository>();
        services.AddScoped<IPurchaseOrderReadRepository, PurchaseOrderReadRepository>();

        services.AddScoped<ISaleOrderReadRepository, SaleOrderReadRepository>();

        services.AddScoped<IStockLevelReadRepository, StockLevelReadRepository>();
        services.AddScoped<IStockMovementReadRepository, StockMovementReadRepository>();
        services.AddScoped<IStockLotReadRepository, StockLotReadRepository>();

        services.AddScoped<ITechnecianReadRepository, TechnecianReadRepository>();

        return services;
    }
}
