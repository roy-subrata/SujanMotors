namespace AutoPartShop.Application.DTOs.Notification;

/// <summary>Daily staff broadcast listing stock items at/below their reorder level.</summary>
public class ReorderAlertEvent
{
    /// <summary>Total items needing reorder (may exceed Items.Count, which is capped).</summary>
    public int ItemCount { get; set; }
    public List<ReorderAlertItem> Items { get; set; } = new();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class ReorderAlertItem
{
    public Guid StockLevelId { get; set; }
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
}
