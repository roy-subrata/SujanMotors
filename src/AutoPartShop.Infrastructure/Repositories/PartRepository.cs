using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;
public class PartRepository(AutoPartDbContext _db) : IPartRepository
{
    public async Task<IEnumerable<Part>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Parts.Where(p => !p.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .FirstOrDefaultAsync(p => p.Id == id && !p.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Part entity, CancellationToken cancellationToken = default)
    {
        _db.Parts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Part entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Parts.FirstOrDefaultAsync(p => p.Id == entity.Id, cancellationToken);
        if (existing != null)
        {
            existing.Update(
                entity.Name,
                entity.Description,
                entity.SKU,
                entity.CategoryId,
                entity.BrandId,
                entity.BaseUnitId,
                entity.UnitId,
                entity.CostPrice,
                entity.SellingPrice,
                entity.MinimumStock,
                entity.IsActive,
                entity.HasWarranty,
                entity.WarrantyPeriodMonths,
                entity.WarrantyType,
                entity.WarrantyTerms,
                entity.WarrantyCertificateTemplate,
                // Universal product fields — must be passed or they get reset to defaults
                entity.Barcode,
                entity.Tags,
                entity.ProductType,
                entity.IsPerishable,
                entity.WeightKg,
                entity.WidthCm,
                entity.HeightCm,
                entity.DepthCm,
                entity.TaxCode,
                entity.RichDescription);

            existing.ModifiedBy = entity.ModifiedBy;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == id);
        if (part != null)
        {
            _db.Parts.Remove(part);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Parts.AnyAsync(p => p.Id == id && !p.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Part>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Parts.Where(p => p.IsActive && !p.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Part> Parts, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Parts.Where(c => !c.Isdeleted);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.SKU.ToLower().Contains(term));
        }
        var totalCount = await query.CountAsync(cancellationToken);

        var parts = await query
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (parts, totalCount);
    }

    public async Task<bool> SKUExistsAsync(string sku, Guid? excludePartId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.ToUpper();
        return await _db.Parts.AnyAsync(p => p.SKU == normalizedSku && !p.Isdeleted && (excludePartId == null || p.Id != excludePartId), cancellationToken);
    }

    public async Task<Part?> GetBySKUAsync(string sku, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.ToUpper();
        return await _db.Parts.FirstOrDefaultAsync(p => p.SKU == normalizedSku && !p.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Part>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _db.Parts.Where(p => p.CategoryId == categoryId && !p.Isdeleted).ToListAsync(cancellationToken);
    }


}

public class VehicleRepository(AutoPartDbContext _db) : IVehicleRepository
{

    public async Task<IEnumerable<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Vehicles.Where(v => !v.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && !v.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Vehicle entity, CancellationToken cancellationToken = default)
    {
        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Vehicle entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == entity.Id, cancellationToken);
        if (existing != null)
        {
            existing.Update(entity.Make, entity.Model, entity.Year, entity.EngineType, entity.Description, entity.IsActive);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vehicle != null)
        {
            _db.Vehicles.Remove(vehicle);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Vehicles.AnyAsync(v => v.Id == id && !v.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Vehicles.Where(v => v.IsActive && !v.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Vehicle> Vehicles, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {

        var query = _db.Vehicles.Where(v => !v.Isdeleted);
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var lowerTerm = searchTerm.ToLower();
            query = query.Where(x =>
             EF.Functions.Like(x.Make, $"%{searchTerm}%") || EF.Functions.Like(x.Model, $"%{searchTerm}%") || EF.Functions.Like(x.Description, $"%{searchTerm}%")
          );
        }
        var totalCount = await query.CountAsync(cancellationToken);

        var vehicles = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (vehicles, totalCount);
    }

    public async Task<IEnumerable<Vehicle>> GetByMakeAsync(string make, CancellationToken cancellationToken = default)
    {
        var normalizedMake = make.ToLower();
        return await _db.Vehicles.Where(v => v.Make.ToLower() == normalizedMake && !v.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _db.Vehicles.Where(v => v.Year == year && !v.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> GetByEngineTypeAsync(string engineType, CancellationToken cancellationToken = default)
    {
        var normalizedType = engineType.ToLower();
        return await _db.Vehicles.Where(v => v.EngineType.ToLower() == normalizedType && !v.Isdeleted).ToListAsync(cancellationToken);
    }
}

public class PartVehicleCompatibilityRepository(AutoPartDbContext _db) : IPartVehicleCompatibilityRepository
{

    public async Task<IEnumerable<PartVehicleCompatibility>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PartVehicleCompatibilities.Where(c => !c.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<PartVehicleCompatibility?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.PartVehicleCompatibilities.FirstOrDefaultAsync(c => c.Id == id && !c.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(PartVehicleCompatibility entity, CancellationToken cancellationToken = default)
    {
        _db.PartVehicleCompatibilities.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PartVehicleCompatibility entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.PartVehicleCompatibilities.FirstOrDefaultAsync(c => c.Id == entity.Id);
        if (existing != null)
        {
            existing.Update(entity.IsCompatible, entity.Notes);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var compatibility = await _db.PartVehicleCompatibilities.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (compatibility != null)
        {
            _db.PartVehicleCompatibilities.Remove(compatibility);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.PartVehicleCompatibilities.AnyAsync(c => c.Id == id && !c.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<PartVehicleCompatibility>> GetCompatibilitiesByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _db.PartVehicleCompatibilities
            .Include(x => x.Vehicle)
            .Where(c => c.PartId == partId && !c.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PartVehicleCompatibility>> GetCompatibilitiesByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _db.PartVehicleCompatibilities
            .Include(x => x.Part)
            .Include(x => x.Vehicle)
            .Where(c => c.VehicleId == vehicleId && !c.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<PartVehicleCompatibility?> GetCompatibilityAsync(Guid partId, Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _db.PartVehicleCompatibilities.FirstOrDefaultAsync(c => c.PartId == partId && c.VehicleId == vehicleId && !c.Isdeleted, cancellationToken);
    }

    public async Task<bool> IsCompatibleAsync(Guid partId, Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _db.PartVehicleCompatibilities.AnyAsync(c => c.PartId == partId && c.VehicleId == vehicleId && c.IsCompatible && !c.Isdeleted, cancellationToken);
    }

}
