namespace AutoPartShop.Application.DTOs.AuditDtos;

public class AuditLogResponse
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class AuditLogSummary
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public int ChangesCount { get; set; }
    public List<PropertyChange> Changes { get; set; } = new();
}

public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class AuditStatistics
{
    public int TotalChanges { get; set; }
    public int InsertCount { get; set; }
    public int UpdateCount { get; set; }
    public int DeleteCount { get; set; }
    public DateTime? FirstChangeDate { get; set; }
    public DateTime? LastChangeDate { get; set; }
    public List<EntityChangeCount> EntityChanges { get; set; } = new();
    public List<UserActivityCount> UserActivities { get; set; } = new();
}

public class EntityChangeCount
{
    public string EntityName { get; set; } = string.Empty;
    public int ChangeCount { get; set; }
}

public class UserActivityCount
{
    public string UserName { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
}
