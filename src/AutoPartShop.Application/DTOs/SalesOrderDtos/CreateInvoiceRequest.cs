namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

public class CreateInvoiceRequest
{
    public Guid SalesOrderId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
