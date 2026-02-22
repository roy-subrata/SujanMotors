using AutoPartShop.Application.Suppliers.Dtos;

namespace AutoPartShop.Application.Suppliers
{
    public interface ISupplierReadRepository
    {
        Task<(IEnumerable<SupplierResponse> Suppliers, int TotalCount)> FindAllAsynce(SupplierQuery query, CancellationToken cancellationToken = default);
    }
}
