namespace AutoPartShop.Infrastructure.Repositories;

using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for Currency entity
/// </summary>
public class CurrencyRepository(AutoPartDbContext context) : ICurrencyRepository
{
    private readonly AutoPartDbContext _context = context;

    /// <inheritdoc/>
    public async Task<IEnumerable<Currency>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .Where(c => !c.Isdeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .FirstOrDefaultAsync(c => c.Id == id && !c.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Currency entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _context.Currencies.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Currency entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _context.Currencies.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currency = await GetByIdAsync(id, cancellationToken);
        if (currency != null)
        {
            currency.Delete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .AnyAsync(c => c.Id == id && !c.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper() && !c.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .FirstOrDefaultAsync(c => c.IsBaseCurrency && !c.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Currency>> GetActiveCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .Where(c => c.IsActive && !c.Isdeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return await _context.Currencies
            .AnyAsync(c => c.Code == code.ToUpper() && !c.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Currency>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .Where(c => !c.Isdeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }
}
