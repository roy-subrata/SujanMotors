using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Brands;
using AutoPartShop.Application.Brands.Dtos;
using AutoPartShop.Application.DTOs.BrandDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/v1/brands")]
[Authorize]
[Produces("application/json")]
public class BrandsController : ControllerBase
{
    private readonly IBrandRepository _brandRepository;
    private readonly IBrandReadRepository _brandReadRepository;
    private readonly ILogger<BrandsController> _logger;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;

    public BrandsController(
        IBrandRepository brandRepository,
        IBrandReadRepository brandReadRepository,
        ILogger<BrandsController> logger,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService)
    {
        _brandRepository = brandRepository;
        _brandReadRepository = brandReadRepository;
        _logger = logger;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
    }

    // ── List ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// List brands with optional filtering and pagination.
    /// Results are ordered by displayOrder ASC, name ASC.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? country,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 10;

        var query = new BrandQuery
        {
            Search = search ?? string.Empty,
            PageNumber = page,
            PageSize = pageSize,
            IsActive = isActive,
            Country = country
        };

        var (items, total) = await _brandReadRepository.FindAllyAsync(query, cancellationToken);
        return Ok(PagedApiResponse<BrandResponse>.Create(items, total, page, pageSize));
    }

    // ── Single by ID ──────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
        if (brand is null)
            return NotFound(ApiError.NotFound($"Brand '{id}' not found", Request.Path));

        return Ok(ApiResponse<BrandResponse>.Ok(MapToResponse(brand)));
    }

    // ── Single by code ────────────────────────────────────────────────────────

    /// <summary>Look up a brand by its unique code (e.g. "NGK", "BOSCH"). Case-insensitive.</summary>
    [HttpGet("by-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode([FromQuery] string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(ApiError.Validation("'code' query parameter is required", instance: Request.Path));

        var brand = await _brandRepository.GetByCodeAsync(code.Trim().ToUpperInvariant(), cancellationToken);
        if (brand is null)
            return NotFound(ApiError.NotFound($"Brand with code '{code}' not found", Request.Path));

        return Ok(ApiResponse<BrandResponse>.Ok(MapToResponse(brand)));
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiError.Validation("Name is required", instance: Request.Path));

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiError.Validation("Code is required", instance: Request.Path));

        // Normalise code — always stored uppercase, no leading/trailing whitespace
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        if (await _brandRepository.ExistsByCodeAsync(normalizedCode, cancellationToken))
            return Conflict(ApiError.Conflict($"Brand code '{normalizedCode}' is already in use", Request.Path));

        var brand = Brand.Create(
            request.Name.Trim(), normalizedCode,
            request.Description ?? string.Empty,
            request.Country ?? string.Empty,
            request.LogoUrl ?? string.Empty,
            request.Website ?? string.Empty,
            request.ContactEmail ?? string.Empty,
            request.ContactPhone ?? string.Empty,
            request.DisplayOrder, request.IsActive);

        var user = _currentUserService.GetCurrentUsername();
        brand.CreatedBy = user;
        brand.ModifiedBy = user;

        await _brandRepository.AddAsync(brand, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = brand.Id },
            ApiResponse<BrandResponse>.Ok(MapToResponse(brand)));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiError.Validation("Name is required", instance: Request.Path));

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiError.Validation("Code is required", instance: Request.Path));

        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
        if (brand is null)
            return NotFound(ApiError.NotFound($"Brand '{id}' not found", Request.Path));

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        // Only check for conflict if the code is actually changing
        if (!brand.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase) &&
            await _brandRepository.ExistsByCodeAsync(normalizedCode, cancellationToken))
            return Conflict(ApiError.Conflict($"Brand code '{normalizedCode}' is already in use", Request.Path));

        brand.Update(
            request.Name.Trim(), normalizedCode,
            request.Description ?? string.Empty,
            request.LogoUrl ?? string.Empty,
            request.Website ?? string.Empty,
            request.Country ?? string.Empty,
            request.ContactEmail ?? string.Empty,
            request.ContactPhone ?? string.Empty,
            request.DisplayOrder, request.IsActive);
        brand.ModifiedBy = _currentUserService.GetCurrentUsername();

        try
        {
            await _brandRepository.UpdateAsync(brand, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Concurrent request created the same code between our check and update
            return Conflict(ApiError.Conflict($"Brand code '{normalizedCode}' is already in use", Request.Path));
        }

        return Ok(ApiResponse<BrandResponse>.Ok(MapToResponse(brand)));
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
        if (brand is null)
            return NotFound(ApiError.NotFound($"Brand '{id}' not found", Request.Path));

        await _brandRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static BrandResponse MapToResponse(Brand brand) => new()
    {
        Id = brand.Id,
        Name = brand.Name,
        Code = brand.Code,
        // Normalise empty strings stored by the domain to null so the response
        // matches the declared nullable types and frontend null-checks work correctly
        Description  = NullIfEmpty(brand.Description),
        LogoUrl      = NullIfEmpty(brand.LogoUrl),
        Website      = NullIfEmpty(brand.Website),
        Country      = NullIfEmpty(brand.Country),
        ContactEmail = NullIfEmpty(brand.ContactEmail),
        ContactPhone = NullIfEmpty(brand.ContactPhone),
        DisplayOrder = brand.DisplayOrder,
        IsActive     = brand.IsActive,
        CreatedAt    = brand.CreatedDate,
        ModifiedAt   = brand.ModifiedDate == default ? null : brand.ModifiedDate
    };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
