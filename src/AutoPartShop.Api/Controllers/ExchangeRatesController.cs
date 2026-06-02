using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CurrencyDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[Authorize]
public class ExchangeRatesController : ControllerBase
{
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ICurrencyConversionService _conversionService;
    private readonly ILogger<ExchangeRatesController> _logger;

    public ExchangeRatesController(
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository,
        ICurrencyConversionService conversionService,
        ILogger<ExchangeRatesController> logger)
    {
        _exchangeRateRepository = exchangeRateRepository;
        _currencyRepository = currencyRepository;
        _conversionService = conversionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all exchange rates
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExchangeRateResponse>>> GetAll()
    {
        var rates = await _exchangeRateRepository.GetAllAsync();
        return Ok(rates.Select(MapToResponse));
    }

    /// <summary>
    /// Get current active exchange rates
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<IEnumerable<ExchangeRateResponse>>> GetCurrent()
    {
        var rates = await _exchangeRateRepository.GetCurrentRatesAsync();
        return Ok(rates.Select(MapToResponse));
    }

    /// <summary>
    /// Get exchange rates for a specific date
    /// </summary>
    [HttpGet("date/{date}")]
    public async Task<ActionResult<IEnumerable<ExchangeRateResponse>>> GetForDate(DateTime date)
    {
        var rates = await _exchangeRateRepository.GetRatesForDateAsync(date);
        return Ok(rates.Select(MapToResponse));
    }

    /// <summary>
    /// Get exchange rate by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExchangeRateResponse>> GetById(Guid id)
    {
        var rate = await _exchangeRateRepository.GetByIdAsync(id);
        if (rate == null)
            return NotFound($"Exchange rate with ID {id} not found");

        return Ok(MapToResponse(rate));
    }

    /// <summary>
    /// Get rate history between two currencies
    /// </summary>
    [HttpGet("history/{fromCurrencyId}/{toCurrencyId}")]
    public async Task<ActionResult<IEnumerable<ExchangeRateResponse>>> GetHistory(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var rates = await _exchangeRateRepository.GetRateHistoryAsync(
            fromCurrencyId,
            toCurrencyId,
            startDate,
            endDate);

        return Ok(rates.Select(MapToResponse));
    }

    /// <summary>
    /// Convert amount between currencies
    /// </summary>
    [HttpPost("convert")]
    public async Task<ActionResult<ConversionResponse>> Convert([FromBody] ConversionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var convertedAmount = await _conversionService.ConvertAsync(
                request.Amount,
                request.FromCurrency,
                request.ToCurrency,
                request.EffectiveDate);

            var rate = await _conversionService.GetExchangeRateAsync(
                request.FromCurrency,
                request.ToCurrency,
                request.EffectiveDate);

            return Ok(new ConversionResponse
            {
                OriginalAmount = request.Amount,
                OriginalCurrency = request.FromCurrency,
                ConvertedAmount = convertedAmount,
                ConvertedCurrency = request.ToCurrency,
                ExchangeRate = rate ?? 1.0m,
                EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow.Date,
                ConversionTimestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new exchange rate (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExchangeRateResponse>> Create([FromBody] CreateExchangeRateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate currencies exist
        var fromCurrency = await _currencyRepository.GetByIdAsync(request.FromCurrencyId);
        if (fromCurrency == null)
            return BadRequest($"From currency with ID {request.FromCurrencyId} not found");

        var toCurrency = await _currencyRepository.GetByIdAsync(request.ToCurrencyId);
        if (toCurrency == null)
            return BadRequest($"To currency with ID {request.ToCurrencyId} not found");

        var exchangeRate = ExchangeRate.Create(
            request.FromCurrencyId,
            request.ToCurrencyId,
            request.Rate,
            request.EffectiveDate,
            request.ExpiryDate,
            request.Source,
            request.Notes);

        await _exchangeRateRepository.AddAsync(exchangeRate);

        return CreatedAtAction(nameof(GetById), new { id = exchangeRate.Id }, MapToResponse(exchangeRate));
    }

    /// <summary>
    /// Bulk create exchange rates (Admin only)
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ExchangeRateResponse>>> CreateBulk([FromBody] List<CreateExchangeRateRequest> requests)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var createdRates = new List<ExchangeRate>();

        foreach (var request in requests)
        {
            var exchangeRate = ExchangeRate.Create(
                request.FromCurrencyId,
                request.ToCurrencyId,
                request.Rate,
                request.EffectiveDate,
                request.ExpiryDate,
                request.Source,
                request.Notes);

            await _exchangeRateRepository.AddAsync(exchangeRate);
            createdRates.Add(exchangeRate);
        }

        return Ok(createdRates.Select(MapToResponse));
    }

    /// <summary>
    /// Update exchange rate (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExchangeRateResponse>> Update(Guid id, [FromBody] UpdateExchangeRateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var exchangeRate = await _exchangeRateRepository.GetByIdAsync(id);
        if (exchangeRate == null)
            return NotFound($"Exchange rate with ID {id} not found");

        exchangeRate.Update(
            request.Rate,
            request.EffectiveDate,
            request.ExpiryDate,
            request.Source,
            request.Notes);

        await _exchangeRateRepository.UpdateAsync(exchangeRate);

        return Ok(MapToResponse(exchangeRate));
    }

    /// <summary>
    /// Delete exchange rate (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var exchangeRate = await _exchangeRateRepository.GetByIdAsync(id);
        if (exchangeRate == null)
            return NotFound($"Exchange rate with ID {id} not found");

        exchangeRate.Delete();
        await _exchangeRateRepository.UpdateAsync(exchangeRate);

        return NoContent();
    }

    private static ExchangeRateResponse MapToResponse(ExchangeRate rate)
    {
        return new ExchangeRateResponse
        {
            Id = rate.Id,
            FromCurrencyId = rate.FromCurrencyId,
            FromCurrencyCode = rate.FromCurrency?.Code ?? string.Empty,
            FromCurrencyName = rate.FromCurrency?.Name ?? string.Empty,
            ToCurrencyId = rate.ToCurrencyId,
            ToCurrencyCode = rate.ToCurrency?.Code ?? string.Empty,
            ToCurrencyName = rate.ToCurrency?.Name ?? string.Empty,
            Rate = rate.Rate,
            EffectiveDate = rate.EffectiveDate,
            ExpiryDate = rate.ExpiryDate,
            Source = rate.Source,
            IsActive = rate.IsActive,
            Notes = rate.Notes
        };
    }
}
