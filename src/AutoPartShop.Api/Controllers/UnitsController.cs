using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.UnitDtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Controller for managing units (measurement units like kg, liters, pieces, etc.)
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class UnitsController : ControllerBase
{
    private readonly IUnitRepository _unitRepository;
    private readonly IUnitConversionRepository _conversionRepository;
    private readonly ILogger<UnitsController> _logger;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;

    public UnitsController(IUnitRepository unitRepository, IUnitConversionRepository conversionRepository, ICodeGenerateService codeGenerateService, ICurrentUserService currentUserService, ILogger<UnitsController> logger)
    {
        _unitRepository = unitRepository;
        _conversionRepository = conversionRepository;
        _logger = logger;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all units
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UnitResponse>))]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all units");
            var units = await _unitRepository.GetAllAsync(cancellationToken);
            var response = units.Select(u => MapToResponse(u));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all units");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving units");
        }
    }

    /// <summary>
    /// Get all active units
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UnitResponse>))]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all active units");
            var units = await _unitRepository.GetAllActiveAsync(cancellationToken);
            var response = units.Select(u => MapToResponse(u));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active units");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active units");
        }
    }

    /// <summary>
    /// Get units with pagination and optional search
    /// Unified endpoint for listing, searching, and pagination
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - GET /api/units/list - Get all units (page 1, 10 per page)
    /// - GET /api/units/list?pageNumber=1&amp;pageSize=20 - Custom page size
    /// - GET /api/units/list?searchTerm=kg - Search for "kg" (page 1, 10 per page)
    /// - GET /api/units/list?searchTerm=kg&amp;pageNumber=2&amp;pageSize=5 - Search with pagination
    /// </remarks>
    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
            _logger.LogInformation("Getting units - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}",
                pageNumber, pageSize, hasSearch ? searchTerm : "none");

            var (units, totalCount) = hasSearch
                ? await _unitRepository.SearchPagedAsync(searchTerm!, pageNumber, pageSize, cancellationToken)
                : await _unitRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);

            var response = units.Select(u => MapToResponse(u));
            return Ok(new
            {
                data = response,
                pagination = new
                {
                    pageNumber,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting units list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving units");
        }
    }

    /// <summary>
    /// Get unit by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnitResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting unit by ID: {UnitId}", id);
            var unit = await _unitRepository.GetByIdAsync(id, cancellationToken);

            if (unit is null)
            {
                _logger.LogWarning("Unit not found: {UnitId}", id);
                return NotFound(new { message = "Unit not found" });
            }

            var response = MapToResponse(unit);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unit by ID: {UnitId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the unit");
        }
    }

    /// <summary>
    /// Create a new unit
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UnitResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUnitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Symbol))
                return BadRequest(new { message = "Name, Code, and Symbol are required" });

            _logger.LogInformation("Creating new unit: {UnitName} ({UnitCode})", request.Name, request.Code);

            // Check for duplicate code
            if (await _unitRepository.CodeExistsAsync(request.Code, null, cancellationToken))
            {
                _logger.LogWarning("Unit code already exists: {UnitCode}", request.Code);
                return Conflict(new { message = $"Unit code '{request.Code}' already exists" });
            }

            // Check for duplicate name
            if (await _unitRepository.NameExistsAsync(request.Name, null, cancellationToken))
            {
                _logger.LogWarning("Unit name already exists: {UnitName}", request.Name);
                return Conflict(new { message = $"Unit name '{request.Name}' already exists" });
            }

            var unit = Unit.Create(request.Name, request.Code, request.Symbol, request.Description);
            var currentUser = _currentUserService.GetCurrentUsername();
            unit.CreatedBy = currentUser;
            unit.ModifiedBy = currentUser;

            await _unitRepository.AddAsync(unit, cancellationToken);

            var response = MapToResponse(unit);
            return CreatedAtAction(nameof(GetById), new { id = unit.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating unit");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating unit");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the unit");
        }
    }

    /// <summary>
    /// Update a unit
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnitResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Symbol))
                return BadRequest(new { message = "Name, Code, and Symbol are required" });

            _logger.LogInformation("Updating unit: {UnitId}", id);

            var unit = await _unitRepository.GetByIdAsync(id, cancellationToken);
            if (unit is null)
            {
                _logger.LogWarning("Unit not found for update: {UnitId}", id);
                return NotFound(new { message = "Unit not found" });
            }

            // Check for duplicate code (excluding current unit)
            if (await _unitRepository.CodeExistsAsync(request.Code, id, cancellationToken))
            {
                _logger.LogWarning("Unit code already exists: {UnitCode}", request.Code);
                return Conflict(new { message = $"Unit code '{request.Code}' already exists" });
            }

            // Check for duplicate name (excluding current unit)
            if (await _unitRepository.NameExistsAsync(request.Name, id, cancellationToken))
            {
                _logger.LogWarning("Unit name already exists: {UnitName}", request.Name);
                return Conflict(new { message = $"Unit name '{request.Name}' already exists" });
            }

            unit.Update(request.Name, request.Code, request.Symbol, request.Description, request.IsActive, request.DisplayOrder);
            unit.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _unitRepository.UpdateAsync(unit, cancellationToken);

            var response = MapToResponse(unit);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating unit");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unit: {UnitId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the unit");
        }
    }

    /// <summary>
    /// Activate a unit
    /// </summary>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnitResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Activating unit: {UnitId}", id);

            var unit = await _unitRepository.GetByIdAsync(id, cancellationToken);
            if (unit is null)
            {
                _logger.LogWarning("Unit not found for activation: {UnitId}", id);
                return NotFound(new { message = "Unit not found" });
            }

            unit.Activate();
            unit.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _unitRepository.UpdateAsync(unit, cancellationToken);

            var response = MapToResponse(unit);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating unit: {UnitId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while activating the unit");
        }
    }

    /// <summary>
    /// Deactivate a unit
    /// </summary>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnitResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deactivating unit: {UnitId}", id);

            var unit = await _unitRepository.GetByIdAsync(id, cancellationToken);
            if (unit is null)
            {
                _logger.LogWarning("Unit not found for deactivation: {UnitId}", id);
                return NotFound(new { message = "Unit not found" });
            }

            unit.Deactivate();
            unit.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _unitRepository.UpdateAsync(unit, cancellationToken);

            var response = MapToResponse(unit);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating unit: {UnitId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the unit");
        }
    }

    /// <summary>
    /// Delete a unit
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting unit: {UnitId}", id);

            var exists = await _unitRepository.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Unit not found for deletion: {UnitId}", id);
                return NotFound(new { message = "Unit not found" });
            }

            // Check if unit is used in conversions
            var conversions = await _conversionRepository.GetAllConversionsForUnitAsync(id, cancellationToken);
            if (conversions.Any())
            {
                _logger.LogWarning("Cannot delete unit that has conversions: {UnitId}", id);
                return BadRequest(new { message = "Cannot delete a unit that has active conversions" });
            }

            await _unitRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting unit: {UnitId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the unit");
        }
    }

    // ==================== UNIT CONVERSION ENDPOINTS ====================

    /// <summary>
    /// Get all unit conversions
    /// </summary>
    [HttpGet("conversions/all")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UnitConversionResponse>))]
    public async Task<IActionResult> GetAllConversions(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all unit conversions");
            var conversions = await _conversionRepository.GetAllAsync(cancellationToken);
            var response = conversions.Select(c => MapToConversionResponse(c, c.FromUnit, c.ToUnit));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all conversions");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving conversions");
        }
    }

    /// <summary>
    /// Get conversions with pagination and optional search
    /// Unified endpoint for listing, searching, and pagination
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - GET /api/units/conversions/list - Get all conversions (page 1, 10 per page)
    /// - GET /api/units/conversions/list?pageNumber=1&amp;pageSize=20 - Custom page size
    /// - GET /api/units/conversions/list?searchTerm=kg - Search for "kg" (page 1, 10 per page)
    /// - GET /api/units/conversions/list?searchTerm=kg&amp;pageNumber=2&amp;pageSize=5 - Search with pagination
    /// </remarks>
    [HttpGet("conversions/list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversionsList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
            _logger.LogInformation("Getting conversions - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}",
                pageNumber, pageSize, hasSearch ? searchTerm : "none");

            var (conversions, totalCount) = hasSearch
                ? await _conversionRepository.SearchPagedAsync(searchTerm!, pageNumber, pageSize, cancellationToken)
                : await _conversionRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);

            var response = conversions.Select(c => MapToConversionResponse(c, c.FromUnit, c.ToUnit));
            return Ok(new
            {
                data = response,
                pagination = new
                {
                    pageNumber,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversions list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving conversions");
        }
    }

    /// <summary>
    /// Get conversions for a specific unit (both from and to)
    /// </summary>
    [HttpGet("{id:guid}/conversions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UnitConversionResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnitConversions(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting conversions for unit: {UnitId}", id);

            var unitExists = await _unitRepository.ExistsAsync(id, cancellationToken);
            if (!unitExists)
            {
                _logger.LogWarning("Unit not found: {UnitId}", id);
                return NotFound(new { message = "Unit not found" });
            }

            var conversions = await _conversionRepository.GetAllConversionsForUnitAsync(id, cancellationToken);
            var response = conversions.Select(c => MapToConversionResponse(c, c.FromUnit, c.ToUnit));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversions for unit: {UnitId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving conversions");
        }
    }

    /// <summary>
    /// Get specific conversion between two units
    /// </summary>
    [HttpGet("conversions/{fromUnitId:guid}/to/{toUnitId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnitConversionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversion(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting conversion from {FromUnitId} to {ToUnitId}", fromUnitId, toUnitId);

            var conversion = await _conversionRepository.GetConversionAsync(fromUnitId, toUnitId, cancellationToken);
            if (conversion is not null)
            {
                return Ok(MapToConversionResponse(conversion, conversion.FromUnit, conversion.ToUnit));
            }

            // Fallback: if direct conversion is not configured, try reverse and invert factor.
            var reverseConversion = await _conversionRepository.GetConversionAsync(toUnitId, fromUnitId, cancellationToken);
            if (reverseConversion is null)
            {
                _logger.LogWarning("Conversion not found from {FromUnitId} to {ToUnitId}", fromUnitId, toUnitId);
                return NotFound(new { message = "Conversion not found" });
            }

            if (reverseConversion.ConversionFactor <= 0)
            {
                _logger.LogWarning("Invalid reverse conversion factor for {ToUnitId} -> {FromUnitId}", toUnitId, fromUnitId);
                return BadRequest(new { message = "Invalid conversion factor configured" });
            }

            var invertedFactor = 1 / reverseConversion.ConversionFactor;
            var response = new UnitConversionResponse
            {
                Id = reverseConversion.Id,
                FromUnitId = fromUnitId,
                ToUnitId = toUnitId,
                FromUnitName = reverseConversion.ToUnit?.Name ?? string.Empty,
                FromUnitCode = reverseConversion.ToUnit?.Code ?? string.Empty,
                ToUnitName = reverseConversion.FromUnit?.Name ?? string.Empty,
                ToUnitCode = reverseConversion.FromUnit?.Code ?? string.Empty,
                ConversionFactor = invertedFactor,
                Description = reverseConversion.Description,
                IsActive = reverseConversion.IsActive,
                CreatedBy = reverseConversion.CreatedBy,
                ModifiedBy = reverseConversion.ModifiedBy
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversion");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the conversion");
        }
    }

    /// <summary>
    /// Create a new unit conversion
    /// </summary>
    [HttpPost("conversions")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UnitConversionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateConversion(CreateUnitConversionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.FromUnitId == Guid.Empty || request.ToUnitId == Guid.Empty)
                return BadRequest(new { message = "FromUnitId and ToUnitId are required" });

            if (request.ConversionFactor <= 0)
                return BadRequest(new { message = "ConversionFactor must be greater than 0" });

            _logger.LogInformation("Creating conversion from {FromUnitId} to {ToUnitId}", request.FromUnitId, request.ToUnitId);

            // Verify units exist
            var fromUnit = await _unitRepository.GetByIdAsync(request.FromUnitId, cancellationToken);
            var toUnit = await _unitRepository.GetByIdAsync(request.ToUnitId, cancellationToken);

            if (fromUnit is null || toUnit is null)
            {
                _logger.LogWarning("One or both units not found");
                return BadRequest(new { message = "One or both units do not exist" });
            }

            // Check if conversion already exists
            if (await _conversionRepository.ConversionExistsAsync(request.FromUnitId, request.ToUnitId, cancellationToken))
            {
                _logger.LogWarning("Conversion already exists from {FromUnitId} to {ToUnitId}", request.FromUnitId, request.ToUnitId);
                return Conflict(new { message = "Conversion already exists between these units" });
            }

            var conversion = UnitConversion.Create(request.FromUnitId, request.ToUnitId, request.ConversionFactor, request.Description);
            var currentUser = _currentUserService.GetCurrentUsername();
            conversion.CreatedBy = currentUser;
            conversion.ModifiedBy = currentUser;

            await _conversionRepository.AddAsync(conversion, cancellationToken);

            var response = MapToConversionResponse(conversion, fromUnit, toUnit);
            return CreatedAtAction(nameof(GetConversion), new { fromUnitId = conversion.FromUnitId, toUnitId = conversion.ToUnitId }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating conversion");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating conversion");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversion");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the conversion");
        }
    }

    /// <summary>
    /// Update a unit conversion
    /// </summary>
    [HttpPut("conversions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnitConversionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConversion(Guid id, UpdateUnitConversionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.ConversionFactor <= 0)
                return BadRequest(new { message = "ConversionFactor must be greater than 0" });

            _logger.LogInformation("Updating conversion: {ConversionId}", id);

            var conversion = await _conversionRepository.GetByIdAsync(id, cancellationToken);
            if (conversion is null)
            {
                _logger.LogWarning("Conversion not found: {ConversionId}", id);
                return NotFound(new { message = "Conversion not found" });
            }

            conversion.Update(request.ConversionFactor, request.Description, request.IsActive);
            conversion.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _conversionRepository.UpdateAsync(conversion, cancellationToken);

            var response = MapToConversionResponse(conversion, conversion.FromUnit, conversion.ToUnit);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating conversion");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversion: {ConversionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the conversion");
        }
    }

    /// <summary>
    /// Delete a unit conversion
    /// </summary>
    [HttpDelete("conversions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversion(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting conversion: {ConversionId}", id);

            var exists = await _conversionRepository.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Conversion not found: {ConversionId}", id);
                return NotFound(new { message = "Conversion not found" });
            }

            await _conversionRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversion: {ConversionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the conversion");
        }
    }

    /// <summary>
    /// Get compatible units for a given base unit
    /// Returns the base unit itself plus all units that have conversions configured
    /// </summary>
    [HttpGet("{id:guid}/compatible-units")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UnitResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompatibleUnits(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting compatible units for base unit: {UnitId}", id);

            var baseUnit = await _unitRepository.GetByIdAsync(id, cancellationToken);
            if (baseUnit is null)
            {
                _logger.LogWarning("Base unit not found: {UnitId}", id);
                return NotFound(new { message = "Base unit not found" });
            }

            // Get all conversions for this unit
            var conversions = await _conversionRepository.GetAllConversionsForUnitAsync(id, cancellationToken);

            // Extract unique unit IDs from conversions (both from and to)
            var compatibleUnitIds = new HashSet<Guid> { id }; // Start with base unit itself
            foreach (var conversion in conversions.Where(c => c.IsActive))
            {
                if (conversion.FromUnitId == id)
                    compatibleUnitIds.Add(conversion.ToUnitId);
                else if (conversion.ToUnitId == id)
                    compatibleUnitIds.Add(conversion.FromUnitId);
            }

            // Fetch all compatible units
            var allUnits = await _unitRepository.GetAllAsync(cancellationToken);
            var compatibleUnits = allUnits.Where(u => compatibleUnitIds.Contains(u.Id) && u.IsActive);

            var response = compatibleUnits.Select(u => MapToResponse(u));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compatible units for unit: {UnitId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving compatible units");
        }
    }

    // ==================== HELPER METHODS ====================

    private UnitResponse MapToResponse(Unit unit)
    {
        return new UnitResponse
        {
            Id = unit.Id,
            Name = unit.Name,
            Code = unit.Code,
            Symbol = unit.Symbol,
            Description = unit.Description,
            IsActive = unit.IsActive,
            DisplayOrder = unit.DisplayOrder,
            CreatedBy = unit.CreatedBy,
            ModifiedBy = unit.ModifiedBy
        };
    }

    private UnitConversionResponse MapToConversionResponse(UnitConversion conversion, Unit? fromUnit = null, Unit? toUnit = null)
    {
        return new UnitConversionResponse
        {
            Id = conversion.Id,
            FromUnitId = conversion.FromUnitId,
            ToUnitId = conversion.ToUnitId,
            FromUnitName = fromUnit?.Name ?? string.Empty,
            FromUnitCode = fromUnit?.Code ?? string.Empty,
            ToUnitName = toUnit?.Name ?? string.Empty,
            ToUnitCode = toUnit?.Code ?? string.Empty,
            ConversionFactor = conversion.ConversionFactor,
            Description = conversion.Description,
            IsActive = conversion.IsActive,
            CreatedBy = conversion.CreatedBy,
            ModifiedBy = conversion.ModifiedBy
        };
    }
}
