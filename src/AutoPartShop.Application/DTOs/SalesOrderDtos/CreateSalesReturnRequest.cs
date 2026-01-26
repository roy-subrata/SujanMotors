namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

public class CreateSalesReturnRequest
{
    public Guid SalesOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RefundType { get; set; } = "CASH_REFUND"; // CASH_REFUND, STORE_CREDIT
    public string Notes { get; set; } = string.Empty;
    public List<CreateSalesReturnLineRequest> Lines { get; set; } = new();
}

public class CreateSalesReturnLineRequest
{
    public Guid SalesOrderLineId { get; set; }
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Condition { get; set; } = "UNOPENED"; // UNOPENED, OPENED, DAMAGED
    public string Notes { get; set; } = string.Empty;
}
