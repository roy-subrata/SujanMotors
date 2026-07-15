using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ProductMediaRepository : IProductMediaRepository
{
    private readonly AutoPartDbContext _dbContext;

    public ProductMediaRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ProductMedia>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductMedias
            .Where(x => x.PartId == partId && !x.Isdeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductMedia?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductMedias
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(ProductMedia entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.ProductMedias.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductMedia entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.ProductMedias.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ProductMedias
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

        if (entity != null)
        {
            entity.Isdeleted = true;
            _dbContext.ProductMedias.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ClearPrimaryAsync(Guid partId, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        var others = await _dbContext.ProductMedias
            .Where(x => x.PartId == partId && x.IsPrimary && !x.Isdeleted && (exceptId == null || x.Id != exceptId))
            .ToListAsync(cancellationToken);

        foreach (var media in others)
        {
            media.Update(media.Url, media.MediaType, media.SortOrder, isPrimary: false, media.VariantId, media.AltText, media.FileName);
        }

        if (others.Count > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
