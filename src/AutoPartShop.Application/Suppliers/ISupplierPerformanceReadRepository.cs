using AutoPartShop.Application.Suppliers.Dtos;

namespace AutoPartShop.Application.Suppliers
{
    public interface ISupplierPerformanceReadRepository
    {
        Task<IEnumerable<SupplierPerformanceResponse>> GetPerformanceAsync(string? search = null, CancellationToken cancellationToken = default);
    }
}
