using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IWarrantyClaimEventRepository
{
    Task AddAsync(WarrantyClaimEvent entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarrantyClaimEvent>> GetByClaimIdAsync(Guid warrantyClaimId, CancellationToken cancellationToken = default);
}
