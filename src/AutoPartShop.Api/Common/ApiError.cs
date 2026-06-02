namespace AutoPartShop.Api.Common;

public sealed class ApiError
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string? Instance { get; init; }
    public string? TraceId { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Backward-compatibility alias for <see cref="Detail"/>, serialized as "message".
    /// The Angular client reads <c>error.message</c> in many places; emitting it keeps
    /// existing error handling working as controllers migrate to ApiError. New clients
    /// should prefer <c>detail</c> (or use the shared extractApiError helper).
    /// </summary>
    public string Message => Detail;

    public static ApiError NotFound(string detail, string? instance = null) => new()
    {
        Type = "NOT_FOUND", Title = "Resource not found",
        Status = 404, Detail = detail, Instance = instance
    };

    public static ApiError Validation(string detail, IDictionary<string, string[]>? errors = null, string? instance = null) => new()
    {
        Type = "VALIDATION_ERROR", Title = "Validation failed",
        Status = 400, Detail = detail, Errors = errors, Instance = instance
    };

    public static ApiError Unauthorized(string detail, string? instance = null) => new()
    {
        Type = "UNAUTHORIZED", Title = "Authentication failed",
        Status = 401, Detail = detail, Instance = instance
    };

    public static ApiError Conflict(string detail, string? instance = null) => new()
    {
        Type = "CONFLICT", Title = "Conflict",
        Status = 409, Detail = detail, Instance = instance
    };

    public static ApiError BusinessRule(string detail, string? instance = null) => new()
    {
        Type = "BUSINESS_RULE_VIOLATION", Title = "Business rule violated",
        Status = 422, Detail = detail, Instance = instance
    };

    public static ApiError Internal(string? traceId = null) => new()
    {
        Type = "INTERNAL_ERROR", Title = "An unexpected error occurred",
        Status = 500, Detail = "An internal server error occurred. Please try again later.",
        TraceId = traceId
    };
}
