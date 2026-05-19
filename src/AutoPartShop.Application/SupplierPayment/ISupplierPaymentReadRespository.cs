using AutoPartShop.Application.Common;
using AutoPartShop.Application.SupplierPayment.Dtos;

namespace AutoPartShop.Application.Supplier
{
    public interface ISupplierPaymentReadRespository
    {
        Task<(IEnumerable<SupplierPaymentResponse> paymentResponse, int total)> FindAllAsynce(SupplierPaymentQuery query, CancellationToken cancellationToken = default);
    }
}

