using System.Text.Json;
using AutoPartShop.Api.Common;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argument exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiError.Validation(ex.Message, instance: context.Request.Path), _json));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Optimistic-concurrency conflict (RowVersion mismatch): another request changed the
            // record first. Surface a clean 409 so the client can reload and retry.
            _logger.LogWarning(ex, "Concurrency conflict on {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 409;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    ApiError.ConcurrencyConflict("This record was changed by another user. Please reload and try again.", context.Request.Path),
                    _json));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiError.Internal(context.TraceIdentifier), _json));
        }
    }
}
