using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IWarrantyRegistrationRepository : IBaseRepository<WarrantyRegistration>
{
    Task<WarrantyRegistration?> GetByWarrantyNumberAsync(string warrantyNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyRegistration>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyRegistration>> GetBySalesOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyRegistration>> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyRegistration>> GetActiveWarrantiesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyRegistration>> GetExpiredWarrantiesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyRegistration>> GetExpiringWarrantiesAsync(int daysFromNow, CancellationToken cancellationToken = default);
    Task<(IEnumerable<WarrantyRegistration> Warranties, int TotalCount)> SearchPagedAsync(
        string? searchTerm,
        string? status,
        Guid? customerId,
        Guid? partId,
        DateTime? expiryDateFrom,
        DateTime? expiryDateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> WarrantyNumberExistsAsync(string warrantyNumber, CancellationToken cancellationToken = default);
}
