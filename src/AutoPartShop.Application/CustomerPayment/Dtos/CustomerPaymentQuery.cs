using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.CustomerPayment.Dtos;

public class CustomerPaymentQuery : BaseQuery
{
    public bool? IsReconciled { get; set; }
    public string? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}


