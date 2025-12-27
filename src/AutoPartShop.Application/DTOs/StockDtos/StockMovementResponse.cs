namespace AutoPartShop.Application.DTOs.StockDtos;

public class StockMovementResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // PENDING, APPROVED, REJECTED
    public string Notes { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
