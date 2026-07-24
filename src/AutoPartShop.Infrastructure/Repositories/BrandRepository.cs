using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly AutoPartDbContext _dbContext;

    public BrandRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Brand>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Brand>()
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Brand>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Brand>()
            .Where(x => x.IsActive && !x.Isdeleted)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Brand entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.Set<Brand>().AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Brand entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Set<Brand>().Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<Brand>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            // In-use guard — a brand assigned to products would otherwise be silently unlinked
            // (Product.BrandId FK is SetNull) or leave products pointing at a soft-deleted brand.
            if (await _dbContext.Parts.AnyAsync(p => p.BrandId == id && !p.Isdeleted, cancellationToken))
                throw new InvalidOperationException("Cannot delete a brand that is assigned to products. Reassign or remove those products first.");

            entity.Isdeleted = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Brand>()
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
}
