using AutoPartShop.Domain.Enums.HR;

namespace AutoPartShop.Domain.Entities.HR;

/// <summary>
/// A monthly payroll cycle. Generated as a DRAFT from the attendance summary,
/// reviewed/adjusted, APPROVED (locked), then PAID — which posts the total into
/// DailyExpense (SALARIES) so the cash book and P&amp;L stay correct.
/// One run per (Year, Month).
/// </summary>
public class PayrollRun : AuditableEntity
{
    public string RunCode { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public PayrollRunStatus Status { get; private set; } = PayrollRunStatus.DRAFT;
    public string Currency { get; private set; } = "BDT";

    // Denormalized totals, refreshed whenever payslips change
    public decimal TotalGross { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal TotalNet { get; private set; }
    public int EmployeeCount { get; private set; }

    public string ApprovedBy { get; private set; } = string.Empty;
    public DateTime? ApprovedAt { get; private set; }

    public string PaidBy { get; private set; } = string.Empty;
    public DateTime? PaidAt { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;  // CASH, BANK_TRANSFER, CHECK
    public Guid? ExpenseId { get; private set; }  // DailyExpense created on payment

    public string Notes { get; private set; } = string.Empty;

    public ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();

    private PayrollRun() { }

    public static PayrollRun Create(string runCode, int year, int month, string notes = "")
    {
        if (string.IsNullOrWhiteSpace(runCode))
            throw new ArgumentException("RunCode cannot be empty", nameof(runCode));

        if (year < 2000 || year > 2100)
            throw new ArgumentException("Invalid year", nameof(year));

        if (month < 1 || month > 12)
            throw new ArgumentException("Invalid month", nameof(month));

        return new PayrollRun
        {
            RunCode = runCode.Trim().ToUpper(),
            Year = year,
            Month = month,
            Notes = notes?.Trim() ?? string.Empty,
            Status = PayrollRunStatus.DRAFT
        };
    }

    public void EnsureDraft()
    {
        if (Status != PayrollRunStatus.DRAFT)
            throw new InvalidOperationException($"Payroll run is {Status}; only DRAFT runs can be modified");
    }

    public void RecalculateTotals()
    {
        var active = Payslips.Where(p => !p.Isdeleted).ToList();
        TotalGross = active.Sum(p => p.GrossPay);
        TotalDeductions = active.Sum(p => p.TotalDeduction);
        TotalNet = active.Sum(p => p.NetPay);
        EmployeeCount = active.Count;
    }

    public void Approve(string approvedBy)
    {
        if (Status != PayrollRunStatus.DRAFT)
            throw new InvalidOperationException($"Cannot approve a {Status} payroll run");

        if (!Payslips.Any(p => !p.Isdeleted))
            throw new InvalidOperationException("Cannot approve a payroll run with no payslips");

        Status = PayrollRunStatus.APPROVED;
        ApprovedBy = approvedBy?.Trim() ?? string.Empty;
        ApprovedAt = DateTime.UtcNow;
    }

    public void MarkPaid(string paidBy, string paymentMethod, Guid expenseId)
    {
        if (Status != PayrollRunStatus.APPROVED)
            throw new InvalidOperationException($"Cannot pay a {Status} payroll run; approve it first");

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod is required", nameof(paymentMethod));

        Status = PayrollRunStatus.PAID;
        PaidBy = paidBy?.Trim() ?? string.Empty;
        PaidAt = DateTime.UtcNow;
        PaymentMethod = paymentMethod.Trim().ToUpper();
        ExpenseId = expenseId;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
