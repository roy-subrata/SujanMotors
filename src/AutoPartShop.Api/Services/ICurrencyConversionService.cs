namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for currency conversion operations
/// </summary>
public interface ICurrencyConversionService
{
    /// <summary>
    /// Convert amount from one currency to another
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency code (e.g., "USD")</param>
    /// <param name="toCurrency">Target currency code (e.g., "BDT")</param>
    /// <param name="effectiveDate">Date for which to use exchange rate (defaults to today)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Converted amount</returns>
    Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert amount to the base currency
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="effectiveDate">Date for which to use exchange rate (defaults to today)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Amount in base currency</returns>
    Task<decimal> ConvertToBaseAsync(
        decimal amount,
        string fromCurrency,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the base currency code
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base currency code (e.g., "BDT")</returns>
    Task<string> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get exchange rate between two currencies
    /// </summary>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrency">Target currency code</param>
    /// <param name="effectiveDate">Date for which to get the rate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate (1 FROM = rate × TO)</returns>
    Task<decimal?> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);
}
