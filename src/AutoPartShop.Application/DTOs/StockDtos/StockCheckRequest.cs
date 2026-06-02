namespace AutoPartShop.Application.DTOs.StockDtos;

/// <summary>POS quick-sale stock availability probe for a single part.</summary>
public class StockCheckRequest
{
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
}

public class StockCheckResponse
{
    public Guid PartId { get; set; }
    public bool Available { get; set; }
    public int StockAvailable { get; set; }
    public string? WarehouseLocation { get; set; }
    public string? SupplierName { get; set; }
    public string? Message { get; set; }
}
