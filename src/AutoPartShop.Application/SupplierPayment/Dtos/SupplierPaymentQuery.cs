using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.SupplierPayment.Dtos
{
    public class SupplierPaymentQuery : BaseQuery
    {
        public bool? IsReconciled { get; set; }
        public string? SupplierId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
    }

}
