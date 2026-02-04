namespace AutoPartShop.Application.DTOs.CustomerDtos;

public class UpdateCustomerRequest
{
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
    public string CustomerType { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public string PrimaryContactPerson { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
