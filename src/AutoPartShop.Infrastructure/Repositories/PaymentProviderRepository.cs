using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PaymentProviderRepository(AutoPartDbContext _db) : IPaymentProviderRepository
{
    public async Task<IEnumerable<PaymentProvider>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.PaymentProviders.Where(x => !x.Isdeleted).OrderBy(x => x.ProviderName).ToListAsync(cancellationToken);

    public async Task<PaymentProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.PaymentProviders.FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

    public async Task AddAsync(PaymentProvider entity, CancellationToken cancellationToken = default)
    {
        _db.PaymentProviders.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PaymentProvider entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.PaymentProviders.FirstOrDefaultAsync(x => x.Id == entity.Id);
        if (existing != null)
        {
            existing.UpdateNotes(entity.Notes);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentProviders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity != null)
        {
            _db.PaymentProviders.Remove(entity);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.PaymentProviders.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

    public async Task<PaymentProvider?> GetByNameAsync(string providerName, CancellationToken cancellationToken = default)
        => await _db.PaymentProviders.FirstOrDefaultAsync(x => x.ProviderName == providerName && !x.Isdeleted, cancellationToken);

    public async Task<IEnumerable<PaymentProvider>> GetByTypeAsync(string providerType, CancellationToken cancellationToken = default)
        => await _db.PaymentProviders.Where(x => x.ProviderType == providerType.ToUpper() && !x.Isdeleted).OrderBy(x => x.ProviderName).ToListAsync(cancellationToken);

    public async Task<IEnumerable<PaymentProvider>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        => await _db.PaymentProviders.Where(x => x.Status == status && !x.Isdeleted).OrderBy(x => x.ProviderName).ToListAsync(cancellationToken);

    public async Task<PaymentProvider?> GetDefaultAsync(CancellationToken cancellationToken = default)
        => await _db.PaymentProviders.FirstOrDefaultAsync(x => x.IsDefault && x.Status == "ACTIVE" && !x.Isdeleted, cancellationToken);

    public async Task<IEnumerable<PaymentProvider>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _db.PaymentProviders.Where(x => x.Status == "ACTIVE" && !x.Isdeleted).OrderBy(x => x.ProviderName).ToListAsync(cancellationToken);

    public async Task<(IEnumerable<PaymentProvider> providers, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        var paged = all.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        return (paged, all.Count());
    }
}
