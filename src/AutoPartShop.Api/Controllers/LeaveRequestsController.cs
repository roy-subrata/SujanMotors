using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Hr.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Leave applications with an approve/reject flow. Approval writes LEAVE marks
/// into the attendance records for the requested date range.
/// </summary>
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveRequestReadRepository _leaveRequestReadRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LeaveRequestsController> _logger;

    public LeaveRequestsController(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveRequestReadRepository leaveRequestReadRepository,
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        ICurrentUserService currentUserService,
        ILogger<LeaveRequestsController> logger)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveRequestReadRepository = leaveRequestReadRepository;
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList(LeaveRequestQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (query is null)
            {
                return BadRequest("Request can not be empty");
            }

            var (requests, totalCount) = await _leaveRequestReadRepository.FindAllQuery(query, cancellationToken);

            return Ok(PagedResult<LeaveRequestResponse>.Create(requests, totalCount, query));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave requests list");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLeaveRequestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
            if (employee is null)
                return BadRequest(new { message = "Employee not found" });

            if (await _leaveRequestRepository.HasOverlapAsync(request.EmployeeId, request.FromDate, request.ToDate, null, cancellationToken))
                return BadRequest(new { message = "The employee already has a pending or approved leave overlapping this range" });

            var leaveRequest = LeaveRequest.Create(
                request.EmployeeId,
                request.LeaveType,
                request.FromDate,
                request.ToDate,
                request.Reason
            );

            var currentUser = _currentUserService.GetCurrentUsername();
            leaveRequest.CreatedBy = currentUser;
            leaveRequest.ModifiedBy = currentUser;

            await _leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);

            return CreatedAtAction(nameof(GetList), new { id = leaveRequest.Id }, new { leaveRequest.Id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave request");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateLeaveRequestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken);
            if (leaveRequest is null) return NotFound();

            if (await _leaveRequestRepository.HasOverlapAsync(leaveRequest.EmployeeId, request.FromDate, request.ToDate, id, cancellationToken))
                return BadRequest(new { message = "The employee already has a pending or approved leave overlapping this range" });

            leaveRequest.Update(request.LeaveType, request.FromDate, request.ToDate, request.Reason);
            leaveRequest.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);

            return Ok(new { leaveRequest.Id });
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
            _logger.LogError(ex, "Error updating leave request");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken);
            if (leaveRequest is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            leaveRequest.Approve(currentUser, request?.Notes ?? string.Empty);
            leaveRequest.ModifiedBy = currentUser;

            await _leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);

            // Reflect the approved leave in the attendance sheet
            var leaveMarks = new List<AttendanceRecord>();
            for (var day = leaveRequest.FromDate; day <= leaveRequest.ToDate; day = day.AddDays(1))
            {
                leaveMarks.Add(AttendanceRecord.Create(
                    leaveRequest.EmployeeId, day, "LEAVE",
                    notes: $"{leaveRequest.LeaveType} leave"));
            }
            await _attendanceRepository.UpsertRangeAsync(leaveMarks, currentUser, cancellationToken);

            return Ok(new { leaveRequest.Id, leaveRequest.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving leave request");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken);
            if (leaveRequest is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            leaveRequest.Reject(currentUser, request?.Notes ?? string.Empty);
            leaveRequest.ModifiedBy = currentUser;

            await _leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);

            return Ok(new { leaveRequest.Id, leaveRequest.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting leave request");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken);
            if (leaveRequest is null) return NotFound();

            await _leaveRequestRepository.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave request");
            return StatusCode(500, "An error occurred");
        }
    }
}
