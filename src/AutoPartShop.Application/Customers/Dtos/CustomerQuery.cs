using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Customers.Dtos
{
    public class CustomerQuery : BaseQuery
    {
        public string? Status { get; set; }
        public string? CustomerType { get; set; }
    }
}


