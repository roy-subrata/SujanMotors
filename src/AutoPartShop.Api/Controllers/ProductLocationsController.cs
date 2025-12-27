using AutoPartShop.Application.DTOs.ProductLocationDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class ProductLocationsController : ControllerBase
{
    private readonly IProductLocationRepository _locationRepository;
    private readonly IPartRepository _partRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<ProductLocationsController> _logger;

    public ProductLocationsController(
        IProductLocationRepository locationRepository,
        IPartRepository partRepository,
        IWarehouseRepository warehouseRepository,
        ILogger<ProductLocationsController> logger)
    {
        _locationRepository = locationRepository;
        _partRepository = partRepository;
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all product locations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var locations = await _locationRepository.GetAllAsync(cancellationToken);
            var response = locations.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all product locations");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving product locations");
        }
    }

    /// <summary>
    /// Get product location by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
            if (location is null)
                return NotFound(new { message = "Product location not found" });

            return Ok(MapToResponse(location));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product location by ID: {LocationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the product location");
        }
    }

    /// <summary>
    /// Get all locations for a specific part
    /// </summary>
    [HttpGet("part/{partId:guid}")]
    public async Task<IActionResult> GetByPart(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var locations = await _locationRepository.GetLocationsByPartAsync(partId, cancellationToken);
            var response = locations.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving product locations");
        }
    }

    /// <summary>
    /// Get all locations in a specific warehouse
    /// </summary>
    [HttpGet("warehouse/{warehouseId:guid}")]
    public async Task<IActionResult> GetByWarehouse(Guid warehouseId, CancellationToken cancellationToken)
    {
        try
        {
            var locations = await _locationRepository.GetLocationsByWarehouseAsync(warehouseId, cancellationToken);
            var response = locations.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for warehouse: {WarehouseId}", warehouseId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving product locations");
        }
    }

    /// <summary>
    /// Get primary location for a part
    /// </summary>
    [HttpGet("part/{partId:guid}/primary")]
    public async Task<IActionResult> GetPrimaryLocation(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var location = await _locationRepository.GetPrimaryLocationByPartAsync(partId, cancellationToken);
            if (location is null)
                return NotFound(new { message = "No primary location found for this part" });

            return Ok(MapToResponse(location));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting primary location for part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the primary location");
        }
    }

    /// <summary>
    /// Create a new product location
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate part exists
            if (!await _partRepository.ExistsAsync(request.PartId, cancellationToken))
                return BadRequest(new { message = "Part not found" });

            // Validate warehouse exists
            if (!await _warehouseRepository.ExistsAsync(request.WarehouseId, cancellationToken))
                return BadRequest(new { message = "Warehouse not found" });

            // Check for duplicate location
            if (await _locationRepository.LocationExistsAsync(request.PartId, request.WarehouseId, request.Section, request.Shelf, null, cancellationToken))
                return Conflict(new { message = "A location with this section and shelf already exists for this part in this warehouse" });

            var location = ProductLocation.Create(
                request.PartId,
                request.WarehouseId,
                request.Section,
                request.Shelf,
                request.IsPrimary,
                request.Notes);

            location.CreatedBy = "System";
            location.ModifiedBy = "System";

            await _locationRepository.AddAsync(location, cancellationToken);

            // If this is set as primary, ensure no other locations are primary
            if (request.IsPrimary)
            {
                await _locationRepository.SetPrimaryLocationAsync(request.PartId, location.Id, cancellationToken);
            }

            return CreatedAtAction(nameof(GetById), new { id = location.Id }, MapToResponse(location));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product location");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the product location");
        }
    }

    /// <summary>
    /// Update a product location
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProductLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
            if (location is null)
                return NotFound(new { message = "Product location not found" });

            // Check for duplicate location (excluding current)
            if (await _locationRepository.LocationExistsAsync(location.PartId, location.WarehouseId, request.Section, request.Shelf, id, cancellationToken))
                return Conflict(new { message = "A location with this section and shelf already exists for this part in this warehouse" });

            location.Update(request.Section, request.Shelf, request.IsPrimary, request.Notes);
            location.ModifiedBy = "System";

            await _locationRepository.UpdateAsync(location, cancellationToken);

            // If this is set as primary, ensure no other locations are primary
            if (request.IsPrimary)
            {
                await _locationRepository.SetPrimaryLocationAsync(location.PartId, location.Id, cancellationToken);
            }

            return Ok(MapToResponse(location));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product location: {LocationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the product location");
        }
    }

    /// <summary>
    /// Set a location as the primary location for a part
    /// </summary>
    [HttpPatch("part/{partId:guid}/set-primary")]
    public async Task<IActionResult> SetPrimary(Guid partId, SetPrimaryLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var location = await _locationRepository.GetByIdAsync(request.LocationId, cancellationToken);
            if (location is null)
                return NotFound(new { message = "Product location not found" });

            if (location.PartId != partId)
                return BadRequest(new { message = "Location does not belong to the specified part" });

            await _locationRepository.SetPrimaryLocationAsync(partId, request.LocationId, cancellationToken);

            return Ok(new { message = "Primary location updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary location for part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while setting the primary location");
        }
    }

    /// <summary>
    /// Delete a product location
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _locationRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Product location not found" });

            await _locationRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product location: {LocationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the product location");
        }
    }

    private ProductLocationResponse MapToResponse(ProductLocation location)
    {
        return new ProductLocationResponse
        {
            Id = location.Id,
            PartId = location.PartId,
            PartName = location.Part?.Name ?? string.Empty,
            PartSKU = location.Part?.SKU ?? string.Empty,
            WarehouseId = location.WarehouseId,
            WarehouseName = location.Warehouse?.Name ?? string.Empty,
            WarehouseCode = location.Warehouse?.Code ?? string.Empty,
            Section = location.Section,
            Shelf = location.Shelf,
            FullLocation = location.GetFullLocation(),
            Notes = location.Notes,
            IsPrimary = location.IsPrimary,
            CreatedBy = location.CreatedBy,
            CreatedAt = location.CreatedDate
        };
    }
}
