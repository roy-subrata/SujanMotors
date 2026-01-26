namespace AutoPartShop.Application.DTOs.WarrantyDtos;

public class CreateWarrantyRegistrationRequest
{
    public Guid PartId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime SaleDate { get; set; }
    public DateTime WarrantyStartDate { get; set; }
    public int WarrantyPeriodMonths { get; set; }
    public string WarrantyType { get; set; } = string.Empty;
    public string WarrantyTerms { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
}
