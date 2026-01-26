using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.BrandDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private readonly IBrandRepository _brandRepository;
    private readonly ILogger<BrandsController> _logger;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;

    public BrandsController(IBrandRepository brandRepository, ILogger<BrandsController> logger, ICodeGenerateService codeGenerateService, ICurrentUserService currentUserService)
    {
        _brandRepository = brandRepository;
        _logger = logger;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all brands
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BrandResponse>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _brandRepository.GetAllAsync(cancellationToken);
            return Ok(brands.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all brands");
            return StatusCode(500, "An error occurred while retrieving brands");
        }
    }

    /// <summary>
    /// Get active brands only
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<BrandResponse>>> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _brandRepository.GetActiveBrandsAsync(cancellationToken);
            return Ok(brands.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active brands");
            return StatusCode(500, "An error occurred while retrieving active brands");
        }
    }

    /// <summary>
    /// Get brand by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BrandResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
            if (brand is null)
                return NotFound(new { message = "Brand not found" });

            return Ok(MapToResponse(brand));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand by ID: {BrandId}", id);
            return StatusCode(500, "An error occurred while retrieving the brand");
        }
    }

    /// <summary>
    /// Get brand by code
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<ActionResult<BrandResponse>> GetByCode(string code, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandRepository.GetByCodeAsync(code, cancellationToken);
            if (brand is null)
                return NotFound(new { message = "Brand not found" });

            return Ok(MapToResponse(brand));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand by code: {BrandCode}", code);
            return StatusCode(500, "An error occurred while retrieving the brand");
        }
    }

    /// <summary>
    /// Create a new brand
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BrandResponse>> Create(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if code already exists
            if (await _brandRepository.ExistsByCodeAsync(request.Code, cancellationToken))
                return BadRequest(new { message = $"Brand with code '{request.Code}' already exists" });

            var brand = Brand.Create(
                request.Name,
                request.Code,
                request.Description,
                request.Country
            );
            var currentUser = _currentUserService.GetCurrentUsername();
            brand.CreatedBy = currentUser;
            brand.ModifiedBy = currentUser;

            await _codeGenerateService.SaveGenerateCodeAsync("BRD", cancellationToken);
            await _brandRepository.AddAsync(brand, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = brand.Id }, MapToResponse(brand));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating brand");
            return StatusCode(500, "An error occurred while creating the brand");
        }
    }

    /// <summary>
    /// Update an existing brand
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BrandResponse>> Update(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (id != request.Id)
                return BadRequest(new { message = "ID mismatch" });

            var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
            if (brand is null)
                return NotFound(new { message = "Brand not found" });

            // Check if code is being changed and new code already exists
            if (!brand.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase))
            {
                if (await _brandRepository.ExistsByCodeAsync(request.Code, cancellationToken))
                    return BadRequest(new { message = $"Brand with code '{request.Code}' already exists" });
            }

            brand.Update(
                request.Name,
                request.Code,
                request.Description,
                request.LogoUrl,
                request.Website,
                request.Country,
                request.ContactEmail,
                request.ContactPhone,
                request.DisplayOrder,
                request.IsActive
            );
            brand.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _brandRepository.UpdateAsync(brand, cancellationToken);

            return Ok(MapToResponse(brand));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand: {BrandId}", id);
            return StatusCode(500, "An error occurred while updating the brand");
        }
    }

    /// <summary>
    /// Delete a brand (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
            if (brand is null)
                return NotFound(new { message = "Brand not found" });

            await _brandRepository.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand: {BrandId}", id);
            return StatusCode(500, "An error occurred while deleting the brand");
        }
    }

    private static BrandResponse MapToResponse(Brand brand)
    {
        return new BrandResponse
        {
            Id = brand.Id,
            Name = brand.Name,
            Code = brand.Code,
            Description = brand.Description,
            LogoUrl = brand.LogoUrl,
            Website = brand.Website,
            Country = brand.Country,
            ContactEmail = brand.ContactEmail,
            ContactPhone = brand.ContactPhone,
            DisplayOrder = brand.DisplayOrder,
            IsActive = brand.IsActive,
            CreatedAt = brand.CreatedDate,
            ModifiedAt = brand.ModifiedDate == default ? null : brand.ModifiedDate
        };
    }
}
