using AutoPartShop.Application.Common;
using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Application.CustomerPayment.Dtos;

public class CustomerPaymentQuery : BaseQuery
{
    public bool? IsReconciled { get; set; }
    public string? CustomerId { get; set; }
    public CustomerPaymentStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}


