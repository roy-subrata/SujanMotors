namespace AutoPartShop.Web.Services;

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
        // Register HttpClient for API communication
        services.AddHttpClient<ICategoryService, CategoryService>((provider, client) =>
        {
            // Get the API base address from configuration
            var config = provider.GetRequiredService<IConfiguration>();
            var apiBaseUrl = config["Services:api:http"];

            // Fallback to ApiBaseUrl if Aspire config not found
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                apiBaseUrl = config["ApiBaseUrl"];
            }

            // Final fallback to localhost
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
                    $"Please check configuration keys 'Services:api:http' or 'ApiBaseUrl'.",
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
