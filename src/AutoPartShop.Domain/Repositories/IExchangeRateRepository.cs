namespace AutoPartShop.Domain.Repositories;

using AutoPartShop.Domain.Entities;

/// <summary>
/// Repository interface for ExchangeRate entity
/// </summary>
public interface IExchangeRateRepository : IBaseRepository<ExchangeRate>
{
    /// <summary>
    /// Get the exchange rate valid for a specific date
    /// Returns the most recent rate that is effective on or before the given date
    /// </summary>
    Task<ExchangeRate?> GetRateAsync(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the exchange rate by currency codes for a specific date
    /// </summary>
    Task<ExchangeRate?> GetRateByCurrencyCodesAsync(
        string fromCurrencyCode,
        string toCurrencyCode,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest/current exchange rate between two currencies
    /// </summary>
    Task<ExchangeRate?> GetLatestRateAsync(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all exchange rates valid for a specific date
    /// </summary>
    Task<List<ExchangeRate>> GetRatesForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all current/active exchange rates
    /// </summary>
    Task<List<ExchangeRate>> GetCurrentRatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get exchange rate history for a currency pair
    /// </summary>
    Task<List<ExchangeRate>> GetRateHistoryAsync(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an exchange rate exists for a specific date
    /// </summary>
    Task<bool> ExistsForDateAsync(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default);
}
