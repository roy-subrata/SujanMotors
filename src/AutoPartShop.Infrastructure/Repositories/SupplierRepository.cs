using AutoPartShop.Domain.Entities;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class SupplierRepository(AutoPartDbContext _db) : ISupplierRepository
{
    public async Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Suppliers.Where(s => !s.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && !s.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Supplier entity, CancellationToken cancellationToken = default)
    {
        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Supplier entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == entity.Id, cancellationToken);
        if (existing != null)
        {
            existing.Update(entity.Name,
                entity.ContactPerson,
                entity.Email,
                entity.Phone,
                entity.Address,
                entity.City,
                entity.State,
                entity.Country,
                entity.PostalCode,
                entity.IsActive
                );
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier != null)
        {
            _db.Suppliers.Remove(supplier);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Suppliers.AnyAsync(s => s.Id == id && !s.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Supplier>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Suppliers.Where(s => s.IsActive && !s.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Supplier> Suppliers, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Suppliers.Where(s => !s.Isdeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Code.ToLower().Contains(term) || s.ContactPerson.ToLower().Contains(term));
        }


        var totalCount = await query.CountAsync(cancellationToken);
        var suppliers = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (suppliers, totalCount);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeSupplierId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpper();
        return await _db.Suppliers.AnyAsync(s => s.Code == normalizedCode && !s.Isdeleted && (excludeSupplierId == null || s.Id != excludeSupplierId), cancellationToken);
    }

    public async Task<Supplier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpper();
        return await _db.Suppliers.FirstOrDefaultAsync(s => s.Code == normalizedCode && !s.Isdeleted, cancellationToken);
    }

}
