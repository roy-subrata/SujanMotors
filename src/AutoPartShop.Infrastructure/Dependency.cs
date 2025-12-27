
using System.Threading.Tasks;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
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
        services.AddScoped<IStockLotRepository, StockLotRepository>();
        services.AddScoped<IStockLotMovementRepository, StockLotMovementRepository>();
        services.AddScoped<ITechnicianRepository, TechnicianRepository>();
        services.AddScoped<IProductLocationRepository, ProductLocationRepository>();

        return services;
    }
}