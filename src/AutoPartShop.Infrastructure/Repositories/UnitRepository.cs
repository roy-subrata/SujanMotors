using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of Unit repository
/// TODO: Replace with Entity Framework Core implementation when database is configured
/// </summary>
public class UnitRepository(AutoPartDbContext dbContext) : IUnitRepository
{
    public async Task<IEnumerable<Unit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Units.Where(u => !u.Isdeleted).OrderBy(u => u.DisplayOrder).ToListAsync(cancellationToken);
    }

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Units.FirstOrDefaultAsync(u => u.Id == id && !u.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Unit entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Units.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Unit entity, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Units.FirstOrDefaultAsync(u => u.Id == entity.Id, cancellationToken);
        if (existing != null)
        {
            dbContext.Units.Remove(existing);
            dbContext.Units.Add(entity);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.Units.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (unit != null)
        {
            dbContext.Units.Remove(unit);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Units.AnyAsync(u => u.Id == id && !u.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Unit>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Units.Where(u => u.IsActive && !u.Isdeleted).OrderBy(u => u.DisplayOrder).ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Unit> Units, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Units.Where(u => !u.Isdeleted).OrderBy(u => u.DisplayOrder);
        var totalCount = await query.CountAsync();
        var units = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (units, totalCount);
    }

    public async Task<(IEnumerable<Unit> Units, int TotalCount)> SearchPagedAsync(
 string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Units.Where(u => !u.Isdeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = $"%{searchTerm}%"; // LIKE pattern
            query = query.Where(u =>
                EF.Functions.Like(u.Name, term) ||
                EF.Functions.Like(u.Code, term)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var units = await query
            .OrderBy(u => u.DisplayOrder)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (units, totalCount);
    }


    public async Task<bool> CodeExistsAsync(string code, Guid? excludeUnitId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpper();
        return await dbContext.Units.AnyAsync(u => u.Code == normalizedCode && !u.Isdeleted && (excludeUnitId == null || u.Id != excludeUnitId), cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeUnitId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToLower();
        return await dbContext.Units.AnyAsync(u => u.Name.ToLower() == normalizedName && !u.Isdeleted && (excludeUnitId == null || u.Id != excludeUnitId), cancellationToken);
    }

    public async Task<Unit?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpper();
        return await dbContext.Units.FirstOrDefaultAsync(u => u.Code == normalizedCode && !u.Isdeleted, cancellationToken);
    }

    public async Task<Unit?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToLower();
        return await dbContext.Units.FirstOrDefaultAsync(u => u.Name.ToLower() == normalizedName && !u.Isdeleted, cancellationToken);
    }
}

/// <summary>
/// In-memory implementation of UnitConversion repository
/// TODO: Replace with Entity Framework Core implementation when database is configured
/// </summary>
public class UnitConversionRepository : IUnitConversionRepository
{
    private static readonly List<UnitConversion> _conversions = new();

    static UnitConversionRepository()
    {
        InitializeDemoData();
    }

    private static void InitializeDemoData()
    {
        // We'll add conversions after units are created in the controller
        // For now, leave empty
    }

    public async Task<IEnumerable<UnitConversion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions.Where(c => !c.Isdeleted).ToList());
    }

    public async Task<UnitConversion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions.FirstOrDefault(c => c.Id == id && !c.Isdeleted));
    }

    public async Task AddAsync(UnitConversion entity, CancellationToken cancellationToken = default)
    {
        _conversions.Add(entity);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(UnitConversion entity, CancellationToken cancellationToken = default)
    {
        var existing = _conversions.FirstOrDefault(c => c.Id == entity.Id);
        if (existing != null)
        {
            _conversions.Remove(existing);
            _conversions.Add(entity);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var conversion = _conversions.FirstOrDefault(c => c.Id == id);
        if (conversion != null)
        {
            _conversions.Remove(conversion);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions.Any(c => c.Id == id && !c.Isdeleted));
    }

    public async Task<IEnumerable<UnitConversion>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions.Where(c => c.IsActive && !c.Isdeleted).ToList());
    }

    public async Task<(IEnumerable<UnitConversion> Conversions, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _conversions.Where(c => !c.Isdeleted).AsEnumerable();
        var totalCount = query.Count();
        var conversions = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return await Task.FromResult((conversions.AsEnumerable(), totalCount));
    }

    public async Task<IEnumerable<UnitConversion>> GetConversionsFromUnitAsync(Guid fromUnitId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions.Where(c => c.FromUnitId == fromUnitId && !c.Isdeleted).ToList());
    }

    public async Task<IEnumerable<UnitConversion>> GetConversionsToUnitAsync(Guid toUnitId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions.Where(c => c.ToUnitId == toUnitId && !c.Isdeleted).ToList());
    }

    public async Task<UnitConversion?> GetConversionAsync(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions.FirstOrDefault(c => c.FromUnitId == fromUnitId && c.ToUnitId == toUnitId && !c.Isdeleted));
    }

    public async Task<bool> ConversionExistsAsync(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions.Any(c => c.FromUnitId == fromUnitId && c.ToUnitId == toUnitId && !c.Isdeleted));
    }

    public async Task<IEnumerable<UnitConversion>> GetAllConversionsForUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_conversions
            .Where(c => (c.FromUnitId == unitId || c.ToUnitId == unitId) && !c.Isdeleted)
            .ToList());
    }

    public async Task<(IEnumerable<UnitConversion> Conversions, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _conversions.Where(c => !c.Isdeleted).AsEnumerable();
        var totalCount = query.Count();
        var conversions = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return await Task.FromResult((conversions.AsEnumerable(), totalCount));
    }
}
