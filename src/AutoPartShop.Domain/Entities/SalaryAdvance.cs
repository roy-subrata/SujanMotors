namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Cash advance given to an employee against future salary. Giving an advance posts a
/// SALARY_ADVANCE DailyExpense (cash out today); the outstanding balance is pulled into
/// the next payroll run's AdvanceDeduction and marked SETTLED when that run is paid.
/// </summary>
public class SalaryAdvance : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public DateTime AdvanceDate { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; } = "CASH";
    public string Notes { get; private set; } = string.Empty;
    public string Status { get; private set; } = "OUTSTANDING";  // OUTSTANDING, SETTLED
    public decimal RecoveredAmount { get; private set; } = 0;    // Recovered so far via payroll (installments)
    public decimal RemainingAmount => Amount - RecoveredAmount;   // Still owed by the employee
    public Guid? ExpenseId { get; private set; }             // DailyExpense posted when given
    public Guid? SettledPayrollRunId { get; private set; }   // Run whose payment fully settled this advance
    public DateTime? SettledAt { get; private set; }

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
            Status = "OUTSTANDING"
        };
    }

    public void LinkExpense(Guid expenseId) => ExpenseId = expenseId;

    public void Settle(Guid payrollRunId)
    {
        if (Status != "OUTSTANDING")
            throw new InvalidOperationException($"Cannot settle a {Status} advance");

        Status = "SETTLED";
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
        if (Status != "OUTSTANDING")
            throw new InvalidOperationException($"Cannot recover a {Status} advance");
        if (amount <= 0)
            throw new ArgumentException("Recovery amount must be greater than zero", nameof(amount));

        var take = Math.Min(amount, RemainingAmount);
        RecoveredAmount += take;

        if (RemainingAmount <= 0)
        {
            Status = "SETTLED";
            SettledPayrollRunId = payrollRunId;
            SettledAt = DateTime.UtcNow;
        }
    }
}
