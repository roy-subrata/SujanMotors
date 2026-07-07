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

    [HttpPost]
    public async Task<IActionResult> Give(GiveAdvanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
            if (employee is null)
                return BadRequest(new { message = "Employee not found" });

            var advance = SalaryAdvance.Create(
                request.EmployeeId,
                request.AdvanceDate,
                request.Amount,
                request.PaymentMethod,
                request.Notes);

            var currentUser = _currentUserService.GetCurrentUsername();
            advance.CreatedBy = currentUser;
            advance.ModifiedBy = currentUser;

            var expense = DailyExpense.Create(
                request.AdvanceDate.Date,
                "SALARY_ADVANCE",
                request.Amount,
                $"Salary advance to {employee.Name} ({employee.EmployeeCode})",
                request.PaymentMethod);
            expense.CreatedBy = currentUser;
            expense.ModifiedBy = currentUser;

            await _advanceRepository.GiveAsync(advance, expense, cancellationToken);

            return Ok(new { advance.Id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error giving salary advance");
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

            if (advance.Status != "OUTSTANDING")
                return BadRequest(new { message = "Only outstanding advances can be cancelled" });

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
