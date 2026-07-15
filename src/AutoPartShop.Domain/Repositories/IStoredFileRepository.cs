using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IStoredFileRepository
{
    Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoredFile>> GetByOwnerAsync(string ownerType, Guid ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(StoredFile entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(StoredFile entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
