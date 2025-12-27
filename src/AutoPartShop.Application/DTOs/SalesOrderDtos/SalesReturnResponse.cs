namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

public class SalesReturnResponse
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // PENDING, APPROVED, RECEIVED, REJECTED, PROCESSED
    public decimal TotalRefundAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<SalesReturnLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SalesReturnLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
