using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class WarehouseRepository(AutoPartDbContext dbContext) : IWarehouseRepository
{
    public async Task<IEnumerable<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Warehouses
            .Where(w => !w.Isdeleted)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Warehouse entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        await dbContext.Warehouses.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Warehouse entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        dbContext.Warehouses.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warehouse is null) return;

        dbContext.Warehouses.Remove(warehouse);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id && !w.Isdeleted, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Warehouses
            .AnyAsync(w => w.Id == id && !w.Isdeleted, cancellationToken);
    }

    public async Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var normalized = code.Trim().ToUpperInvariant();

        return await dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Code == normalized && !w.Isdeleted, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeWarehouseId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var normalized = code.Trim().ToUpperInvariant();

        var reusult = await dbContext.Warehouses.AnyAsync(
            w => w.Code == normalized
                 && !w.Isdeleted
                 && (excludeWarehouseId == null || w.Id != excludeWarehouseId),
            cancellationToken);
        return reusult;
    }
}
