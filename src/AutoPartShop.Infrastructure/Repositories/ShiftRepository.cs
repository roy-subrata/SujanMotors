using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly AutoPartDbContext _dbContext;

    public ShiftRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Shift>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Shifts
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Shifts
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Shift entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var exists = await _dbContext.Shifts
            .AnyAsync(x => x.Name == entity.Name && !x.Isdeleted, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Shift '{entity.Name}' already exists");

        await _dbContext.Shifts.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Shift entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Shifts.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Shifts
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

        if (entity != null)
        {
            entity.Isdeleted = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .AnyAsync(e => e.ShiftId == id && !e.Isdeleted, cancellationToken);
    }
}
