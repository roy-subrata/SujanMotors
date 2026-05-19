using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CurrencyDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ICurrencyConversionService _conversionService;
    private readonly ILogger<CurrenciesController> _logger;

    public CurrenciesController(
        ICurrencyRepository currencyRepository,
        ICurrencyConversionService conversionService,
        ILogger<CurrenciesController> logger)
    {
        _currencyRepository = currencyRepository;
        _conversionService = conversionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all currencies
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CurrencyResponse>>> GetAll()
    {
        var currencies = await _currencyRepository.GetAllOrderedAsync();
        return Ok(currencies.Select(MapToResponse));
    }

    /// <summary>
    /// Get active currencies only
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<CurrencyResponse>>> GetActive()
    {
        var currencies = await _currencyRepository.GetActiveCurrenciesAsync();
        return Ok(currencies.Select(MapToResponse));
    }

    /// <summary>
    /// Get base currency
    /// </summary>
    [HttpGet("base")]
    public async Task<ActionResult<CurrencyResponse>> GetBase()
    {
        var currency = await _currencyRepository.GetBaseCurrencyAsync();
        if (currency == null)
            return NotFound("No base currency configured");

        return Ok(MapToResponse(currency));
    }

    /// <summary>
    /// Get currency by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CurrencyResponse>> GetById(Guid id)
    {
        var currency = await _currencyRepository.GetByIdAsync(id);
        if (currency == null)
            return NotFound($"Currency with ID {id} not found");

        return Ok(MapToResponse(currency));
    }

    /// <summary>
    /// Get currency by code
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<ActionResult<CurrencyResponse>> GetByCode(string code)
    {
        var currency = await _currencyRepository.GetByCodeAsync(code);
        if (currency == null)
            return NotFound($"Currency with code {code} not found");

        return Ok(MapToResponse(currency));
    }

    /// <summary>
    /// Create a new currency (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CurrencyResponse>> Create([FromBody] CreateCurrencyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if currency code already exists
        if (await _currencyRepository.ExistsByCodeAsync(request.Code))
            return Conflict($"Currency with code {request.Code} already exists");

        var currency = Currency.Create(
            request.Code,
            request.Name,
            request.Symbol,
            request.DecimalPlaces,
            request.IsActive,
            request.IsBaseCurrency,
            request.DisplayOrder);

        await _currencyRepository.AddAsync(currency);

        return CreatedAtAction(nameof(GetById), new { id = currency.Id }, MapToResponse(currency));
    }

    /// <summary>
    /// Update currency (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CurrencyResponse>> Update(Guid id, [FromBody] UpdateCurrencyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currency = await _currencyRepository.GetByIdAsync(id);
        if (currency == null)
            return NotFound($"Currency with ID {id} not found");

        currency.Update(
            request.Name,
            request.Symbol,
            request.DecimalPlaces,
            request.IsActive,
            request.DisplayOrder);

        await _currencyRepository.UpdateAsync(currency);

        return Ok(MapToResponse(currency));
    }

    /// <summary>
    /// Set currency as base currency (Admin only)
    /// </summary>
    [HttpPut("{id}/set-base")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SetAsBase(Guid id)
    {
        var currency = await _currencyRepository.GetByIdAsync(id);
        if (currency == null)
            return NotFound($"Currency with ID {id} not found");

        // Remove base currency status from current base
        var currentBase = await _currencyRepository.GetBaseCurrencyAsync();
        if (currentBase != null && currentBase.Id != id)
        {
            currentBase.RemoveBaseCurrencyStatus();
            await _currencyRepository.UpdateAsync(currentBase);
        }

        // Set new base currency
        currency.SetAsBaseCurrency();
        await _currencyRepository.UpdateAsync(currency);

        return Ok(new { message = $"Currency {currency.Code} set as base currency" });
    }

    /// <summary>
    /// Delete currency (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var currency = await _currencyRepository.GetByIdAsync(id);
        if (currency == null)
            return NotFound($"Currency with ID {id} not found");

        try
        {
            currency.Delete();
            await _currencyRepository.UpdateAsync(currency);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static CurrencyResponse MapToResponse(Currency currency)
    {
        return new CurrencyResponse
        {
            Id = currency.Id,
            Code = currency.Code,
            Name = currency.Name,
            Symbol = currency.Symbol,
            DecimalPlaces = currency.DecimalPlaces,
            IsActive = currency.IsActive,
            IsBaseCurrency = currency.IsBaseCurrency,
            DisplayOrder = currency.DisplayOrder
        };
    }
}
