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
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        IAttendanceRepository attendanceRepository,
        IAttendanceReadRepository attendanceReadRepository,
        IEmployeeRepository employeeRepository,
        IShiftRepository shiftRepository,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ILogger<AttendanceController> logger)
    {
        _attendanceRepository = attendanceRepository;
        _attendanceReadRepository = attendanceReadRepository;
        _employeeRepository = employeeRepository;
        _shiftRepository = shiftRepository;
        _configuration = configuration;
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

    /// <summary>
    /// Device-facing check-in/out endpoint (fingerprint reader, QR kiosk, etc.).
    /// Authenticated by the X-Device-Key header (config Hr:DeviceApiKey), not JWT.
    /// First punch of the day = check-in (LATE when after shift start + grace),
    /// later punches update check-out.
    /// </summary>
    [HttpPost("punch")]
    [AllowAnonymous]
    public async Task<IActionResult> Punch(PunchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var configuredKey = _configuration["Hr:DeviceApiKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
                return StatusCode(503, new { message = "Device check-in is not configured (Hr:DeviceApiKey)" });

            if (!Request.Headers.TryGetValue("X-Device-Key", out var providedKey) || providedKey != configuredKey)
                return Unauthorized(new { message = "Invalid device key" });

            if (string.IsNullOrWhiteSpace(request.EmployeeCode))
                return BadRequest(new { message = "EmployeeCode is required" });

            var employee = await _employeeRepository.GetByCodeAsync(request.EmployeeCode.Trim().ToUpper(), cancellationToken);
            if (employee is null || employee.Status != "ACTIVE")
                return NotFound(new { message = "Unknown or inactive employee code" });

            var timestamp = request.Timestamp ?? DateTime.Now;
            var day = timestamp.Date;
            var time = new TimeSpan(timestamp.Hour, timestamp.Minute, 0);

            var existing = await _attendanceRepository.GetAsync(employee.Id, day, cancellationToken);
            if (existing is null)
            {
                // Check-in: shift decides PRESENT vs LATE
                var status = "PRESENT";
                if (employee.ShiftId is Guid shiftId)
                {
                    var shift = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
                    if (shift is not null && time > shift.StartTime.Add(TimeSpan.FromMinutes(shift.GraceMinutes)))
                        status = "LATE";
                }

                var record = AttendanceRecord.Create(employee.Id, day, status, checkInTime: time, notes: "Device punch");
                await _attendanceRepository.UpsertRangeAsync([record], "device", cancellationToken);

                return Ok(new { employee.EmployeeCode, employee.Name, action = "CHECK_IN", status, time = time.ToString(@"hh\:mm") });
            }

            // Subsequent punch: extend check-out (keep original status and check-in)
            if (existing.CheckInTime is null || time > existing.CheckInTime)
            {
                existing.Mark(existing.Status, existing.CheckInTime ?? time, time, existing.Notes);
                await _attendanceRepository.UpsertRangeAsync([existing], "device", cancellationToken);
            }

            return Ok(new { employee.EmployeeCode, employee.Name, action = "CHECK_OUT", status = existing.Status, time = time.ToString(@"hh\:mm") });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing attendance punch");
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
