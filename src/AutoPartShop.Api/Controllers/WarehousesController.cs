using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.WarehouseDtos;
using AutoPartShop.Application.Warehouse;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.InventoryView)]
public class WarehousesController(
    IWarehouseRepository _warehouseRepository,
    IWarehouseReadRepository _warehouseReadRepository,
    ICurrentUserService _currentUserService,
    ILogger<WarehousesController> _logger
) : ControllerBase
{

    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] WarehouseQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {

            var (warehouses, total) = await _warehouseReadRepository.FindAllAsync(query, cancellationToken);
            return Ok(PagedResult<WarehouseResponse>.Create(warehouses, total, query));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouses list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving warehouses");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = await _warehouseReadRepository.GetByIdAsync(id, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            return Ok(warehouse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouse by ID: {WarehouseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the warehouse");
        }
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = await _warehouseReadRepository.GetByCodeAsync(code, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            return Ok(warehouse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouse by code: {WarehouseCode}", code);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the warehouse");
        }
    }

    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    public async Task<IActionResult> Create(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Code) ||
                string.IsNullOrWhiteSpace(request.Location))
                return BadRequest(new { message = "Name, Code, and Location are required" });

            if (await _warehouseRepository.CodeExistsAsync(request.Code, null, cancellationToken))
                return Conflict(new { message = "Warehouse code already exists" });

            var warehouse = Warehouse.Create(
                request.Name,
                request.Code,
                request.Location,
                request.City,
                request.State,
                request.Country,
                request.PostalCode,
                request.Manager
                );

            warehouse.ManagerEmail = request.ManagerEmail;
            warehouse.ManagerPhone = request.ManagerPhone;
            warehouse.StorageCapacity = request.StorageCapacity;
            warehouse.CapacityUnit = request.CapacityUnit;
            warehouse.Description = request.Description;
            var currentUser = _currentUserService.GetCurrentUsername();
            warehouse.CreatedBy = currentUser;
            warehouse.ModifiedBy = currentUser;
            await _warehouseRepository.AddAsync(warehouse, cancellationToken);

            var created = await _warehouseReadRepository.GetByIdAsync(warehouse.Id, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, created ?? new WarehouseResponse { Id = warehouse.Id, Name = warehouse.Name, Code = warehouse.Code });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warehouse");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the warehouse");
        }
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> Update(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            warehouse.Update(request.Name, request.Location, request.City, request.State,
                request.Country, request.PostalCode, request.Manager, request.ManagerEmail,
                request.ManagerPhone, request.StorageCapacity, request.CapacityUnit,
                request.Description, request.IsActive);
            warehouse.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);

            var updated = await _warehouseReadRepository.GetByIdAsync(id, cancellationToken);
            return Ok(updated ?? new WarehouseResponse { Id = warehouse.Id, Name = warehouse.Name, Code = warehouse.Code });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating warehouse: {WarehouseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the warehouse");
        }
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            warehouse.Activate();
            warehouse.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);

            var updated = await _warehouseReadRepository.GetByIdAsync(id, cancellationToken);
            return Ok(updated ?? new WarehouseResponse { Id = warehouse.Id, Name = warehouse.Name, Code = warehouse.Code });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating warehouse: {WarehouseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while activating the warehouse");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            warehouse.Deactivate();
            warehouse.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);

            var updated = await _warehouseReadRepository.GetByIdAsync(id, cancellationToken);
            return Ok(updated ?? new WarehouseResponse { Id = warehouse.Id, Name = warehouse.Name, Code = warehouse.Code });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating warehouse: {WarehouseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the warehouse");
        }
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.InventoryDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            await _warehouseRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warehouse: {WarehouseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the warehouse");
        }
    }


}
