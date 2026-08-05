namespace AutoPartShop.Api.Services;

using AutoPartShop.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

/// <summary>
/// Result of a base-currency conversion, carrying both the converted amount and the rate applied.
/// </summary>
public readonly record struct FxConversionResult(decimal BaseAmount, decimal RateToBase);

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
    private const int DefaultDecimalPlaces = 2;

    // static: this service is registered Scoped (new instance per request), but IMemoryCache
    // is a singleton — an instance-level token source would only ever cancel itself, never
    // the token attached to cache entries written by other request scopes. A shared static
    // source (already thread-safe via Interlocked.Exchange below) keeps invalidation working
    // regardless of DI lifetime.
    private static CancellationTokenSource _cacheResetSource = new();

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
        // Case 2: Converting from base currency (direct lookup of the base→target rate row,
        // not a mathematical inversion of a target→base rate)
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

        // Round to the target currency's configured decimal places using banker's rounding
        var targetCurrency = await _currencyRepository.GetByCodeAsync(toCurrency, cancellationToken);
        var decimalPlaces = targetCurrency?.DecimalPlaces ?? DefaultDecimalPlaces;
        return Math.Round(convertedAmount, decimalPlaces, MidpointRounding.ToEven);
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
    public async Task<FxConversionResult> ConvertToBaseWithRateAsync(
        decimal amount,
        string fromCurrency,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        var baseCurrency = await GetBaseCurrencyAsync(cancellationToken);

        var normalizedFrom = fromCurrency?.Trim().ToUpper() ?? throw new ArgumentNullException(nameof(fromCurrency));

        // Same currency — no conversion needed.
        if (normalizedFrom == baseCurrency)
            return new FxConversionResult(amount, 1m);

        var date = effectiveDate ?? DateTime.UtcNow.Date;

        var rate = await GetExchangeRateInternalAsync(normalizedFrom, baseCurrency, date, cancellationToken);
        if (rate == null)
            throw new InvalidOperationException($"No exchange rate found for {normalizedFrom} to {baseCurrency} on {date:yyyy-MM-dd}");

        // Round to the target (base) currency's configured decimal places using banker's rounding,
        // mirroring ConvertAsync so stored base amounts match what the report used to compute on the fly.
        var targetCurrency = await _currencyRepository.GetByCodeAsync(baseCurrency, cancellationToken);
        var decimalPlaces = targetCurrency?.DecimalPlaces ?? DefaultDecimalPlaces;
        var baseAmount = Math.Round(amount * rate.Value, decimalPlaces, MidpointRounding.ToEven);

        return new FxConversionResult(baseAmount, rate.Value);
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
            _cache.Set(BaseCurrencyCacheKey, baseCurrencySetting, CreateCacheEntryOptions());
            return baseCurrencySetting;
        }

        // Fallback: Get from Currency table (IsBaseCurrency flag)
        var baseCurrencyEntity = await _currencyRepository.GetBaseCurrencyAsync(cancellationToken);
        if (baseCurrencyEntity != null)
        {
            _cache.Set(BaseCurrencyCacheKey, baseCurrencyEntity.Code, CreateCacheEntryOptions());
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

        // Only cache found rates. A missing rate isn't cached (negative caching) so it
        // doesn't poison lookups for the full expiration window once a rate is added.
        if (rate.HasValue)
        {
            _cache.Set(cacheKey, rate, CreateCacheEntryOptions());
        }

        return rate;
    }

    /// <inheritdoc/>
    public void InvalidateCache()
    {
        var oldSource = Interlocked.Exchange(ref _cacheResetSource, new CancellationTokenSource());
        oldSource.Cancel();
        oldSource.Dispose();
    }

    private MemoryCacheEntryOptions CreateCacheEntryOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
        }.AddExpirationToken(new CancellationChangeToken(_cacheResetSource.Token));
    }
}
