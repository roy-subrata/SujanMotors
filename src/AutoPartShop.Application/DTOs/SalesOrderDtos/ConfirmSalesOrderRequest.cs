namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

/// <summary>
/// Optional body for the PATCH /salesorder/{id}/confirm endpoint.
/// If ManualAllocations is null or empty, FIFO is used for all lines.
/// </summary>
public class ConfirmSalesOrderRequest
{
    /// <summary>
    /// Per-line manual lot selections. Only needed when the user wants to
    /// pick specific lots instead of relying on FIFO.
    /// Lines not listed here will fall back to FIFO automatically.
    /// </summary>
    public List<LineManualAllocation>? ManualAllocations { get; set; }
}

public class LineManualAllocation
{
    public Guid SalesOrderLineId { get; set; }
    public List<LotAllocationRequest> Lots { get; set; } = new();
}

public class LotAllocationRequest
{
    public Guid StockLotId { get; set; }
    public int Quantity { get; set; }
}

