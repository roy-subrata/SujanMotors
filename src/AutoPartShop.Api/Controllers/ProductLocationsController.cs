using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.ProductLocationDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Warehouse bin/shelf locations for a product — a join to a printed, structured
/// <see cref="WarehouseLocation"/> bin (see WarehouseLocationsController), not free text.
/// All endpoints are scoped under the owning product.
/// </summary>
[Route("api/v1/products/{productId:guid}/locations")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.InventoryView)]
public class ProductLocationsController : ControllerBase
{
    private readonly IProductLocationRepository _locationRepository;
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseLocationRepository _warehouseLocationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProductLocationsController> _logger;

    public ProductLocationsController(
        IProductLocationRepository locationRepository,
        IProductRepository productRepository,
        IWarehouseLocationRepository warehouseLocationRepository,
        ICurrentUserService currentUserService,
        ILogger<ProductLocationsController> logger)
    {
        _locationRepository = locationRepository;
        _productRepository = productRepository;
        _warehouseLocationRepository = warehouseLocationRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // GET /api/v1/products/{productId}/locations?warehouseId=
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid productId, [FromQuery] Guid? warehouseId, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
            return NotFound(ApiError.NotFound($"Product '{productId}' not found", Request.Path));

        var locations = await _locationRepository.GetLocationsByPartAsync(productId, warehouseId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(locations.Select(MapToResponse)));
    }

    // GET /api/v1/products/{productId}/locations/primary
    [HttpGet("primary")]
    public async Task<IActionResult> GetPrimary(Guid productId, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
            return NotFound(ApiError.NotFound($"Product '{productId}' not found", Request.Path));

        var location = await _locationRepository.GetPrimaryLocationByPartAsync(productId, cancellationToken);
        if (location is null)
            return NotFound(ApiError.NotFound("No primary location found for this product", Request.Path));

        return Ok(ApiResponse<ProductLocationResponse>.Ok(MapToResponse(location)));
    }

    // GET /api/v1/products/{productId}/locations/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
        if (location is null || location.PartId != productId)
            return NotFound(ApiError.NotFound($"Location '{id}' not found on product '{productId}'", Request.Path));

        return Ok(ApiResponse<ProductLocationResponse>.Ok(MapToResponse(location)));
    }

    // POST /api/v1/products/{productId}/locations
    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateLocationBody body, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
            return NotFound(ApiError.NotFound($"Product '{productId}' not found", Request.Path));

        if (!await _warehouseLocationRepository.ExistsAsync(body.WarehouseLocationId, cancellationToken))
            return BadRequest(ApiError.Validation("Warehouse location not found", instance: Request.Path));

        if (await _locationRepository.LocationExistsAsync(productId, body.WarehouseLocationId, null, cancellationToken))
            return Conflict(ApiError.Conflict("This product is already assigned to this location", Request.Path));

        var location = ProductLocation.Create(productId, body.WarehouseLocationId, body.IsPrimary, body.Notes);
        var user = _currentUserService.GetCurrentUsername();
        location.CreatedBy = user;
        location.ModifiedBy = user;

        await _locationRepository.AddAsync(location, cancellationToken);

        if (body.IsPrimary)
            await _locationRepository.SetPrimaryLocationAsync(productId, location.Id, cancellationToken);

        var created = await _locationRepository.GetByIdAsync(location.Id, cancellationToken) ?? location;

        return CreatedAtAction(nameof(GetById), new { productId, id = location.Id },
            ApiResponse<ProductLocationResponse>.Ok(MapToResponse(created)));
    }

    // PUT /api/v1/products/{productId}/locations/{id}
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateProductLocationRequest body, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
        if (location is null || location.PartId != productId)
            return NotFound(ApiError.NotFound($"Location '{id}' not found on product '{productId}'", Request.Path));

        if (!await _warehouseLocationRepository.ExistsAsync(body.WarehouseLocationId, cancellationToken))
            return BadRequest(ApiError.Validation("Warehouse location not found", instance: Request.Path));

        if (await _locationRepository.LocationExistsAsync(productId, body.WarehouseLocationId, id, cancellationToken))
            return Conflict(ApiError.Conflict("This product is already assigned to this location", Request.Path));

        location.Update(body.WarehouseLocationId, body.IsPrimary, body.Notes);
        location.ModifiedBy = _currentUserService.GetCurrentUsername();

        await _locationRepository.UpdateAsync(location, cancellationToken);

        if (body.IsPrimary)
            await _locationRepository.SetPrimaryLocationAsync(productId, location.Id, cancellationToken);

        var updated = await _locationRepository.GetByIdAsync(location.Id, cancellationToken) ?? location;
        return Ok(ApiResponse<ProductLocationResponse>.Ok(MapToResponse(updated)));
    }

    // PATCH /api/v1/products/{productId}/locations/{id}/set-primary
    [HttpPatch("{id:guid}/set-primary")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> SetPrimary(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
        if (location is null || location.PartId != productId)
            return NotFound(ApiError.NotFound($"Location '{id}' not found on product '{productId}'", Request.Path));

        await _locationRepository.SetPrimaryLocationAsync(productId, id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { message = "Primary location updated" }));
    }

    // DELETE /api/v1/products/{productId}/locations/{id}
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.InventoryDelete)]
    public async Task<IActionResult> Delete(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken);
        if (location is null || location.PartId != productId)
            return NotFound(ApiError.NotFound($"Location '{id}' not found on product '{productId}'", Request.Path));

        await _locationRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static ProductLocationResponse MapToResponse(ProductLocation location) => new()
    {
        Id = location.Id,
        PartId = location.PartId,
        PartName = location.Part?.Name ?? string.Empty,
        PartSKU = location.Part?.SKU ?? string.Empty,
        WarehouseLocationId = location.WarehouseLocationId,
        WarehouseId = location.Location?.WarehouseId ?? Guid.Empty,
        WarehouseName = location.Location?.Warehouse?.Name ?? string.Empty,
        WarehouseCode = location.Location?.Warehouse?.Code ?? string.Empty,
        Zone = location.Location?.Zone ?? string.Empty,
        Aisle = location.Location?.Aisle ?? string.Empty,
        Rack = location.Location?.Rack ?? string.Empty,
        Bin = location.Location?.Bin ?? string.Empty,
        LocationCode = location.Location?.GetLocationCode() ?? string.Empty,
        Notes = location.Notes,
        IsPrimary = location.IsPrimary,
        CreatedBy = location.CreatedBy,
        CreatedAt = location.CreatedDate
    };
}

// Request body — productId comes from the URL, not the body
public record CreateLocationBody(
    Guid WarehouseLocationId,
    bool IsPrimary,
    string? Notes);
