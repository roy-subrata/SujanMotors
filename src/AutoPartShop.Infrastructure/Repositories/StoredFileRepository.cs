using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class StoredFileRepository : IStoredFileRepository
{
    private readonly AutoPartDbContext _dbContext;

    public StoredFileRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoredFiles
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<StoredFile>> GetByOwnerAsync(string ownerType, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var normalizedType = ownerType.Trim().ToUpperInvariant();
        return await _dbContext.StoredFiles
            .Where(x => x.OwnerType == normalizedType && x.OwnerId == ownerId && !x.Isdeleted)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StoredFile entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.StoredFiles.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StoredFile entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.StoredFiles.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.StoredFiles
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

        if (entity != null)
        {
            entity.Isdeleted = true;
            _dbContext.StoredFiles.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
