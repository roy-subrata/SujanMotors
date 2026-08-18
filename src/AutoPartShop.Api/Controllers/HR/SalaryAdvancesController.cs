using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.HR;
using AutoPartShop.Application.HR.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Entities.HR;
using AutoPartShop.Domain.Enums.HR;
using AutoPartShop.Domain.Repositories.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers.HR;

/// <summary>
/// Salary advances: giving one posts a SALARY_ADVANCE cash-book expense in the same
/// transaction; the outstanding balance is auto-deducted (and settled) by the next
/// payroll run.
/// </summary>
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class SalaryAdvancesController : ControllerBase
{
    private readonly ISalaryAdvanceRepository _advanceRepository;
    private readonly ISalaryAdvanceReadRepository _advanceReadRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SalaryAdvancesController> _logger;

    public SalaryAdvancesController(
        ISalaryAdvanceRepository advanceRepository,
        ISalaryAdvanceReadRepository advanceReadRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        ILogger<SalaryAdvancesController> logger)
    {
        _advanceRepository = advanceRepository;
        _advanceReadRepository = advanceReadRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList(SalaryAdvanceQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (query is null)
            {
                return BadRequest("Request can not be empty");
            }

            var (advances, totalCount) = await _advanceReadRepository.FindAllQuery(query, cancellationToken);

            return Ok(PagedResult<SalaryAdvanceResponse>.Create(advances, totalCount, query));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting salary advances list");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Raises an advance request. Nothing is paid out and no cash-book expense is posted until the
    /// request is approved — previously this endpoint handed over the money immediately, with no
    /// authorisation gate and no ceiling on the amount.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Request(GiveAdvanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
            if (employee is null)
                return BadRequest(new { message = "Employee not found" });

            if (employee.MonthlySalary <= 0)
                return BadRequest(new { message = "This employee has no monthly salary set, so an advance cannot be recovered from payroll." });

            // An advance is recovered out of future salary, so it cannot exceed what the employee
            // will earn — counting anything already advanced and not yet recovered. Without this,
            // 99999 was accepted against a monthly salary of 20000.
            var outstanding = (await _advanceRepository.GetOutstandingByEmployeeAsync(request.EmployeeId, cancellationToken))
                .Sum(a => a.RemainingAmount);
            var headroom = employee.MonthlySalary - outstanding;

            if (request.Amount > headroom)
                return BadRequest(new
                {
                    message = $"An advance of {request.Amount:N2} exceeds the remaining limit of {headroom:N2} " +
                              $"(monthly salary {employee.MonthlySalary:N2} less {outstanding:N2} already outstanding)."
                });

            var advance = SalaryAdvance.Create(
                request.EmployeeId,
                request.AdvanceDate,
                request.Amount,
                request.PaymentMethod,
                request.Notes);

            var currentUser = _currentUserService.GetCurrentUsername();
            advance.CreatedBy = currentUser;
            advance.ModifiedBy = currentUser;

            await _advanceRepository.RequestAsync(advance, cancellationToken);

            return Ok(new { advance.Id, advance.Status });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting salary advance");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Authorises the payout: posts the SALARY_ADVANCE cash-book expense and moves the advance to
    /// OUTSTANDING so payroll starts recovering it. This is where cash leaves the till.
    /// </summary>
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var advance = await _advanceRepository.GetByIdAsync(id, cancellationToken);
            if (advance is null) return NotFound();

            var employee = await _employeeRepository.GetByIdAsync(advance.EmployeeId, cancellationToken);
            if (employee is null)
                return BadRequest(new { message = "Employee not found" });

            var currentUser = _currentUserService.GetCurrentUsername();
            advance.Approve(currentUser);
            advance.ModifiedBy = currentUser;

            var expense = DailyExpense.Create(
                advance.AdvanceDate.Date,
                "SALARY_ADVANCE",
                advance.Amount,
                $"Salary advance to {employee.Name} ({employee.EmployeeCode})",
                advance.PaymentMethod);
            expense.CreatedBy = currentUser;
            expense.ModifiedBy = currentUser;

            await _advanceRepository.ApproveAsync(advance, expense, cancellationToken);

            return Ok(new { advance.Id, advance.Status });
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
            _logger.LogError(ex, "Error approving salary advance {AdvanceId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>Declines a request. No expense was posted, so there is nothing to reverse.</summary>
    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Reject(Guid id, RejectAdvanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var advance = await _advanceRepository.GetByIdAsync(id, cancellationToken);
            if (advance is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            advance.Reject(currentUser, request?.Reason ?? string.Empty);
            advance.ModifiedBy = currentUser;

            await _advanceRepository.RejectAsync(advance, cancellationToken);

            return Ok(new { advance.Id, advance.Status });
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
            _logger.LogError(ex, "Error rejecting salary advance {AdvanceId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>Cancels an OUTSTANDING advance and removes its cash-book expense.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var advance = await _advanceRepository.GetByIdAsync(id, cancellationToken);
            if (advance is null) return NotFound();

            if (advance.Status is not (SalaryAdvanceStatus.REQUESTED or SalaryAdvanceStatus.OUTSTANDING))
                return BadRequest(new { message = "Only requested or outstanding advances can be cancelled" });

            advance.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _advanceRepository.CancelAsync(advance, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling salary advance");
            return StatusCode(500, "An error occurred");
        }
    }
}
