using AutoPartsShop.Web.Components;
using AutoPartShop.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var config = builder.Configuration;

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add application services (Category service, etc.)
builder.Services.AddApplicationServices(builder.Environment, builder.Configuration);

// Configure HttpClient for API communication
// Get API base URL from configuration, with Aspire naming convention
var apiBaseUrl = config["Services:api:http"] ?? config["ApiBaseUrl"] ?? "http://localhost:5000";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
