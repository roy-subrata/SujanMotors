using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class WarrantyClaimEventRepository(AutoPartDbContext _db) : IWarrantyClaimEventRepository
{
    public async Task AddAsync(WarrantyClaimEvent entity, CancellationToken cancellationToken = default)
    {
        _db.WarrantyClaimEvents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyClaimEvent>> GetByClaimIdAsync(Guid warrantyClaimId, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaimEvents
            .Where(e => e.WarrantyClaimId == warrantyClaimId && !e.Isdeleted)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync(cancellationToken);
    }
}
