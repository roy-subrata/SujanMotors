using AutoPartShop.Application.Warehouse;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
namespace AutoPartShop.Infrastructure.Repositories;


public class WarehouseReadRepository(AutoPartDbContext _db) : IWarehouseReadRepository
{
    public async Task<(IEnumerable<WarehouseResponse> Warehouses, int TotalCount)> FindAllAsync(WarehouseQueryDto query, CancellationToken cancellationToken = default)
    {
        var warehousesQuery = _db.Warehouses
            .AsNoTracking()
            .Where(w => !w.Isdeleted);

        var term = (query.Search ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(term))
        {
            warehousesQuery = warehousesQuery.Where(w =>
                EF.Functions.Like(w.Name.ToLower(), $"%{term}%") ||
                EF.Functions.Like(w.Code.ToLower(), $"%{term}%") ||
                EF.Functions.Like(w.Location.ToLower(), $"%{term}%") ||
                EF.Functions.Like(w.City.ToLower(), $"%{term}%") ||
                EF.Functions.Like(w.State.ToLower(), $"%{term}%") ||
                EF.Functions.Like(w.Country.ToLower(), $"%{term}%") ||
                EF.Functions.Like(w.PostalCode.ToLower(), $"%{term}%") ||
                EF.Functions.Like(w.Manager.ToLower(), $"%{term}%"));
        }

        if (query.Sorts != null && query.Sorts.Any())
        {
            var sorts =
                query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
            warehousesQuery = warehousesQuery.OrderByMultiple(sorts);
        }
        else
        {
            warehousesQuery = warehousesQuery.OrderByDescending(w => w.CreatedDate);
        }

        var totalCount = await warehousesQuery.CountAsync(cancellationToken);
        var warehouses = await warehousesQuery
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(w => new WarehouseResponse
            {
                Id = w.Id,
                Name = w.Name,
                Code = w.Code,
                Location = w.Location,
                City = w.City,
                State = w.State,
                Country = w.Country,
                PostalCode = w.PostalCode,
                Manager = w.Manager,
                ManagerEmail = w.ManagerEmail,
                ManagerPhone = w.ManagerPhone,
                StorageCapacity = w.StorageCapacity,
                CapacityUnit = w.CapacityUnit,
                Description = w.Description,
                IsActive = w.IsActive,
                CreatedBy = w.CreatedBy ?? "",
                ModifiedBy = w.ModifiedBy ?? ""
            })
            .ToListAsync(cancellationToken);

        return (warehouses, totalCount);
    }

    public async Task<WarehouseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id && !w.Isdeleted, cancellationToken);
        return warehouse is null ? null : new WarehouseResponse
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            Code = warehouse.Code,
            Location = warehouse.Location,
            City = warehouse.City,
            State = warehouse.State,
            PostalCode = warehouse.PostalCode,
            Country = warehouse.Country,
            Manager = warehouse.Manager,
            ManagerEmail = warehouse.ManagerEmail,
            ManagerPhone = warehouse.ManagerPhone,
            StorageCapacity = warehouse.StorageCapacity,
            CapacityUnit = warehouse.CapacityUnit,
            Description = warehouse.Description,
            IsActive = warehouse.IsActive,
            CreatedBy = warehouse.CreatedBy ?? "",
            ModifiedBy = warehouse.ModifiedBy ?? ""
        };
    }

    public async Task<WarehouseResponse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Code == normalized && !w.Isdeleted, cancellationToken);
        return warehouse is null ? null : new WarehouseResponse
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            Code = warehouse.Code,
            Location = warehouse.Location,
            City = warehouse.City,
            State = warehouse.State,
            PostalCode = warehouse.PostalCode,
            Country = warehouse.Country,
            Manager = warehouse.Manager,
            ManagerEmail = warehouse.ManagerEmail,
            ManagerPhone = warehouse.ManagerPhone,
            StorageCapacity = warehouse.StorageCapacity,
            CapacityUnit = warehouse.CapacityUnit,
            Description = warehouse.Description,
            IsActive = warehouse.IsActive,
            CreatedBy = warehouse.CreatedBy ?? "",
            ModifiedBy = warehouse.ModifiedBy ?? ""
        };
    }
}
