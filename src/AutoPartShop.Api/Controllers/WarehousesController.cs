using AutoPartShop.Application.DTOs.WarehouseDtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<WarehousesController> _logger;

    public WarehousesController(IWarehouseRepository warehouseRepository, ILogger<WarehousesController> logger)
    {
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var warehouses = await _warehouseRepository.GetAllAsync(cancellationToken);
            var response = warehouses.Select(w => MapToResponse(w));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all warehouses");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving warehouses");
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var warehouses = await _warehouseRepository.GetAllActiveAsync(cancellationToken);
            var response = warehouses.Select(w => MapToResponse(w));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active warehouses");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active warehouses");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (warehouses, totalCount) = await _warehouseRepository.SearchPagedAsync(searchTerm ?? string.Empty, pageNumber, pageSize, cancellationToken);

            var response = warehouses.Select(w => MapToResponse(w));
            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
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
            var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            return Ok(MapToResponse(warehouse));
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
            var warehouse = await _warehouseRepository.GetByCodeAsync(code, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            return Ok(MapToResponse(warehouse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouse by code: {WarehouseCode}", code);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the warehouse");
        }
    }

    [HttpGet("city/{city}")]
    public async Task<IActionResult> GetByCity(string city, CancellationToken cancellationToken)
    {
        try
        {
            var warehouses = await _warehouseRepository.GetByCityAsync(city, cancellationToken);
            var response = warehouses.Select(w => MapToResponse(w));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouses by city: {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving warehouses");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Location))
                return BadRequest(new { message = "Name, and Location are required" });

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
            warehouse.CreatedBy = "System";
            warehouse.ModifiedBy = "System";

            await _warehouseRepository.AddAsync(warehouse, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, MapToResponse(warehouse));
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
            warehouse.ModifiedBy = "System";

            await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);

            return Ok(MapToResponse(warehouse));
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
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            warehouse.Activate();
            warehouse.ModifiedBy = "System";
            await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);

            return Ok(MapToResponse(warehouse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating warehouse: {WarehouseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while activating the warehouse");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
            if (warehouse is null) return NotFound(new { message = "Warehouse not found" });

            warehouse.Deactivate();
            warehouse.ModifiedBy = "System";
            await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);

            return Ok(MapToResponse(warehouse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating warehouse: {WarehouseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the warehouse");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _warehouseRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Warehouse not found" });

            await _warehouseRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warehouse: {WarehouseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the warehouse");
        }
    }

    private WarehouseResponse MapToResponse(Warehouse warehouse)
    {
        return new WarehouseResponse
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            Code = warehouse.Code,
            Location = warehouse.Location,
            City = warehouse.City,
            State = warehouse.State,
            Country = warehouse.Country,
            PostalCode = warehouse.PostalCode,
            Manager = warehouse.Manager,
            ManagerEmail = warehouse.ManagerEmail,
            ManagerPhone = warehouse.ManagerPhone,
            StorageCapacity = warehouse.StorageCapacity,
            CapacityUnit = warehouse.CapacityUnit,
            Description = warehouse.Description,
            IsActive = warehouse.IsActive,
            CreatedBy = warehouse.CreatedBy,
            ModifiedBy = warehouse.ModifiedBy
        };
    }
}
