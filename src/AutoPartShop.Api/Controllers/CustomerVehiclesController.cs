using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CustomerVehicleDtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/customers/{customerId:guid}/vehicles")]
[Route("api/v1/customers/{customerId:guid}/vehicles")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class CustomerVehiclesController : ControllerBase
{
    private readonly ICustomerVehicleRepository _vehicleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CustomerVehiclesController> _logger;

    public CustomerVehiclesController(
        ICustomerVehicleRepository vehicleRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService,
        ILogger<CustomerVehiclesController> logger)
    {
        _vehicleRepository = vehicleRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetByCustomer(Guid customerId, [FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var vehicles = await _vehicleRepository.GetByCustomerAsync(customerId, activeOnly, cancellationToken);
            return Ok(vehicles.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicles for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the customer's vehicles");
        }
    }

    [HttpGet("{vehicleId:guid}")]
    public async Task<IActionResult> GetById(Guid customerId, Guid vehicleId, CancellationToken cancellationToken)
    {
        try
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
            if (vehicle is null || vehicle.CustomerId != customerId)
                return NotFound(new { message = "Vehicle not found" });

            return Ok(MapToResponse(vehicle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle: {VehicleId}", vehicleId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the vehicle");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid customerId, CreateCustomerVehicleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _customerRepository.ExistsAsync(customerId, cancellationToken))
                return NotFound(new { message = "Customer not found" });

            var vehicle = CustomerVehicle.Create(
                customerId,
                request.RegistrationNo,
                request.Make,
                request.Model,
                request.Year,
                request.EngineType,
                request.VIN,
                request.Color,
                request.Mileage,
                request.Notes,
                request.CatalogVehicleId
            );

            var currentUser = _currentUserService.GetCurrentUsername();
            vehicle.CreatedBy = currentUser;
            vehicle.ModifiedBy = currentUser;

            await _vehicleRepository.AddAsync(vehicle, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { customerId, vehicleId = vehicle.Id }, MapToResponse(vehicle));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the vehicle");
        }
    }

    [HttpPut("{vehicleId:guid}")]
    public async Task<IActionResult> Update(Guid customerId, Guid vehicleId, UpdateCustomerVehicleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
            if (vehicle is null || vehicle.CustomerId != customerId)
                return NotFound(new { message = "Vehicle not found" });

            vehicle.Update(
                request.RegistrationNo,
                request.Make,
                request.Model,
                request.Year,
                request.EngineType,
                request.VIN,
                request.Color,
                request.Mileage,
                request.Notes,
                request.CatalogVehicleId
            );

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
            _logger.LogError(ex, "Error updating vehicle: {VehicleId}", vehicleId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the vehicle");
        }
    }

    [HttpDelete("{vehicleId:guid}")]
    public async Task<IActionResult> Delete(Guid customerId, Guid vehicleId, CancellationToken cancellationToken)
    {
        try
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
            if (vehicle is null || vehicle.CustomerId != customerId)
                return NotFound(new { message = "Vehicle not found" });

            await _vehicleRepository.DeleteAsync(vehicleId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle: {VehicleId}", vehicleId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the vehicle");
        }
    }

    private static CustomerVehicleResponse MapToResponse(CustomerVehicle v) => new()
    {
        Id = v.Id,
        CustomerId = v.CustomerId,
        RegistrationNo = v.RegistrationNo,
        VIN = v.VIN,
        Make = v.Make,
        Model = v.Model,
        Year = v.Year,
        EngineType = v.EngineType,
        Color = v.Color,
        Mileage = v.Mileage,
        Notes = v.Notes,
        CatalogVehicleId = v.CatalogVehicleId,
        Label = v.GetLabel(),
        IsActive = v.IsActive,
        CreatedAt = v.CreatedDate
    };
}
