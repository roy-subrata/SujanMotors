using AutoPartShop.Api.Services;
using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Hr.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Monthly payroll runs: generate DRAFT from the attendance summary, adjust payslips,
/// approve, then pay — which posts a SALARIES DailyExpense in the same transaction.
/// </summary>
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class PayrollController : ControllerBase
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceReadRepository _attendanceReadRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        IPayrollRepository payrollRepository,
        IEmployeeRepository employeeRepository,
        IAttendanceReadRepository attendanceReadRepository,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ILogger<PayrollController> logger)
    {
        _payrollRepository = payrollRepository;
        _employeeRepository = employeeRepository;
        _attendanceReadRepository = attendanceReadRepository;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var runs = await _payrollRepository.GetAllAsync(cancellationToken);
            return Ok(runs.Select(r => MapRun(r, includePayslips: false)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll runs");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _payrollRepository.GetByIdAsync(id, includePayslips: true, cancellationToken);
            if (run is null) return NotFound();

            return Ok(MapRun(run, includePayslips: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll run");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Creates (or regenerates) the DRAFT run for a month from active employees and
    /// their attendance summary. Regeneration replaces payslips but keeps the run.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(GeneratePayrollRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Year < 2000 || request.Year > 2100 || request.Month < 1 || request.Month > 12)
                return BadRequest(new { message = "Invalid year or month" });

            var monthStart = new DateTime(request.Year, request.Month, 1);
            if (monthStart > DateTime.UtcNow.Date)
                return BadRequest(new { message = "Cannot generate payroll for a future month" });

            var currentUser = _currentUserService.GetCurrentUsername();
            var existing = await _payrollRepository.GetByYearMonthAsync(request.Year, request.Month, includePayslips: true, cancellationToken);

            PayrollRun run;
            if (existing is not null)
            {
                if (existing.Status != "DRAFT")
                    return BadRequest(new { message = $"Payroll for {request.Year}-{request.Month:D2} is already {existing.Status}" });

                // Regenerate: drop existing draft payslips
                foreach (var slip in existing.Payslips)
                    slip.Isdeleted = true;
                run = existing;
            }
            else
            {
                var runCode = await _codeGenerateService.GenerateAsync("PAY", cancellationToken);
                run = PayrollRun.Create(runCode, request.Year, request.Month, request.Notes);
                run.CreatedBy = currentUser;
                run.ModifiedBy = currentUser;
                await _payrollRepository.AddAsync(run, cancellationToken);
            }

            var employees = (await _employeeRepository.GetByStatusAsync("ACTIVE", cancellationToken)).ToList();
            if (employees.Count == 0)
                return BadRequest(new { message = "No active employees to run payroll for" });

            var summary = await _attendanceReadRepository.GetMonthlySummary(request.Year, request.Month, cancellationToken);
            var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

            foreach (var employee in employees)
            {
                var att = summary.FirstOrDefault(s => s.EmployeeId == employee.Id);
                var payslip = Payslip.Create(
                    run.Id, employee, daysInMonth,
                    att?.PresentDays ?? 0,
                    att?.LateDays ?? 0,
                    att?.HalfDays ?? 0,
                    att?.AbsentDays ?? 0,
                    att?.LeaveDays ?? 0,
                    att?.HolidayDays ?? 0);
                payslip.CreatedBy = currentUser;
                payslip.ModifiedBy = currentUser;
                run.Payslips.Add(payslip);
            }

            run.RecalculateTotals();
            run.ModifiedBy = currentUser;
            await _payrollRepository.UpdateAsync(run, cancellationToken);

            var fresh = await _payrollRepository.GetByIdAsync(run.Id, includePayslips: true, cancellationToken);
            return Ok(MapRun(fresh!, includePayslips: true));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payroll run");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>Update one payslip's manual adjustments (DRAFT runs only).</summary>
    [HttpPut("{id:guid}/payslips/{payslipId:guid}")]
    public async Task<IActionResult> UpdatePayslip(Guid id, Guid payslipId, UpdatePayslipRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _payrollRepository.GetByIdAsync(id, includePayslips: true, cancellationToken);
            if (run is null) return NotFound();

            run.EnsureDraft();

            var payslip = run.Payslips.FirstOrDefault(p => p.Id == payslipId && !p.Isdeleted);
            if (payslip is null) return NotFound(new { message = "Payslip not found in this run" });

            payslip.UpdateAdjustments(
                request.OvertimeAmount,
                request.BonusAmount,
                request.OtherAllowance,
                request.AdvanceDeduction,
                request.OtherDeduction,
                request.AdjustmentNotes);
            payslip.ModifiedBy = _currentUserService.GetCurrentUsername();

            run.RecalculateTotals();
            await _payrollRepository.UpdateAsync(run, cancellationToken);

            return Ok(MapRun(run, includePayslips: true));
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
            _logger.LogError(ex, "Error updating payslip");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _payrollRepository.GetByIdAsync(id, includePayslips: true, cancellationToken);
            if (run is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            run.Approve(currentUser);
            run.ModifiedBy = currentUser;

            await _payrollRepository.UpdateAsync(run, cancellationToken);

            return Ok(MapRun(run, includePayslips: true));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving payroll run");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>Marks the run PAID and posts a SALARIES DailyExpense atomically.</summary>
    [HttpPatch("{id:guid}/pay")]
    public async Task<IActionResult> Pay(Guid id, PayPayrollRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _payrollRepository.GetByIdAsync(id, includePayslips: true, cancellationToken);
            if (run is null) return NotFound();

            if (run.Status != "APPROVED")
                return BadRequest(new { message = $"Cannot pay a {run.Status} payroll run; approve it first" });

            if (run.TotalNet <= 0)
                return BadRequest(new { message = "Total net pay must be greater than zero" });

            var currentUser = _currentUserService.GetCurrentUsername();
            var monthName = new DateTime(run.Year, run.Month, 1).ToString("MMMM yyyy");

            var expense = DailyExpense.Create(
                DateTime.UtcNow.Date,
                "SALARIES",
                run.TotalNet,
                $"Staff salaries for {monthName} ({run.RunCode}, {run.EmployeeCount} employees)",
                request.PaymentMethod,
                currency: run.Currency);
            expense.SetReferenceNumber(run.RunCode);
            expense.CreatedBy = currentUser;
            expense.ModifiedBy = currentUser;

            run.ModifiedBy = currentUser;
            await _payrollRepository.PayAsync(run, expense, currentUser, request.PaymentMethod, cancellationToken);

            return Ok(MapRun(run, includePayslips: true));
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
            _logger.LogError(ex, "Error paying payroll run");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _payrollRepository.GetByIdAsync(id, includePayslips: false, cancellationToken);
            if (run is null) return NotFound();

            if (run.Status == "PAID")
                return BadRequest(new { message = "Cannot delete a PAID payroll run" });

            await _payrollRepository.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll run");
            return StatusCode(500, "An error occurred");
        }
    }

    private static PayrollRunResponse MapRun(PayrollRun r, bool includePayslips) => new()
    {
        Id = r.Id,
        RunCode = r.RunCode,
        Year = r.Year,
        Month = r.Month,
        Status = r.Status,
        Currency = r.Currency,
        TotalGross = r.TotalGross,
        TotalDeductions = r.TotalDeductions,
        TotalNet = r.TotalNet,
        EmployeeCount = r.EmployeeCount,
        ApprovedBy = r.ApprovedBy,
        ApprovedAt = r.ApprovedAt,
        PaidBy = r.PaidBy,
        PaidAt = r.PaidAt,
        PaymentMethod = r.PaymentMethod,
        ExpenseId = r.ExpenseId,
        Notes = r.Notes,
        CreatedAt = r.CreatedDate,
        Payslips = includePayslips
            ? r.Payslips.Where(p => !p.Isdeleted).OrderBy(p => p.EmployeeName).Select(MapPayslip).ToList()
            : []
    };

    private static PayslipResponse MapPayslip(Payslip p) => new()
    {
        Id = p.Id,
        EmployeeId = p.EmployeeId,
        EmployeeCode = p.EmployeeCode,
        EmployeeName = p.EmployeeName,
        Designation = p.Designation,
        Department = p.Department,
        MonthlySalary = p.MonthlySalary,
        DaysInMonth = p.DaysInMonth,
        PresentDays = p.PresentDays,
        LateDays = p.LateDays,
        HalfDays = p.HalfDays,
        AbsentDays = p.AbsentDays,
        LeaveDays = p.LeaveDays,
        HolidayDays = p.HolidayDays,
        OvertimeAmount = p.OvertimeAmount,
        BonusAmount = p.BonusAmount,
        OtherAllowance = p.OtherAllowance,
        AdvanceDeduction = p.AdvanceDeduction,
        OtherDeduction = p.OtherDeduction,
        AdjustmentNotes = p.AdjustmentNotes,
        AbsenceDeduction = p.AbsenceDeduction,
        GrossPay = p.GrossPay,
        TotalDeduction = p.TotalDeduction,
        NetPay = p.NetPay
    };
}
