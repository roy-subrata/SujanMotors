namespace AutoPartShop.Application.DTOs.SupplierDtos;

public class UpdateSupplierRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
