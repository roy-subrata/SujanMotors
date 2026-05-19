using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class SupplierPaymentAccountRepository(AutoPartDbContext _db) : ISupplierPaymentAccountRepository
{
    public async Task<IEnumerable<SupplierPaymentAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPaymentAccounts
            .Include(x => x.Supplier)
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.Supplier.Name)
            .ThenByDescending(x => x.IsDefault)
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierPaymentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPaymentAccounts
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<SupplierPaymentAccount>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPaymentAccounts
            .Include(x => x.Supplier)
            .Where(x => x.SupplierId == supplierId && !x.Isdeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.AccountName)
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierPaymentAccount?> GetDefaultBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPaymentAccounts
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.SupplierId == supplierId && x.IsDefault && x.IsActive && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<SupplierPaymentAccount>> GetActiveBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPaymentAccounts
            .Include(x => x.Supplier)
            .Where(x => x.SupplierId == supplierId && x.IsActive && !x.Isdeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.AccountName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SupplierPaymentAccount entity, CancellationToken cancellationToken = default)
    {
        _db.SupplierPaymentAccounts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SupplierPaymentAccount entity, CancellationToken cancellationToken = default)
    {
        _db.SupplierPaymentAccounts.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SupplierPaymentAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity != null)
        {
            entity.Isdeleted = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPaymentAccounts.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
}
