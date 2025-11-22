namespace AutoPartShop.Web.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Extension methods for registering services in the Web project
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Add category service with HttpClient configuration for Aspire
    /// </summary>
    public static IServiceCollection AddCategoryService(this IServiceCollection services, IHostEnvironment environment)
    {
        // Configure JSON serialization options to match API response format (camelCase)
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // Allow case-insensitive matching
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Expect camelCase from API
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignore null values
            WriteIndented = false // Compact JSON
        };

        // Register HttpClient for API communication
        services.AddHttpClient<ICategoryService, CategoryService>((provider, client) =>
        {
            // Get the API base address from configuration
            var config = provider.GetRequiredService<IConfiguration>();

            // Priority order:
            // 1. ApiBaseUrl - direct localhost/IP address (for standalone/dev)
            // 2. Services:api:http - Aspire service name (for AppHost)
            // 3. http://localhost:5000 - fallback default
            var apiBaseUrl = config["ApiBaseUrl"];

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                apiBaseUrl = config["Services:api:http"];
            }

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                apiBaseUrl = "http://localhost:5000";
            }

            // Ensure the URL is properly formatted (remove trailing slashes for consistency)
            apiBaseUrl = apiBaseUrl?.TrimEnd('/') ?? "http://localhost:5000";

            try
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "AutoPartShop-Web");

                // Add timeout
                client.Timeout = TimeSpan.FromSeconds(30);
            }
            catch (UriFormatException ex)
            {
                throw new InvalidOperationException(
                    $"Invalid API base URL configured: '{apiBaseUrl}'. " +
                    $"Please check configuration keys 'ApiBaseUrl' or 'Services:api:http'.",
                    ex);
            }
        });

        return services;
    }

    /// <summary>
    /// Add all application services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
        // Add category service
        services.AddCategoryService(environment);

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }
}
