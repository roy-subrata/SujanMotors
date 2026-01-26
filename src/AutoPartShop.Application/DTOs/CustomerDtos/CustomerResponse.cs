namespace AutoPartShop.Application.DTOs.CustomerDtos;

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
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
    public string Status { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal AdvanceAmount { get; set; }
    public decimal DueAmount { get; set; }
    public bool CanPlaceOrder { get; set; }
    public string PrimaryContactPerson { get; set; } = string.Empty;
    public DateTime? LastPurchaseDate { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
