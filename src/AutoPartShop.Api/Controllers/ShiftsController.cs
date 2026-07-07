using AutoPartShop.Api.Services;
using AutoPartShop.Application.Hr.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class ShiftsController : ControllerBase
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ShiftsController> _logger;

    public ShiftsController(
        IShiftRepository shiftRepository,
        ICurrentUserService currentUserService,
        ILogger<ShiftsController> logger)
    {
        _shiftRepository = shiftRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var shifts = await _shiftRepository.GetAllAsync(cancellationToken);
            return Ok(shifts.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shifts");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveShiftRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var shift = Shift.Create(request.Name, request.StartTime, request.EndTime, request.GraceMinutes, request.Notes);

            var currentUser = _currentUserService.GetCurrentUsername();
            shift.CreatedBy = currentUser;
            shift.ModifiedBy = currentUser;

            await _shiftRepository.AddAsync(shift, cancellationToken);

            return Ok(MapToResponse(shift));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveShiftRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var shift = await _shiftRepository.GetByIdAsync(id, cancellationToken);
            if (shift is null) return NotFound();

            shift.Update(request.Name, request.StartTime, request.EndTime, request.GraceMinutes, request.Notes);
            shift.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _shiftRepository.UpdateAsync(shift, cancellationToken);

            return Ok(MapToResponse(shift));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var shift = await _shiftRepository.GetByIdAsync(id, cancellationToken);
            if (shift is null) return NotFound();

            if (await _shiftRepository.IsInUseAsync(id, cancellationToken))
                return BadRequest(new { message = "Shift is assigned to employees; reassign them first" });

            await _shiftRepository.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shift");
            return StatusCode(500, "An error occurred");
        }
    }

    private static ShiftResponse MapToResponse(Shift s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        GraceMinutes = s.GraceMinutes,
        Notes = s.Notes
    };
}
