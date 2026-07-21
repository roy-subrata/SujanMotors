using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class WarehouseLocationRepository : IWarehouseLocationRepository
{
    private readonly AutoPartDbContext _db;

    public WarehouseLocationRepository(AutoPartDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<WarehouseLocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.WarehouseLocations
            .Include(x => x.Warehouse)
            .Include(x => x.Category)
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.Warehouse.Name)
            .ThenBy(x => x.Zone)
            .ThenBy(x => x.Aisle)
            .ThenBy(x => x.Rack)
            .ThenBy(x => x.Bin)
            .ToListAsync(cancellationToken);
    }

    public async Task<WarehouseLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.WarehouseLocations
            .Include(x => x.Warehouse)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<WarehouseLocation>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _db.WarehouseLocations
            .Include(x => x.Category)
            .Where(x => x.WarehouseId == warehouseId && !x.Isdeleted)
            .OrderBy(x => x.Zone)
            .ThenBy(x => x.Aisle)
            .ThenBy(x => x.Rack)
            .ThenBy(x => x.Bin)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<WarehouseLocation> Locations, int TotalCount)> SearchPagedAsync(
        WarehouseLocationQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var locationsQuery = _db.WarehouseLocations
            .Include(x => x.Warehouse)
            .Include(x => x.Category)
            .Where(x => !x.Isdeleted);

        if (query.WarehouseId.HasValue)
            locationsQuery = locationsQuery.Where(x => x.WarehouseId == query.WarehouseId.Value);

        if (query.CategoryId.HasValue)
            locationsQuery = locationsQuery.Where(x => x.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            locationsQuery = locationsQuery.Where(x =>
                x.Zone.Contains(search) ||
                x.Aisle.Contains(search) ||
                x.Rack.Contains(search) ||
                x.Bin.Contains(search) ||
                (x.Zone + "-" + x.Aisle + "-" + x.Rack + "-" + x.Bin).Contains(search));
        }

        var totalCount = await locationsQuery.CountAsync(cancellationToken);

        var locations = await locationsQuery
            .OrderBy(x => x.Warehouse.Name)
            .ThenBy(x => x.Zone)
            .ThenBy(x => x.Aisle)
            .ThenBy(x => x.Rack)
            .ThenBy(x => x.Bin)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (locations, totalCount);
    }

    public async Task<bool> LocationExistsAsync(
        Guid warehouseId,
        string zone,
        string aisle,
        string rack,
        string bin,
        Guid? excludeLocationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WarehouseLocations
            .Where(x => x.WarehouseId == warehouseId
                && x.Zone == zone
                && x.Aisle == aisle
                && x.Rack == rack
                && x.Bin == bin
                && !x.Isdeleted);

        if (excludeLocationId.HasValue)
            query = query.Where(x => x.Id != excludeLocationId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(WarehouseLocation entity, CancellationToken cancellationToken = default)
    {
        await _db.WarehouseLocations.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WarehouseLocation entity, CancellationToken cancellationToken = default)
    {
        _db.WarehouseLocations.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await _db.WarehouseLocations.FindAsync(new object[] { id }, cancellationToken);
        if (location != null)
        {
            location.Isdeleted = true;
            location.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.WarehouseLocations.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
}
