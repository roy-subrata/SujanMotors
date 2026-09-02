using AutoPartShop.Api.Services;
using AutoPartShop.Api.Services.HR;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.HR;
using AutoPartShop.Application.HR.Dtos;
using AutoPartShop.Domain.Entities.HR;
using AutoPartShop.Domain.Enums.HR;
using AutoPartShop.Domain.Repositories.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers.HR;

/// <summary>
/// Leave applications with an approve/reject flow. Approval writes LEAVE marks
/// into the attendance records for the requested date range.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveRequestReadRepository _leaveRequestReadRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveRequestApprovalService _leaveRequestApprovalService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LeaveRequestsController> _logger;

    public LeaveRequestsController(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveRequestReadRepository leaveRequestReadRepository,
        IEmployeeRepository employeeRepository,
        ILeaveRequestApprovalService leaveRequestApprovalService,
        ICurrentUserService currentUserService,
        ILogger<LeaveRequestsController> logger)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveRequestReadRepository = leaveRequestReadRepository;
        _employeeRepository = employeeRepository;
        _leaveRequestApprovalService = leaveRequestApprovalService;
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
                return BadRequest(new { message = "Request can not be empty" });
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

    /// <summary>
    /// Leave balance per type for an employee. Used = approved days; pending days are shown
    /// separately because they only consume entitlement once approved.
    /// </summary>
    [HttpGet("balance/{employeeId:guid}")]
    public async Task<IActionResult> GetBalance(Guid employeeId, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null) return NotFound(new { message = "Employee not found" });

            var requests = await _leaveRequestRepository.GetByEmployeeAsync(employeeId, cancellationToken);

            var approvedDays = requests
                .Where(r => r.Status == LeaveRequestStatus.APPROVED)
                .GroupBy(r => r.LeaveType)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalDays));
            var pendingDays = requests
                .Where(r => r.Status == LeaveRequestStatus.PENDING)
                .GroupBy(r => r.LeaveType)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalDays));

            var balance = new List<LeaveBalanceItem>();
            foreach (var type in new[] { "ANNUAL", "CASUAL", "SICK", "UNPAID" })
            {
                var entitlement = employee.GetLeaveEntitlement(type);
                var used = approvedDays.GetValueOrDefault(type);
                balance.Add(new LeaveBalanceItem
                {
                    LeaveType = type,
                    Entitlement = entitlement,
                    UsedDays = used,
                    PendingDays = pendingDays.GetValueOrDefault(type),
                    RemainingDays = entitlement.HasValue ? entitlement.Value - used : null
                });
            }

            return Ok(new
            {
                EmployeeId = employee.Id,
                employee.EmployeeCode,
                employee.Name,
                Balance = balance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave balance for employee {EmployeeId}", employeeId);
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
            // Fast-path existence check for a friendly 404; the authoritative entitlement check and
            // the atomic approve+attendance-marks transaction live in the approval service.
            if (await _leaveRequestRepository.GetByIdAsync(id, cancellationToken) is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            var result = await _leaveRequestApprovalService.ApproveAsync(id, request?.Notes ?? string.Empty, currentUser, cancellationToken);

            return Ok(new { result.Id, result.Status });
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
            // Fast-path existence check for a friendly 404; the decision transaction lives in the
            // approval service.
            if (await _leaveRequestRepository.GetByIdAsync(id, cancellationToken) is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            var result = await _leaveRequestApprovalService.RejectAsync(id, request?.Notes ?? string.Empty, currentUser, cancellationToken);

            return Ok(new { result.Id, result.Status });
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
