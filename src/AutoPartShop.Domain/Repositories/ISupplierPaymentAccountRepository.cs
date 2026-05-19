using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface ISupplierPaymentAccountRepository
{
    Task<IEnumerable<SupplierPaymentAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SupplierPaymentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPaymentAccount>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<SupplierPaymentAccount?> GetDefaultBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPaymentAccount>> GetActiveBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task AddAsync(SupplierPaymentAccount entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupplierPaymentAccount entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
