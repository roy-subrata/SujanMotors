namespace AutoPartShop.Infrastructure.Repositories;

using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for ExchangeRate entity
/// </summary>
public class ExchangeRateRepository(AutoPartDbContext context) : IExchangeRateRepository
{
    private readonly AutoPartDbContext _context = context;

    /// <inheritdoc/>
    public async Task<IEnumerable<ExchangeRate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExchangeRates
            .Include(er => er.FromCurrency)
            .Include(er => er.ToCurrency)
            .Where(er => !er.Isdeleted)
            .OrderByDescending(er => er.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExchangeRate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExchangeRates
            .Include(er => er.FromCurrency)
            .Include(er => er.ToCurrency)
            .FirstOrDefaultAsync(er => er.Id == id && !er.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(ExchangeRate entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _context.ExchangeRates.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ExchangeRate entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _context.ExchangeRates.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exchangeRate = await GetByIdAsync(id, cancellationToken);
        if (exchangeRate != null)
        {
            exchangeRate.Delete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExchangeRates
            .AnyAsync(er => er.Id == id && !er.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExchangeRate?> GetRateAsync(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExchangeRates
            .Include(er => er.FromCurrency)
            .Include(er => er.ToCurrency)
            .Where(er => er.FromCurrencyId == fromCurrencyId
                      && er.ToCurrencyId == toCurrencyId
                      && er.IsActive
                      && !er.Isdeleted
                      && er.EffectiveDate <= effectiveDate
                      && (er.ExpiryDate == null || er.ExpiryDate >= effectiveDate))
            .OrderByDescending(er => er.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExchangeRate?> GetRateByCurrencyCodesAsync(
        string fromCurrencyCode,
        string toCurrencyCode,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExchangeRates
            .Include(er => er.FromCurrency)
            .Include(er => er.ToCurrency)
            .Where(er => er.FromCurrency!.Code == fromCurrencyCode.ToUpper()
                      && er.ToCurrency!.Code == toCurrencyCode.ToUpper()
                      && er.IsActive
                      && !er.Isdeleted
                      && er.EffectiveDate <= effectiveDate
                      && (er.ExpiryDate == null || er.ExpiryDate >= effectiveDate))
            .OrderByDescending(er => er.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExchangeRate?> GetLatestRateAsync(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExchangeRates
            .Include(er => er.FromCurrency)
            .Include(er => er.ToCurrency)
            .Where(er => er.FromCurrencyId == fromCurrencyId
                      && er.ToCurrencyId == toCurrencyId
                      && er.IsActive
                      && !er.Isdeleted)
            .OrderByDescending(er => er.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ExchangeRate>> GetRatesForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExchangeRates
            .Include(er => er.FromCurrency)
            .Include(er => er.ToCurrency)
            .Where(er => er.IsActive
                      && !er.Isdeleted
                      && er.EffectiveDate <= date
                      && (er.ExpiryDate == null || er.ExpiryDate >= date))
            .OrderBy(er => er.FromCurrency!.Code)
            .ThenBy(er => er.ToCurrency!.Code)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ExchangeRate>> GetCurrentRatesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await GetRatesForDateAsync(today, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ExchangeRate>> GetRateHistoryAsync(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ExchangeRates
            .Include(er => er.FromCurrency)
            .Include(er => er.ToCurrency)
            .Where(er => er.FromCurrencyId == fromCurrencyId
                      && er.ToCurrencyId == toCurrencyId
                      && !er.Isdeleted);

        if (startDate.HasValue)
            query = query.Where(er => er.EffectiveDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(er => er.EffectiveDate <= endDate.Value);

        return await query
            .OrderByDescending(er => er.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsForDateAsync(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExchangeRates
            .AnyAsync(er => er.FromCurrencyId == fromCurrencyId
                         && er.ToCurrencyId == toCurrencyId
                         && er.IsActive
                         && !er.Isdeleted
                         && er.EffectiveDate <= effectiveDate
                         && (er.ExpiryDate == null || er.ExpiryDate >= effectiveDate),
                      cancellationToken);
    }
}
