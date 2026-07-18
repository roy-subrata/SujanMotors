namespace AutoPartShop.Application.DTOs.ProformaInvoiceDtos;

public class CreateProformaInvoiceRequest
{
    public Guid SalesOrderId { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class ProformaInvoiceResponse
{
    public Guid Id { get; set; }
    public string ProformaNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string SONumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
