namespace AutoPartShop.Application.Suppliers.Dtos;

/// <summary>
/// Supplier quality/performance metrics derived from accepted goods receipts and purchase returns.
/// </summary>
public class SupplierPerformanceResponse
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public int GrnCount { get; set; }              // Number of accepted goods receipts
    public int TotalReceivedQty { get; set; }      // Units received across those GRNs
    public int TotalRejectedQty { get; set; }      // Damaged/rejected units
    public int TotalAcceptedQty { get; set; }      // Received - rejected
    public int ReturnCount { get; set; }           // Purchase returns raised against the supplier
    public decimal DamagedRatePct { get; set; }    // TotalRejected / TotalReceived * 100, rounded to 2dp
}
