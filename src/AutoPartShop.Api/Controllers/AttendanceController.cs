using AutoPartShop.Api.Services;
using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Hr.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Daily attendance marking and monthly summaries. Manual entry by Admin/Manager (v1).
/// </summary>
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceReadRepository _attendanceReadRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        IAttendanceRepository attendanceRepository,
        IAttendanceReadRepository attendanceReadRepository,
        ICurrentUserService currentUserService,
        ILogger<AttendanceController> logger)
    {
        _attendanceRepository = attendanceRepository;
        _attendanceReadRepository = attendanceReadRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>Daily sheet: all active employees with their mark (if any) for the given date.</summary>
    [HttpGet("daily")]
    public async Task<IActionResult> GetDailySheet([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        try
        {
            if (date == default)
                return BadRequest(new { message = "Date is required" });

            var rows = await _attendanceReadRepository.GetDailySheet(date, cancellationToken);
            return Ok(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily attendance sheet");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>Bulk insert/update marks for one date.</summary>
    [HttpPost("daily")]
    public async Task<IActionResult> MarkDaily(MarkAttendanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || request.Date == default)
                return BadRequest(new { message = "Date is required" });

            if (request.Date.Date > DateTime.UtcNow.Date.AddDays(1))
                return BadRequest(new { message = "Cannot mark attendance for a future date" });

            if (request.Entries.Count == 0)
                return BadRequest(new { message = "No attendance entries supplied" });

            var records = request.Entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Status))
                .Select(e => AttendanceRecord.Create(e.EmployeeId, request.Date, e.Status, e.CheckInTime, e.CheckOutTime, e.Notes))
                .ToList();

            await _attendanceRepository.UpsertRangeAsync(records, _currentUserService.GetCurrentUsername(), cancellationToken);

            var rows = await _attendanceReadRepository.GetDailySheet(request.Date, cancellationToken);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking daily attendance");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>Per-employee status counts for a month — feeds the payroll run (Phase 3).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        try
        {
            if (year < 2000 || year > 2100 || month < 1 || month > 12)
                return BadRequest(new { message = "Invalid year or month" });

            var rows = await _attendanceReadRepository.GetMonthlySummary(year, month, cancellationToken);
            return Ok(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly attendance summary");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>One employee's day-by-day records for a month.</summary>
    [HttpGet("employee/{employeeId:guid}")]
    public async Task<IActionResult> GetEmployeeMonth(Guid employeeId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        try
        {
            if (year < 2000 || year > 2100 || month < 1 || month > 12)
                return BadRequest(new { message = "Invalid year or month" });

            var records = await _attendanceRepository.GetByEmployeeMonthAsync(employeeId, year, month, cancellationToken);
            var response = records.Select(r => new AttendanceRecordResponse
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                Date = r.Date,
                CheckInTime = r.CheckInTime,
                CheckOutTime = r.CheckOutTime,
                Status = r.Status,
                Notes = r.Notes
            });
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee month attendance");
            return StatusCode(500, "An error occurred");
        }
    }
}
