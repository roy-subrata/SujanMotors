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
    public Guid? ExpenseId { get; private set; }             // DailyExpense posted when given
    public Guid? SettledPayrollRunId { get; private set; }   // Run whose payment settled this advance
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
        SettledPayrollRunId = payrollRunId;
        SettledAt = DateTime.UtcNow;
    }
}
