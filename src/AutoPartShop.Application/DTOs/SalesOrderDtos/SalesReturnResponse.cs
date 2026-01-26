namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

public class SalesReturnResponse
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; } // Sales Order Number
    public Guid WarehouseId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // PENDING, APPROVED, RECEIVED, REJECTED, PROCESSED
    public decimal TotalRefundAmount { get; set; }
    public string RefundType { get; set; } = "CASH_REFUND"; // CASH_REFUND, STORE_CREDIT
    public string Notes { get; set; } = string.Empty;
    public List<SalesReturnLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SalesReturnLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
