using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Brands;
using AutoPartShop.Application.Brands.Dtos;
using AutoPartShop.Application.DTOs.BrandDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/v1/brands")]
[HasPermission(Permissions.InventoryView)]
[Produces("application/json")]
public class BrandsController : ControllerBase
{
    private readonly IBrandRepository _brandRepository;
    private readonly IBrandReadRepository _brandReadRepository;
    private readonly ILogger<BrandsController> _logger;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly AutoPartDbContext _dbContext;

    public BrandsController(
        IBrandRepository brandRepository,
        IBrandReadRepository brandReadRepository,
        ILogger<BrandsController> logger,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        AutoPartDbContext dbContext)
    {
        _brandRepository = brandRepository;
        _brandReadRepository = brandReadRepository;
        _logger = logger;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    // â”€â”€ List â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
        if (pageSize < 1) pageSize = 10;
        else if (pageSize > 100) pageSize = 100;

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

    // â”€â”€ Single by ID â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€ Single by code â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    // â”€â”€ Create â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiError.Validation("Name is required", instance: Request.Path));

        var existingBrand = await _dbContext.Brands
            .FirstOrDefaultAsync(b => b.Name.ToLower() == request.Name.Trim().ToLower() && !b.Isdeleted, cancellationToken);
        if (existingBrand is not null)
            return Conflict(ApiError.Conflict($"A brand named '{request.Name.Trim()}' already exists", instance: Request.Path));

        var brand = Brand.Create(
            request.Name.Trim(),
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

    // â”€â”€ Update â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiError.Validation("Name is required", instance: Request.Path));

        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
        if (brand is null)
            return NotFound(ApiError.NotFound($"Brand '{id}' not found", Request.Path));

        var duplicateBrand = await _dbContext.Brands
            .FirstOrDefaultAsync(b => b.Id != id && b.Name.ToLower() == request.Name.Trim().ToLower() && !b.Isdeleted, cancellationToken);
        if (duplicateBrand is not null)
            return Conflict(ApiError.Conflict($"A brand named '{request.Name.Trim()}' already exists", instance: Request.Path));

        brand.Update(
            request.Name.Trim(),
            request.Description ?? string.Empty,
            request.LogoUrl ?? string.Empty,
            request.Website ?? string.Empty,
            request.Country ?? string.Empty,
            request.ContactEmail ?? string.Empty,
            request.ContactPhone ?? string.Empty,
            request.DisplayOrder, request.IsActive);
        brand.ModifiedBy = _currentUserService.GetCurrentUsername();

        await _brandRepository.UpdateAsync(brand, cancellationToken);

        return Ok(ApiResponse<BrandResponse>.Ok(MapToResponse(brand)));
    }

    // â”€â”€ Delete â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.InventoryDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
        if (brand is null)
            return NotFound(ApiError.NotFound($"Brand '{id}' not found", Request.Path));

        try
        {
            await _brandRepository.DeleteAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiError.BusinessRuleConflict(ex.Message, Request.Path));
        }

        return NoContent();
    }

    // â”€â”€ Mapping â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static BrandResponse MapToResponse(Brand brand) => new()
    {
        Id = brand.Id,
        Name = brand.Name,
        // Normalise empty strings stored by the domain to null so the response
        // matches the declared nullable types and frontend null-checks work correctly
        Description = NullIfEmpty(brand.Description),
        LogoUrl = NullIfEmpty(brand.LogoUrl),
        Website = NullIfEmpty(brand.Website),
        Country = NullIfEmpty(brand.Country),
        ContactEmail = NullIfEmpty(brand.ContactEmail),
        ContactPhone = NullIfEmpty(brand.ContactPhone),
        DisplayOrder = brand.DisplayOrder,
        IsActive = brand.IsActive,
        CreatedAt = brand.CreatedDate,
        ModifiedAt = brand.ModifiedDate == default ? null : brand.ModifiedDate
    };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
