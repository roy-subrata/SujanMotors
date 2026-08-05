using System.Text;
using System.Text.Json.Serialization;
using AutoPartShop.Api.Middleware;
using AutoPartShop.Api.Hubs;
using AutoPartShop.Api.Services;
using AutoPartShop.Api.Services.HR;
using AutoPartShop.Application.Interfaces;
using Serilog;
using Serilog.Formatting.Compact;
using AutoPartShop.Application;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;
AutoPartShop.Api.Pdf.Design.DocFonts.Register();

var builder = WebApplication.CreateBuilder(args);

// --- Logging bootstrap (Serilog → console + Seq) ---
// Seq:Url is optional: leave it blank and logs go to the console only, so a local run
// doesn't need a Seq instance. Set it (e.g. "http://seq:5341" in Docker, env var Seq__Url)
// to also ship structured logs to the Seq UI.
var serviceName = builder.Configuration["Seq:ServiceName"] ?? "autopartshop-api";
var seqUrl = builder.Configuration["Seq:Url"];
var seqApiKey = builder.Configuration["Seq:ApiKey"];

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithProperty("Application", serviceName)
      .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
      .WriteTo.Console(new CompactJsonFormatter());

    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        lc.WriteTo.Seq(seqUrl, apiKey: string.IsNullOrWhiteSpace(seqApiKey) ? null : seqApiKey);
    }
});
// ---------------------------------------------------

// Configure CORS
// In Development we echo back any origin (required for SignalR negotiate with
// credentials: 'include' — AllowAnyOrigin() would send '*' and break it).
// In all other environments we restrict to an explicit allow-list supplied via
// configuration ("Cors:AllowedOrigins"), so production never trusts arbitrary origins.
var corsPolicy = "AllowAllApps";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AutoPartDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException(
        "JwtSettings:SecretKey is not configured. Provide it via user-secrets (dev), " +
        "environment variables, or a secrets vault. The API will not start without a signing key.");
}
var issuer = jwtSettings["Issuer"] ?? "AutoPartShopAPI";
var audience = jwtSettings["Audience"] ?? "AutoPartShopClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Reject tokens whose account was disabled after issue (e.g. HR offboarding).
    // IsActive is cached for 60s per user, so revocation is near-instant while the
    // added cost is at most one indexed lookup per user per minute.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userIdValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.Principal?.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
                return;

            var cache = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var isActive = await cache.GetOrCreateAsync($"user-active:{userId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                var db = context.HttpContext.RequestServices.GetRequiredService<AutoPartDbContext>();
                return await db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => (bool?)u.IsActive)
                    .FirstOrDefaultAsync() ?? false;
            });

            if (!isActive)
                context.Fail("Account is disabled");
        }
    };
});
builder.Services.AddMemoryCache();

// Recover the real client IP from X-Forwarded-For before anything partitions on it.
builder.Services.AddProxyForwardedHeaders(builder.Configuration);

// Request rate limiting — see Middleware/RateLimiting.cs for the tiers and their rationale.
builder.Services.AddApiRateLimiting(builder.Configuration);

// Add Authorization — permission policies ("permission:xxx") are built on demand and
// resolved against the RolePermissions table (Admin bypasses; see Api/Authorization).
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, AutoPartShop.Api.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AutoPartShop.Api.Authorization.PermissionAuthorizationHandler>();
builder.Services.AddScoped<IPermissionCheckService, PermissionCheckService>();

// Register HttpContextAccessor (required for CurrentUserService)
builder.Services.AddHttpContextAccessor();

// Register application services
// Shop business clock — resolves the shop's calendar day from UTC (Shop:TzOffsetMinutes).
// Singleton: it holds only the configured offset and reads DateTime.UtcNow per call.
builder.Services.AddSingleton<IShopClock, ShopClock>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// HR module's Api-layer adapter for Core's ICashierProfileService (see remarks on the interface
// and on CashierProfileService for why this is registered here rather than in Infrastructure's
// Dependency.cs — Infrastructure has no project reference back to Api).
builder.Services.AddScoped<ICashierProfileService, CashierProfileService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IShopProfileProvider, ShopProfileProvider>();
builder.Services.AddScoped<StockManagementService>();
builder.Services.AddScoped<StockAdjustmentApplier>();
// Shared stock-decrement algorithm for every channel that sells stock (POS quick sale,
// ecommerce checkout, in-store ecommerce checkout) — see IStockConsumptionService remarks.
builder.Services.AddScoped<IStockConsumptionService, StockConsumptionService>();
builder.Services.AddScoped<SupplierPaymentSummaryService>();
builder.Services.AddScoped<ISupplierLedgerService, SupplierLedgerService>();
builder.Services.AddScoped<ICustomerAccountSummaryService, CustomerAccountSummaryService>();
builder.Services.AddScoped<IUnitConversionService, UnitConversionService>();
builder.Services.AddScoped<IFinancialSummaryService, FinancialSummaryService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IDailyExpenseService, DailyExpenseService>();
builder.Services.AddScoped<IPricingValidationService, PricingValidationService>();

// Register multi-currency services
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
builder.Services.AddMemoryCache(); // Required for currency conversion caching

// Register warranty services
builder.Services.AddScoped<IWarrantyService, WarrantyService>();
builder.Services.AddScoped<IWarrantyClaimNotifier, WarrantyClaimNotifier>();

// Register discount and pricing services
builder.Services.AddScoped<IDiscountResolutionService, DiscountResolutionService>();

// Bulk product import (Excel)
builder.Services.AddScoped<IProductImportService, ProductImportService>();

// Configure JSON serialization to use camelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
           new JsonStringEnumConverter()
       );
    });
// SignalR for real-time staff notifications
builder.Services.AddSignalR();

// Allow SignalR to read JWT from query string (WebSocket/SSE can't set Authorization header)
builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        var existing = options.Events ?? new JwtBearerEvents();
        options.Events = existing;
        options.Events.OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) &&
                ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                ctx.Token = token;
            }
            return Task.CompletedTask;
        };
    });

// Broadcaster that adapts ISaleEventBroadcaster → IHubContext<SaleNotificationHub>
builder.Services.AddScoped<ISaleEventBroadcaster, SignalRSaleEventBroadcaster>();

// Reorder alerts: daily low-stock scan broadcast to staff over the same hub
builder.Services.AddScoped<IReorderAlertBroadcaster, SignalRReorderAlertBroadcaster>();
builder.Services.AddScoped<ReorderAlertScanner>();
builder.Services.AddHostedService<ReorderAlertService>();

// Automatic warranty expiry: marks ACTIVE warranties EXPIRED once their expiry date passes.
builder.Services.AddHostedService<WarrantyExpiryService>();

// Scheduled database backups (schedule read from BACKUP:* application settings)
builder.Services.AddHostedService<BackupSchedulerService>();

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

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed database with initial data
await DatabaseSeeder.SeedAsync(app.Services);

// Swagger exposes the full API surface; keep it out of production to avoid information disclosure.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoPart Shop API V1");
        c.RoutePrefix = "docs"; // set swagger path to /docs
    });
}

await app.ApplyMigration();

// Must run before anything reads the client IP (rate limiting, request logs): rewrites
// RemoteIpAddress from X-Forwarded-For so it is the caller, not the reverse proxy.
app.UseForwardedHeaders();
app.LogForwardedHeadersTrust(builder.Configuration);

// Enable CORS
app.UseCors(corsPolicy);
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

app.UseRouting();
app.UseAuthentication();

// Deliberately between authentication and authorization.
//
// After UseAuthentication so HttpContext.User is populated and authenticated callers
// partition by user id rather than by IP — a shop NATs every till through one address, and
// an IP partition would make cashiers throttle one another.
//
// Before UseAuthorization because that is what short-circuits with 401/403. Downstream of
// it, a caller spraying requests with a bogus token would never reach the limiter at all.
// UseAuthentication itself never short-circuits: it just leaves User unauthenticated, which
// falls back to the per-IP partition — exactly what an anonymous abuser should get.
app.UseRateLimiter();

app.UseAuthorization();
app.MapControllers();
app.MapHub<SaleNotificationHub>("/hubs/sale-notifications");

// Ping the service is live or not
app.MapGet("/live", () => "I am live");

app.Run();

internal class OpenApiReference
{
    public ReferenceType Type { get; set; }
    public string Id { get; set; } = string.Empty;
}
