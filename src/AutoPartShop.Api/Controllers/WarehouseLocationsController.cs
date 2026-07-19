using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.WarehouseLocationDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Standalone warehouse bin/shelf locations (Zone-Aisle-Rack-Bin) — a physical slot that can be
/// printed as a barcode label and stuck on an empty shelf before any product is assigned there.
/// Not scoped under a product; a location exists independently of what (if anything) is stocked in it.
/// </summary>
[Route("api/v1/warehouse-locations")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.InventoryView)]
public class WarehouseLocationsController : ControllerBase
{
    private readonly IWarehouseLocationRepository _locationRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WarehouseLocationsController> _logger;

    public WarehouseLocationsController(
        IWarehouseLocationRepository locationRepository,
        IWarehouseRepository warehouseRepository,
        ICategoryRepository categoryRepository,
        ICurrentUserService currentUserService,
        ILogger<WarehouseLocationsController> logger)
    {
        _locationRepository = locationRepository;
        _warehouseRepository = warehouseRepository;
        _categoryRepository = categoryRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Paged, filterable list of warehouse locations.
    /// </summary>
    // GET /api/v1/warehouse-locations?warehouseId=&categoryId=&search=&pageNumber=&pageSize=
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        else if (pageSize > 100) pageSize = 100;

        var query = new WarehouseLocationQuery
        {
            WarehouseId = warehouseId,
            CategoryId = categoryId,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var (locations, totalCount) = await _locationRepository.SearchPagedAsync(query, cancellationToken);
        return Ok(PagedApiResponse<WarehouseLocationResponse>.Create(locations.Select(MapToResponse), totalCount, pageNumber, pageSize));
    }

    /// <summary>
    /// All locations in a single warehouse, unpaged — for populating a picker.
    /// </summary>
    // GET /api/v1/warehouse-locations/warehouse/{warehouseId}
    [HttpGet("warehouse/{warehouseId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByWarehouse(Guid warehouseId, CancellationToken cancellationToken)
    {
        if (!await _warehouseRepository.ExistsAsync(warehouseId, cancellationToken))
            return NotFound(ApiError.NotFound($"Warehouse '{warehouseId}' not found", Request.Path));

        var locations = await _locationRepository.GetByWarehouseAsync(warehouseId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(locations.Select(MapToResponse)));
    }

    // GET /api/v1/warehouse-locations/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
        if (location is null)
            return NotFound(ApiError.NotFound($"Warehouse location '{id}' not found", Request.Path));

        return Ok(ApiResponse<WarehouseLocationResponse>.Ok(MapToResponse(location)));
    }

    // POST /api/v1/warehouse-locations
    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseLocationRequest request, CancellationToken cancellationToken)
    {
        if (!await _warehouseRepository.ExistsAsync(request.WarehouseId, cancellationToken))
            return NotFound(ApiError.NotFound($"Warehouse '{request.WarehouseId}' not found", Request.Path));

        if (request.CategoryId.HasValue && !await _categoryRepository.ExistsAsync(request.CategoryId.Value, cancellationToken))
            return BadRequest(ApiError.Validation("Category not found", instance: Request.Path));

        if (await _locationRepository.LocationExistsAsync(request.WarehouseId, request.Zone, request.Aisle, request.Rack, request.Bin, null, cancellationToken))
            return Conflict(ApiError.Conflict("A location with this Zone/Aisle/Rack/Bin already exists in this warehouse", Request.Path));

        WarehouseLocation location;
        try
        {
            location = WarehouseLocation.Create(request.WarehouseId, request.Zone, request.Aisle, request.Rack, request.Bin, request.CategoryId, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message, instance: Request.Path));
        }

        var user = _currentUserService.GetCurrentUsername();
        location.CreatedBy = user;
        location.ModifiedBy = user;

        await _locationRepository.AddAsync(location, cancellationToken);

        // Re-fetch with navigation properties loaded for the response.
        var created = await _locationRepository.GetByIdAsync(location.Id, cancellationToken) ?? location;

        _logger.LogInformation("Warehouse location {LocationCode} created in warehouse {WarehouseId} by {User}",
            created.GetLocationCode(), created.WarehouseId, user);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<WarehouseLocationResponse>.Ok(MapToResponse(created)));
    }

    // PUT /api/v1/warehouse-locations/{id}
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseLocationRequest request, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
        if (location is null)
            return NotFound(ApiError.NotFound($"Warehouse location '{id}' not found", Request.Path));

        if (!await _warehouseRepository.ExistsAsync(request.WarehouseId, cancellationToken))
            return NotFound(ApiError.NotFound($"Warehouse '{request.WarehouseId}' not found", Request.Path));

        if (request.CategoryId.HasValue && !await _categoryRepository.ExistsAsync(request.CategoryId.Value, cancellationToken))
            return BadRequest(ApiError.Validation("Category not found", instance: Request.Path));

        if (await _locationRepository.LocationExistsAsync(request.WarehouseId, request.Zone, request.Aisle, request.Rack, request.Bin, id, cancellationToken))
            return Conflict(ApiError.Conflict("A location with this Zone/Aisle/Rack/Bin already exists in this warehouse", Request.Path));

        try
        {
            location.Update(request.WarehouseId, request.Zone, request.Aisle, request.Rack, request.Bin, request.CategoryId, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message, instance: Request.Path));
        }

        location.ModifiedBy = _currentUserService.GetCurrentUsername();

        await _locationRepository.UpdateAsync(location, cancellationToken);

        var updated = await _locationRepository.GetByIdAsync(location.Id, cancellationToken) ?? location;
        return Ok(ApiResponse<WarehouseLocationResponse>.Ok(MapToResponse(updated)));
    }

    // DELETE /api/v1/warehouse-locations/{id}
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.InventoryDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
        if (location is null)
            return NotFound(ApiError.NotFound($"Warehouse location '{id}' not found", Request.Path));

        await _locationRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static WarehouseLocationResponse MapToResponse(WarehouseLocation location) => new()
    {
        Id = location.Id,
        WarehouseId = location.WarehouseId,
        WarehouseName = location.Warehouse?.Name ?? string.Empty,
        WarehouseCode = location.Warehouse?.Code ?? string.Empty,
        Zone = location.Zone,
        Aisle = location.Aisle,
        Rack = location.Rack,
        Bin = location.Bin,
        LocationCode = location.GetLocationCode(),
        CategoryId = location.CategoryId,
        CategoryName = location.Category?.Name,
        Notes = location.Notes,
        IsActive = location.IsActive,
        CreatedBy = location.CreatedBy,
        CreatedAt = location.CreatedDate
    };
}
