using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ProductLocationRepository : IProductLocationRepository
{
    private readonly AutoPartDbContext _db;

    public ProductLocationRepository(AutoPartDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ProductLocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ProductLocations
            .Include(x => x.Part)
            .Include(x => x.Warehouse)
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.IsPrimary ? 0 : 1)
            .ThenBy(x => x.Section)
            .ThenBy(x => x.Shelf)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.ProductLocations
            .Include(x => x.Part)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<ProductLocation>> GetLocationsByPartAsync(Guid partId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.ProductLocations
            .Include(x => x.Warehouse)
            .Where(x => x.PartId == partId && !x.Isdeleted);

        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        return await query
            .OrderBy(x => x.IsPrimary ? 0 : 1)
            .ThenBy(x => x.Warehouse.Name)
            .ThenBy(x => x.Section)
            .ThenBy(x => x.Shelf)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductLocation>> GetLocationsByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _db.ProductLocations
            .Include(x => x.Part)
            .Where(x => x.WarehouseId == warehouseId && !x.Isdeleted)
            .OrderBy(x => x.Section)
            .ThenBy(x => x.Shelf)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductLocation?> GetLocationByPartAndWarehouseAsync(Guid partId, Guid warehouseId, string section, string shelf, CancellationToken cancellationToken = default)
    {
        return await _db.ProductLocations
            .Include(x => x.Part)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.PartId == partId
                && x.WarehouseId == warehouseId
                && x.Section == section
                && x.Shelf == shelf
                && !x.Isdeleted, cancellationToken);
    }

    public async Task<ProductLocation?> GetPrimaryLocationByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _db.ProductLocations
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.PartId == partId && x.IsPrimary && !x.Isdeleted, cancellationToken);
    }

    public async Task SetPrimaryLocationAsync(Guid partId, Guid locationId, CancellationToken cancellationToken = default)
    {
        // First, unset all primary flags for this part
        var allLocations = await _db.ProductLocations
            .Where(x => x.PartId == partId && !x.Isdeleted)
            .ToListAsync(cancellationToken);

        foreach (var loc in allLocations)
        {
            loc.UnsetPrimary();
        }

        // Set the specified location as primary
        var primaryLocation = allLocations.FirstOrDefault(x => x.Id == locationId);
        if (primaryLocation != null)
        {
            primaryLocation.SetAsPrimary();
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> LocationExistsAsync(Guid partId, Guid warehouseId, string section, string shelf, Guid? excludeLocationId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.ProductLocations
            .Where(x => x.PartId == partId
                && x.WarehouseId == warehouseId
                && x.Section == section
                && x.Shelf == shelf
                && !x.Isdeleted);

        if (excludeLocationId.HasValue)
        {
            query = query.Where(x => x.Id != excludeLocationId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(ProductLocation entity, CancellationToken cancellationToken = default)
    {
        await _db.ProductLocations.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductLocation entity, CancellationToken cancellationToken = default)
    {
        _db.ProductLocations.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await _db.ProductLocations.FindAsync(new object[] { id }, cancellationToken);
        if (location != null)
        {
            location.Isdeleted = true;
            location.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.ProductLocations.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
}
