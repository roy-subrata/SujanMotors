namespace AutoPartShop.Application.DTOs.SupplierDtos;

public class CreateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = "NET30";
    public decimal CreditLimit { get; set; } = 0;
}
