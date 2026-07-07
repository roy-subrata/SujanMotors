using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IHolidayRepository
{
    Task<IEnumerable<Holiday>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task<Holiday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsOnDateAsync(DateTime date, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Holiday entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Holiday entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
