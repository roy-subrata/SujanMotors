using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.SaleOrders.Dtos
{
    public class SaleOrderQuery : BaseQuery
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        public string? Channel { get; set; }
        public Guid? CustomerId { get; set; }
    }
}
