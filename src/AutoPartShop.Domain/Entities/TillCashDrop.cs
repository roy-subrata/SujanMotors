namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A cash removal from the drawer to the safe during an open TillSession — reduces what should be
/// counted in the drawer at close without being a sale or a refund.
/// </summary>
public class TillCashDrop : AuditableEntity
{
    public Guid TillSessionId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime DroppedAt { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    public TillSession? TillSession { get; set; }

    private TillCashDrop() { }

    public static TillCashDrop Create(Guid tillSessionId, decimal amount, string notes = "")
    {
        if (tillSessionId == Guid.Empty)
            throw new ArgumentException("TillSessionId cannot be empty", nameof(tillSessionId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        return new TillCashDrop
        {
            TillSessionId = tillSessionId,
            Amount = amount,
            DroppedAt = DateTime.UtcNow,
            Notes = notes?.Trim() ?? string.Empty
        };
    }
}
