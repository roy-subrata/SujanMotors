
using AutoPartShop.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static ICodeGenerateService;

namespace AutoPartShop.Application;


public static class Dependency
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ICodeGenerateService, CodeGenerateService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        return services;
    }
}