using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class HolidayRepository : IHolidayRepository
{
    private readonly AutoPartDbContext _dbContext;

    public HolidayRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Holiday>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        return await _dbContext.Holidays
            .Where(x => x.Date >= start && x.Date < end && !x.Isdeleted)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<Holiday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Holidays
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<bool> ExistsOnDateAsync(DateTime date, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var day = date.Date;
        return await _dbContext.Holidays
            .AnyAsync(x => x.Date == day && !x.Isdeleted && x.Id != excludeId, cancellationToken);
    }

    public async Task AddAsync(Holiday entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.Holidays.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Holiday entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Holidays.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Holidays
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

        if (entity != null)
        {
            entity.Isdeleted = true;
            _dbContext.Holidays.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
