namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A cashier's open/close session on a physical till, with cash-drawer reconciliation. This is the
/// gap CashBookController doesn't cover — that controller is a whole-business daily cash ledger,
/// not scoped to a single cashier's shift on a single terminal.
///
/// Deliberately does not link to CustomerPayment via a foreign key. Every CustomerPayment-creating
/// path already reliably stamps CreatedBy (the acting username) and PaymentDate, so "cash sales
/// during this session" is derived at close time as SUM(Amount) WHERE PaymentMethod='CASH' AND
/// CreatedBy=CashierUsername AND PaymentDate BETWEEN OpenedAt AND ClosedAt — zero schema changes to
/// the 6 existing payment-creation call sites. CashierUsername is snapshotted at open time so the
/// join stays correct even if the account's username changes later.
///
/// Reconciliation figures (CashSalesTotal, CashRefundsTotal, ExpectedAmount, OverShortAmount) are
/// computed once and frozen at Close — the entity owns the arithmetic, the caller supplies the
/// externally-sourced sales/refund totals it cannot reach on its own. Freezing means a historical
/// Shift Report never silently changes if payment data is edited afterward.
/// </summary>
public class TillSession : AuditableEntity
{
    public Guid CashierId { get; private set; }
    public string CashierUsername { get; private set; } = string.Empty;
    public string TerminalLabel { get; private set; } = string.Empty;  // e.g. "Till 01" — free text, no Terminal master entity
    public string? ShiftLabel { get; private set; }  // e.g. "Shift A" — free text, not FK'd to the HR Shift entity
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public decimal OpeningFloat { get; private set; }
    public decimal? ClosingCountedAmount { get; private set; }
    public string Status { get; private set; } = "OPEN";  // OPEN, CLOSED

    // Snapshotted at Close — see class remarks.
    public decimal CashSalesTotal { get; private set; }
    public decimal CashRefundsTotal { get; private set; }
    public decimal CashDropsTotal { get; private set; }
    public decimal ExpectedAmount { get; private set; }
    public decimal OverShortAmount { get; private set; }

    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public ApplicationUser? Cashier { get; set; }
    public ICollection<TillCashDrop> CashDrops { get; set; } = new List<TillCashDrop>();

    private TillSession() { }

    public static TillSession Create(
        Guid cashierId, string cashierUsername, string terminalLabel,
        decimal openingFloat, string? shiftLabel = null, string notes = "")
    {
        if (cashierId == Guid.Empty)
            throw new ArgumentException("CashierId cannot be empty", nameof(cashierId));

        if (string.IsNullOrWhiteSpace(cashierUsername))
            throw new ArgumentException("CashierUsername cannot be empty", nameof(cashierUsername));

        if (string.IsNullOrWhiteSpace(terminalLabel))
            throw new ArgumentException("TerminalLabel cannot be empty", nameof(terminalLabel));

        if (openingFloat < 0)
            throw new ArgumentException("Opening float cannot be negative", nameof(openingFloat));

        return new TillSession
        {
            CashierId = cashierId,
            CashierUsername = cashierUsername.Trim(),
            TerminalLabel = terminalLabel.Trim(),
            ShiftLabel = string.IsNullOrWhiteSpace(shiftLabel) ? null : shiftLabel.Trim(),
            OpeningFloat = openingFloat,
            OpenedAt = DateTime.UtcNow,
            Status = "OPEN",
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    public void RecordCashDrop(TillCashDrop drop)
    {
        if (Status != "OPEN")
            throw new InvalidOperationException($"Cannot record a cash drop on a {Status} session");

        CashDrops.Add(drop);
    }

    /// <summary>
    /// Closes the session and freezes the reconciliation. cashSalesTotal/cashRefundsTotal come from
    /// the caller (a CustomerPayment aggregate the entity has no access to); CashDropsTotal is
    /// summed from this session's own recorded drops.
    /// </summary>
    public void Close(decimal countedAmount, decimal cashSalesTotal, decimal cashRefundsTotal, string notes = "")
    {
        if (Status != "OPEN")
            throw new InvalidOperationException($"Only an OPEN session can be closed. Current: {Status}");

        if (countedAmount < 0)
            throw new ArgumentException("Counted amount cannot be negative", nameof(countedAmount));

        CashSalesTotal = cashSalesTotal;
        CashRefundsTotal = cashRefundsTotal;
        CashDropsTotal = CashDrops.Sum(d => d.Amount);
        ExpectedAmount = OpeningFloat + CashSalesTotal - CashRefundsTotal - CashDropsTotal;
        ClosingCountedAmount = countedAmount;
        OverShortAmount = countedAmount - ExpectedAmount;

        ClosedAt = DateTime.UtcNow;
        Status = "CLOSED";
        if (!string.IsNullOrWhiteSpace(notes))
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes}\n{notes.Trim()}";
    }
}
