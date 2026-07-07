using System.Text;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Interfaces;
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
    private readonly ISalaryAdvanceRepository _salaryAdvanceRepository;
    private readonly IHrSalesReadRepository _hrSalesReadRepository;
    private readonly INotificationService _notificationService;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        IPayrollRepository payrollRepository,
        IEmployeeRepository employeeRepository,
        IAttendanceReadRepository attendanceReadRepository,
        ISalaryAdvanceRepository salaryAdvanceRepository,
        IHrSalesReadRepository hrSalesReadRepository,
        INotificationService notificationService,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ILogger<PayrollController> logger)
    {
        _payrollRepository = payrollRepository;
        _employeeRepository = employeeRepository;
        _attendanceReadRepository = attendanceReadRepository;
        _salaryAdvanceRepository = salaryAdvanceRepository;
        _hrSalesReadRepository = hrSalesReadRepository;
        _notificationService = notificationService;
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
            var outstandingAdvances = await _salaryAdvanceRepository.GetOutstandingTotalsAsync(cancellationToken);
            var salesTotals = await _hrSalesReadRepository.GetMonthlySalesTotalsByEmployee(request.Year, request.Month, cancellationToken);
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

                payslip.ApplyGeneratedFigures(
                    advanceDeduction: outstandingAdvances.GetValueOrDefault(employee.Id),
                    taxDeduction: employee.MonthlyTaxDeduction,
                    monthlySalesTotal: salesTotals.GetValueOrDefault(employee.Id),
                    commissionRate: employee.CommissionRate);

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
                request.CommissionAmount,
                request.AdvanceDeduction,
                request.TaxDeduction,
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

            // Advances baked into this run's deductions are settled by this payment
            var employeesWithAdvance = run.Payslips
                .Where(p => !p.Isdeleted && p.AdvanceDeduction > 0)
                .Select(p => p.EmployeeId);

            run.ModifiedBy = currentUser;
            await _payrollRepository.PayAsync(run, expense, currentUser, request.PaymentMethod, employeesWithAdvance, cancellationToken);

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

    /// <summary>
    /// Emails an HTML payslip and/or texts a short summary to every employee in the run
    /// who has contact details on file. Only for APPROVED or PAID runs.
    /// </summary>
    [HttpPost("{id:guid}/send-payslips")]
    public async Task<IActionResult> SendPayslips(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _payrollRepository.GetByIdAsync(id, includePayslips: true, cancellationToken);
            if (run is null) return NotFound();

            if (run.Status != "APPROVED" && run.Status != "PAID")
                return BadRequest(new { message = "Payslips can only be sent for approved or paid runs" });

            var employees = (await _employeeRepository.GetAllAsync(cancellationToken)).ToDictionary(e => e.Id);
            var monthName = new DateTime(run.Year, run.Month, 1).ToString("MMMM yyyy");
            var result = new SendPayslipsResponse();

            foreach (var payslip in run.Payslips.Where(p => !p.Isdeleted))
            {
                employees.TryGetValue(payslip.EmployeeId, out var employee);
                var email = employee?.Email;
                var phone = employee?.Phone;
                var sent = false;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        await _notificationService.SendEmailAsync(email,
                            $"Payslip — {monthName} ({payslip.EmployeeCode})",
                            BuildPayslipHtml(payslip, run, monthName), cancellationToken);
                        result.EmailsSent++;
                        sent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to email payslip to {Email}", email);
                    }
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    try
                    {
                        await _notificationService.SendSmsAsync(phone,
                            $"{monthName} salary: net {payslip.NetPay:N2} {run.Currency} " +
                            $"(gross {payslip.GrossPay:N2}, deductions {payslip.TotalDeduction:N2}). Ref {run.RunCode}.",
                            cancellationToken);
                        result.SmsSent++;
                        sent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to SMS payslip to {Phone}", phone);
                    }
                }

                if (!sent) result.Skipped++;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payslips");
            return StatusCode(500, "An error occurred");
        }
    }

    private static string BuildPayslipHtml(Payslip p, PayrollRun run, string monthName)
    {
        static string Row(string label, decimal value, string currency) =>
            $"<tr><td style='padding:4px 12px 4px 0;color:#555'>{label}</td><td style='padding:4px 0;text-align:right'>{value:N2} {currency}</td></tr>";

        var sb = new StringBuilder();
        sb.Append($"<div style='font-family:Arial,sans-serif;max-width:480px'>");
        sb.Append($"<h2 style='margin-bottom:0'>Payslip — {monthName}</h2>");
        sb.Append($"<p style='color:#777;margin-top:4px'>{p.EmployeeName} ({p.EmployeeCode}) · {p.Designation} · Ref {run.RunCode}</p>");
        sb.Append("<table style='width:100%;border-collapse:collapse;font-size:14px'>");
        sb.Append(Row("Basic salary", p.MonthlySalary, run.Currency));
        if (p.OvertimeAmount > 0) sb.Append(Row("Overtime", p.OvertimeAmount, run.Currency));
        if (p.BonusAmount > 0) sb.Append(Row("Bonus", p.BonusAmount, run.Currency));
        if (p.OtherAllowance > 0) sb.Append(Row("Allowance", p.OtherAllowance, run.Currency));
        if (p.CommissionAmount > 0) sb.Append(Row("Sales commission", p.CommissionAmount, run.Currency));
        sb.Append(Row("Gross pay", p.GrossPay, run.Currency));
        if (p.AbsenceDeduction > 0) sb.Append(Row($"Absence deduction ({p.AbsentDays} absent, {p.HalfDays} half-days)", -p.AbsenceDeduction, run.Currency));
        if (p.AdvanceDeduction > 0) sb.Append(Row("Salary advance", -p.AdvanceDeduction, run.Currency));
        if (p.TaxDeduction > 0) sb.Append(Row("Tax", -p.TaxDeduction, run.Currency));
        if (p.OtherDeduction > 0) sb.Append(Row("Other deduction", -p.OtherDeduction, run.Currency));
        sb.Append($"<tr><td style='padding:8px 12px 4px 0;font-weight:bold;border-top:1px solid #ccc'>Net pay</td>" +
                  $"<td style='padding:8px 0 4px;text-align:right;font-weight:bold;border-top:1px solid #ccc'>{p.NetPay:N2} {run.Currency}</td></tr>");
        sb.Append("</table>");
        sb.Append($"<p style='color:#999;font-size:12px'>Attendance: {p.PresentDays} present, {p.LateDays} late, {p.HalfDays} half, {p.AbsentDays} absent, {p.LeaveDays} leave.</p>");
        sb.Append("</div>");
        return sb.ToString();
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
        CommissionAmount = p.CommissionAmount,
        MonthlySalesTotal = p.MonthlySalesTotal,
        AdvanceDeduction = p.AdvanceDeduction,
        TaxDeduction = p.TaxDeduction,
        OtherDeduction = p.OtherDeduction,
        AdjustmentNotes = p.AdjustmentNotes,
        AbsenceDeduction = p.AbsenceDeduction,
        GrossPay = p.GrossPay,
        TotalDeduction = p.TotalDeduction,
        NetPay = p.NetPay
    };
}
