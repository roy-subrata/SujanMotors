using AutoPartShop.Domain.Entities.HR;

namespace AutoPartShop.Domain.Repositories.HR;

public interface IShiftRepository
{
    Task<IEnumerable<Shift>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Shift entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Shift entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken = default);
}
