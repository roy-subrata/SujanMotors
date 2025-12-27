namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks all changes made to entities in the system
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;  // Table/Entity name
    public string EntityId { get; set; } = string.Empty;  // Primary key of the entity
    public string Action { get; set; } = string.Empty;  // INSERT, UPDATE, DELETE
    public string PropertyName { get; set; } = string.Empty;  // Name of the changed property
    public string? OldValue { get; set; }  // Previous value (null for INSERT)
    public string? NewValue { get; set; }  // New value (null for DELETE)
    public string PerformedBy { get; set; } = string.Empty;  // User who made the change
    public DateTime PerformedAt { get; set; }  // When the change was made
    public string? IpAddress { get; set; }  // Optional: IP address of the user
    public string? UserAgent { get; set; }  // Optional: Browser/client info
}
