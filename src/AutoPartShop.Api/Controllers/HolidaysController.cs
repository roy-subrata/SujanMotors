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
public class HolidaysController : ControllerBase
{
    private readonly IHolidayRepository _holidayRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<HolidaysController> _logger;

    public HolidaysController(
        IHolidayRepository holidayRepository,
        ICurrentUserService currentUserService,
        ILogger<HolidaysController> logger)
    {
        _holidayRepository = holidayRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetByYear([FromQuery] int year, CancellationToken cancellationToken)
    {
        try
        {
            if (year < 2000 || year > 2100)
                return BadRequest(new { message = "Invalid year" });

            var holidays = await _holidayRepository.GetByYearAsync(year, cancellationToken);
            return Ok(holidays.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holidays");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveHolidayRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (await _holidayRepository.ExistsOnDateAsync(request.Date, null, cancellationToken))
                return BadRequest(new { message = "A holiday already exists on this date" });

            var holiday = Holiday.Create(request.Date, request.Name);

            var currentUser = _currentUserService.GetCurrentUsername();
            holiday.CreatedBy = currentUser;
            holiday.ModifiedBy = currentUser;

            await _holidayRepository.AddAsync(holiday, cancellationToken);

            return Ok(MapToResponse(holiday));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating holiday");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveHolidayRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var holiday = await _holidayRepository.GetByIdAsync(id, cancellationToken);
            if (holiday is null) return NotFound();

            if (await _holidayRepository.ExistsOnDateAsync(request.Date, id, cancellationToken))
                return BadRequest(new { message = "A holiday already exists on this date" });

            holiday.Update(request.Date, request.Name);
            holiday.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _holidayRepository.UpdateAsync(holiday, cancellationToken);

            return Ok(MapToResponse(holiday));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating holiday");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var holiday = await _holidayRepository.GetByIdAsync(id, cancellationToken);
            if (holiday is null) return NotFound();

            await _holidayRepository.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting holiday");
            return StatusCode(500, "An error occurred");
        }
    }

    private static HolidayResponse MapToResponse(Holiday h) => new()
    {
        Id = h.Id,
        Date = h.Date,
        Name = h.Name
    };
}
