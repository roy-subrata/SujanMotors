namespace AutoPartShop.Application.DTOs.CustomerDtos;

public class CreateCustomerRequest
{
    public string CustomerCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AlternatePhone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string CustomerType { get; set; } = "RETAIL";
    public decimal CreditLimit { get; set; } = 0;
    public string TaxId { get; set; } = string.Empty;
    public string PrimaryContactPerson { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
