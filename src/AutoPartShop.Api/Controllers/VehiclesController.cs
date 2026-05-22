using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.VehicleDtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IPartVehicleCompatibilityRepository _compatibilityRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<VehiclesController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public VehiclesController(IVehicleRepository vehicleRepository, IPartVehicleCompatibilityRepository compatibilityRepository,
        IProductRepository productRepository, ICurrentUserService currentUserService, ILogger<VehiclesController> logger)
    {
        _vehicleRepository = vehicleRepository;
        _compatibilityRepository = compatibilityRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var vehicles = await _vehicleRepository.GetAllAsync(cancellationToken);
            var response = vehicles.Select(v => MapToResponse(v));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all vehicles");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving vehicles");
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var vehicles = await _vehicleRepository.GetAllActiveAsync(cancellationToken);
            var response = vehicles.Select(v => MapToResponse(v));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active vehicles");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active vehicles");
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

            var (vehicles, totalCount) = await _vehicleRepository.SearchPagedAsync(searchTerm ?? string.Empty, pageNumber, pageSize, cancellationToken);
            var response = vehicles.Select(v => MapToResponse(v));
            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicles list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving vehicles");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
            if (vehicle is null) return NotFound(new { message = "Vehicle not found" });

            return Ok(MapToResponse(vehicle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle by ID: {VehicleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the vehicle");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Make) || string.IsNullOrWhiteSpace(request.Model) ||
                string.IsNullOrWhiteSpace(request.EngineType) || request.Year < 1900)
                return BadRequest(new { message = "Make, Model, EngineType, and valid Year are required" });

            var vehicle = Vehicle.Create(request.Make, request.Model, request.Year, request.EngineType, request.Description);
            var currentUser = _currentUserService.GetCurrentUsername();
            vehicle.CreatedBy = currentUser;
            vehicle.ModifiedBy = currentUser;

            await _vehicleRepository.AddAsync(vehicle, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, MapToResponse(vehicle));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the vehicle");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
            if (vehicle is null) return NotFound(new { message = "Vehicle not found" });

            vehicle.Update(request.Make, request.Model, request.Year, request.EngineType, request.Description, request.IsActive);
            vehicle.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);

            return Ok(MapToResponse(vehicle));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle: {VehicleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the vehicle");
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
            if (vehicle is null) return NotFound(new { message = "Vehicle not found" });

            vehicle.Activate();
            vehicle.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);

            return Ok(MapToResponse(vehicle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating vehicle: {VehicleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while activating the vehicle");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
            if (vehicle is null) return NotFound(new { message = "Vehicle not found" });

            vehicle.Deactivate();
            vehicle.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);

            return Ok(MapToResponse(vehicle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating vehicle: {VehicleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the vehicle");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _vehicleRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Vehicle not found" });

            await _vehicleRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle: {VehicleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the vehicle");
        }
    }

    // Part Compatibility Endpoints
    [HttpPost("{vehicleId:guid}/parts/{partId:guid}/compatibility")]
    public async Task<IActionResult> AddPartCompatibility(Guid vehicleId, Guid partId, CreatePartCompatibilityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _vehicleRepository.ExistsAsync(vehicleId, cancellationToken) || !await _productRepository.ExistsAsync(partId, cancellationToken))
                return BadRequest(new { message = "Vehicle or Part does not exist" });

            var existing = await _compatibilityRepository.GetCompatibilityAsync(partId, vehicleId, cancellationToken);
            if (existing != null)
                return Conflict(new { message = "Compatibility already exists" });

            var compatibility = PartVehicleCompatibility.Create(partId, vehicleId, request.IsCompatible, request.Notes);
            var currentUser = _currentUserService.GetCurrentUsername();
            compatibility.CreatedBy = currentUser;
            compatibility.ModifiedBy = currentUser;

            await _compatibilityRepository.AddAsync(compatibility, cancellationToken);

            return CreatedAtAction(nameof(GetCompatibilities), new { vehicleId }, MapToCompatibilityResponse(compatibility));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding part compatibility");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding compatibility");
        }
    }

    [HttpGet("{vehicleId:guid}/compatibilities")]
    public async Task<IActionResult> GetCompatibilities(Guid vehicleId, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _vehicleRepository.ExistsAsync(vehicleId, cancellationToken))
                return NotFound(new { message = "Vehicle not found" });

            var compatibilities = await _compatibilityRepository.GetCompatibilitiesByVehicleAsync(vehicleId, cancellationToken);
            var response = compatibilities.Select(c => MapToCompatibilityResponse(c));

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compatibilities");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving compatibilities");
        }
    }

    [HttpDelete("compatibilities/{compatibilityId:guid}")]
    public async Task<IActionResult> RemoveCompatibility(Guid compatibilityId, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _compatibilityRepository.ExistsAsync(compatibilityId, cancellationToken))
                return NotFound(new { message = "Compatibility not found" });

            await _compatibilityRepository.DeleteAsync(compatibilityId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing compatibility");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing compatibility");
        }
    }

    private VehicleResponse MapToResponse(Vehicle vehicle)
    {
        return new VehicleResponse
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            EngineType = vehicle.EngineType,
            Description = vehicle.Description,
            IsActive = vehicle.IsActive,
            CreatedBy = vehicle.CreatedBy,
            ModifiedBy = vehicle.ModifiedBy
        };
    }

    private PartCompatibilityResponse MapToCompatibilityResponse(PartVehicleCompatibility compatibility)
    {
        return new PartCompatibilityResponse
        {
            Id = compatibility.Id,
            PartId = compatibility.PartId,
            PartName = compatibility.Part?.Name ?? string.Empty,
            PartSKU = compatibility.Part?.SKU ?? string.Empty,
            VehicleId = compatibility.VehicleId,
            VehicleInfo = compatibility.Vehicle != null ? $"{compatibility.Vehicle.Make} {compatibility.Vehicle.Model} {compatibility.Vehicle.Year}" : string.Empty,
            IsCompatible = compatibility.IsCompatible,
            Notes = compatibility.Notes,
            CreatedBy = compatibility.CreatedBy
        };
    }
}
