namespace AutoPartShop.Api.Services;

using AutoPartShop.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Service implementation for currency conversion
/// </summary>
public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly ICurrencyRepository _currencyRepository;
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private readonly IApplicationSettingsRepository _settingsRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrencyConversionService> _logger;

    private const string BaseCurrencyCacheKey = "BaseCurrency";
    private const int CacheExpirationMinutes = 60;

    public CurrencyConversionService(
        ICurrencyRepository currencyRepository,
        IExchangeRateRepository exchangeRateRepository,
        IApplicationSettingsRepository settingsRepository,
        IMemoryCache cache,
        ILogger<CurrencyConversionService> logger)
    {
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _settingsRepository = settingsRepository;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        // Normalize currency codes
        fromCurrency = fromCurrency?.Trim().ToUpper() ?? throw new ArgumentNullException(nameof(fromCurrency));
        toCurrency = toCurrency?.Trim().ToUpper() ?? throw new ArgumentNullException(nameof(toCurrency));

        // If same currency, no conversion needed
        if (fromCurrency == toCurrency)
            return amount;

        // Use today if no date specified
        var date = effectiveDate ?? DateTime.UtcNow.Date;

        // Get base currency
        var baseCurrency = await GetBaseCurrencyAsync(cancellationToken);

        decimal convertedAmount;

        // Case 1: Converting to base currency (direct conversion)
        if (toCurrency == baseCurrency)
        {
            var rate = await GetExchangeRateInternalAsync(fromCurrency, toCurrency, date, cancellationToken);
            if (rate == null)
                throw new InvalidOperationException($"No exchange rate found for {fromCurrency} to {toCurrency} on {date:yyyy-MM-dd}");

            convertedAmount = amount * rate.Value;
        }
        // Case 2: Converting from base currency (inverse conversion)
        else if (fromCurrency == baseCurrency)
        {
            var rate = await GetExchangeRateInternalAsync(baseCurrency, toCurrency, date, cancellationToken);
            if (rate == null)
                throw new InvalidOperationException($"No exchange rate found for {baseCurrency} to {toCurrency} on {date:yyyy-MM-dd}");

            convertedAmount = amount * rate.Value;
        }
        // Case 3: Converting between non-base currencies (through base)
        else
        {
            // Convert FROM → BASE
            var rateToBase = await GetExchangeRateInternalAsync(fromCurrency, baseCurrency, date, cancellationToken);
            if (rateToBase == null)
                throw new InvalidOperationException($"No exchange rate found for {fromCurrency} to {baseCurrency} on {date:yyyy-MM-dd}");

            var amountInBase = amount * rateToBase.Value;

            // Convert BASE → TO
            var rateFromBase = await GetExchangeRateInternalAsync(baseCurrency, toCurrency, date, cancellationToken);
            if (rateFromBase == null)
                throw new InvalidOperationException($"No exchange rate found for {baseCurrency} to {toCurrency} on {date:yyyy-MM-dd}");

            convertedAmount = amountInBase * rateFromBase.Value;
        }

        // Round to 2 decimal places using banker's rounding
        return Math.Round(convertedAmount, 2, MidpointRounding.ToEven);
    }

    /// <inheritdoc/>
    public async Task<decimal> ConvertToBaseAsync(
        decimal amount,
        string fromCurrency,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        var baseCurrency = await GetBaseCurrencyAsync(cancellationToken);
        return await ConvertAsync(amount, fromCurrency, baseCurrency, effectiveDate, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue<string>(BaseCurrencyCacheKey, out var cachedCurrency) && !string.IsNullOrEmpty(cachedCurrency))
        {
            return cachedCurrency;
        }

        // Get from database - try settings first
        var baseCurrencySetting = await _settingsRepository.GetValueAsync("BASE_CURRENCY", cancellationToken);

        if (!string.IsNullOrWhiteSpace(baseCurrencySetting))
        {
            // Cache for future use
            _cache.Set(BaseCurrencyCacheKey, baseCurrencySetting, TimeSpan.FromMinutes(CacheExpirationMinutes));
            return baseCurrencySetting;
        }

        // Fallback: Get from Currency table (IsBaseCurrency flag)
        var baseCurrencyEntity = await _currencyRepository.GetBaseCurrencyAsync(cancellationToken);
        if (baseCurrencyEntity != null)
        {
            _cache.Set(BaseCurrencyCacheKey, baseCurrencyEntity.Code, TimeSpan.FromMinutes(CacheExpirationMinutes));
            return baseCurrencyEntity.Code;
        }

        // Ultimate fallback
        _logger.LogWarning("No base currency configured, defaulting to BDT");
        return "BDT";
    }

    /// <inheritdoc/>
    public async Task<decimal?> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        fromCurrency = fromCurrency?.Trim().ToUpper() ?? throw new ArgumentNullException(nameof(fromCurrency));
        toCurrency = toCurrency?.Trim().ToUpper() ?? throw new ArgumentNullException(nameof(toCurrency));

        if (fromCurrency == toCurrency)
            return 1.0m;

        var date = effectiveDate ?? DateTime.UtcNow.Date;
        return await GetExchangeRateInternalAsync(fromCurrency, toCurrency, date, cancellationToken);
    }

    /// <summary>
    /// Internal method to get exchange rate with caching
    /// </summary>
    private async Task<decimal?> GetExchangeRateInternalAsync(
        string fromCurrency,
        string toCurrency,
        DateTime effectiveDate,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"ExchangeRate_{fromCurrency}_{toCurrency}_{effectiveDate:yyyyMMdd}";

        // Try cache first
        if (_cache.TryGetValue<decimal?>(cacheKey, out var cachedRate))
        {
            return cachedRate;
        }

        // Get from database
        var exchangeRate = await _exchangeRateRepository.GetRateByCurrencyCodesAsync(
            fromCurrency,
            toCurrency,
            effectiveDate,
            cancellationToken);

        decimal? rate = exchangeRate?.Rate;

        // Cache the result (even if null, to avoid repeated DB queries)
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
        };
        _cache.Set(cacheKey, rate, cacheOptions);

        return rate;
    }
}
