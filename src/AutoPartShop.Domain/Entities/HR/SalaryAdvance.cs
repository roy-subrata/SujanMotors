using AutoPartShop.Domain.Enums.HR;

namespace AutoPartShop.Domain.Entities.HR;

/// <summary>
/// Cash advance against an employee's future salary. An advance is REQUESTED first and pays out
/// nothing; approving it posts the SALARY_ADVANCE DailyExpense (cash out today) and moves it to
/// OUTSTANDING, from where the balance is pulled into the next payroll run's AdvanceDeduction and
/// marked SETTLED once fully recovered. The approval step exists so cash cannot leave the till on
/// an unreviewed request.
/// </summary>
public class SalaryAdvance : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public DateTime AdvanceDate { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; } = "CASH";
    public string Notes { get; private set; } = string.Empty;
    public SalaryAdvanceStatus Status { get; private set; } = SalaryAdvanceStatus.OUTSTANDING;
    public decimal RecoveredAmount { get; private set; } = 0;    // Recovered so far via payroll (installments)
    public decimal RemainingAmount => Amount - RecoveredAmount;   // Still owed by the employee
    public Guid? ExpenseId { get; private set; }             // DailyExpense posted on approval
    public Guid? SettledPayrollRunId { get; private set; }   // Run whose payment fully settled this advance
    public DateTime? SettledAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private SalaryAdvance() { }

    public static SalaryAdvance Create(Guid employeeId, DateTime advanceDate, decimal amount,
        string paymentMethod = "CASH", string notes = "")
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId cannot be empty", nameof(employeeId));

        if (advanceDate == default)
            throw new ArgumentException("AdvanceDate is required", nameof(advanceDate));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod is required", nameof(paymentMethod));

        return new SalaryAdvance
        {
            EmployeeId = employeeId,
            AdvanceDate = advanceDate.Date,
            Amount = amount,
            PaymentMethod = paymentMethod.Trim().ToUpper(),
            Notes = notes?.Trim() ?? string.Empty,
            Status = SalaryAdvanceStatus.REQUESTED
        };
    }

    public void LinkExpense(Guid expenseId) => ExpenseId = expenseId;

    /// <summary>
    /// Authorises the payout. Only after this does the caller post the cash-book expense and does
    /// payroll start recovering the balance.
    /// </summary>
    public void Approve(string approvedBy)
    {
        if (Status != SalaryAdvanceStatus.REQUESTED)
            throw new InvalidOperationException($"Only a REQUESTED advance can be approved. Current: {Status}");

        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("ApprovedBy is required", nameof(approvedBy));

        Status = SalaryAdvanceStatus.OUTSTANDING;
        ApprovedBy = approvedBy.Trim();
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (Status != SalaryAdvanceStatus.REQUESTED)
            throw new InvalidOperationException($"Only a REQUESTED advance can be rejected. Current: {Status}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection reason is required", nameof(reason));

        Status = SalaryAdvanceStatus.REJECTED;
        ApprovedBy = rejectedBy?.Trim();
        ApprovedAt = DateTime.UtcNow;
        RejectionReason = reason.Trim();
    }

    public void Settle(Guid payrollRunId)
    {
        if (Status != SalaryAdvanceStatus.OUTSTANDING)
            throw new InvalidOperationException($"Cannot settle a {Status} advance");

        Status = SalaryAdvanceStatus.SETTLED;
        RecoveredAmount = Amount;
        SettledPayrollRunId = payrollRunId;
        SettledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Recovers part (or all) of the outstanding balance from a payroll run. The advance is only
    /// marked SETTLED once fully recovered; otherwise it stays OUTSTANDING for the next run
    /// (installment recovery). The amount is capped at what is still owed.
    /// </summary>
    public void Recover(decimal amount, Guid payrollRunId)
    {
        if (Status != SalaryAdvanceStatus.OUTSTANDING)
            throw new InvalidOperationException($"Cannot recover a {Status} advance");
        if (amount <= 0)
            throw new ArgumentException("Recovery amount must be greater than zero", nameof(amount));

        var take = Math.Min(amount, RemainingAmount);
        RecoveredAmount += take;

        if (RemainingAmount <= 0)
        {
            Status = SalaryAdvanceStatus.SETTLED;
            SettledPayrollRunId = payrollRunId;
            SettledAt = DateTime.UtcNow;
        }
    }
}
