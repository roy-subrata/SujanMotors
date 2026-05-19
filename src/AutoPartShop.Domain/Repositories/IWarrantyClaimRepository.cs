using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IWarrantyClaimRepository : IBaseRepository<WarrantyClaim>
{
    Task<WarrantyClaim?> GetByClaimNumberAsync(string claimNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyClaim>> GetByWarrantyRegistrationIdAsync(Guid warrantyRegistrationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyClaim>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyClaim>> GetByTechnicianIdAsync(Guid technicianId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyClaim>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyClaim>> GetPendingClaimsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyClaim>> GetInProgressClaimsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyClaim>> GetOpenClaimsAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<WarrantyClaim> Claims, int TotalCount)> SearchPagedAsync(
        string? searchTerm,
        string? status,
        string? serviceType,
        Guid? customerId,
        Guid? technicianId,
        Guid? warrantyRegistrationId,
        DateTime? claimDateFrom,
        DateTime? claimDateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> ClaimNumberExistsAsync(string claimNumber, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalServiceCostByWarrantyAsync(Guid warrantyRegistrationId, CancellationToken cancellationToken = default);
}
