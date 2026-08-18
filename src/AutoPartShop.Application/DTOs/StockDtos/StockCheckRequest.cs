namespace AutoPartShop.Application.DTOs.StockDtos;

/// <summary>POS quick-sale stock availability probe for a single part.</summary>
public class StockCheckRequest
{
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }  // when set, checks the specific variant's stock
    public int Quantity { get; set; }
}

public class StockCheckResponse
{
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }

    /// <summary>
    /// False when no such (non-deleted) part exists. Without this a POS barcode typo is
    /// indistinguishable from a genuine stock-out — both report available:false, stockAvailable:0.
    /// </summary>
    public bool PartFound { get; set; } = true;

    public bool Available { get; set; }
    public int StockAvailable { get; set; }
    public string? WarehouseLocation { get; set; }
    public string? SupplierName { get; set; }
    public string? Message { get; set; }
}
