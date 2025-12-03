using Microsoft.OpenApi;
using AutoPartShop.Infrastructure.Repositories;
using AutoPartShop.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS
var corsPolicy = "AllowAllApps";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // For development, allow any origin without credentials
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // For production, allow specific origins
            policy.WithOrigins(
                // Angular App
                "http://localhost:4200",  // Angular dev server
                "https://localhost:4200", // Angular dev server HTTPS
                // Blazor App
                "http://localhost:5173",  // Blazor dev server default
                "http://localhost:5174",  // Alternative Blazor port
                "https://localhost:7173", // HTTPS Blazor dev
                "https://localhost:7174", // HTTPS Blazor dev alternative
                "http://localhost:7020",  // Aspire Blazor app port
                "https://localhost:5109"  // HTTPS Blazor dev alternative
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        }
    });
});

// Register repositories
builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
builder.Services.AddSingleton<IUnitRepository, UnitRepository>();
builder.Services.AddSingleton<IUnitConversionRepository, UnitConversionRepository>();
builder.Services.AddSingleton<IPartRepository, PartRepository>();
builder.Services.AddSingleton<IVehicleRepository, VehicleRepository>();
builder.Services.AddSingleton<IPartVehicleCompatibilityRepository, PartVehicleCompatibilityRepository>();
builder.Services.AddSingleton<ISupplierRepository, SupplierRepository>();
builder.Services.AddSingleton<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddSingleton<IStockLevelRepository, StockLevelRepository>();
builder.Services.AddSingleton<IStockMovementRepository, StockMovementRepository>();
builder.Services.AddSingleton<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddSingleton<IGoodsReceiptRepository, GoodsReceiptRepository>();
builder.Services.AddSingleton<ISalesOrderRepository, SalesOrderRepository>();
builder.Services.AddSingleton<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddSingleton<ISalesReturnRepository, SalesReturnRepository>();
builder.Services.AddSingleton<IPriceHistoryRepository, PriceHistoryRepository>();
builder.Services.AddSingleton<IPurchaseReturnRepository, PurchaseReturnRepository>();
builder.Services.AddSingleton<ICustomerRepository, CustomerRepository>();
builder.Services.AddSingleton<IPaymentProviderRepository, PaymentProviderRepository>();
builder.Services.AddSingleton<ICustomerPaymentRepository, CustomerPaymentRepository>();
builder.Services.AddSingleton<ISupplierPaymentRepository, SupplierPaymentRepository>();
builder.Services.AddSingleton<IStockLotRepository, StockLotRepository>();
builder.Services.AddSingleton<IStockLotMovementRepository, StockLotMovementRepository>();

// Register application services
builder.Services.AddScoped<StockManagementService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    // This line reads the XML file you generated in the build folder
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AutoPart Shop",
        Version = "v1",
        Description = "AutoPart Shop API with Swagger & JWT"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoPart Shop API V1");
        c.RoutePrefix = "docs"; // set swagger path to /docs
    });
}

// Enable CORS
app.UseCors(corsPolicy);

// Only redirect HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.MapControllers();

// Ping the service is live or not
app.MapGet("/live", () => "I am live");

app.Run();
