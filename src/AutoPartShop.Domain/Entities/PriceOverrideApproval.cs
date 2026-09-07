namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A short-lived, single-use token proving a manager approved one specific price-override request
/// (a line below the cost floor, or above the MRP ceiling) via a live credential check at checkout
/// — see PricingController's request-override endpoint. Id IS the token handed back to the client.
/// Deliberately small: it's both the enforcement mechanism (expiring, single-use) and, alongside
/// the AuditLog rows written when it's issued/consumed, the durable record of who approved what.
/// </summary>
public class PriceOverrideApproval : BaseEntity
{
    public Guid ApprovedByUserId { get; private set; }
    public string ApprovedByUsername { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Null until the token is actually applied to a sale.</summary>
    public DateTime? ConsumedAt { get; private set; }

    /// <summary>The sale the approval was applied to, once consumed.</summary>
    public string? ConsumedBySalesOrderNumber { get; private set; }

    public bool IsConsumed => ConsumedAt.HasValue;

    private PriceOverrideApproval() { }

    public static PriceOverrideApproval Create(Guid approvedByUserId, string approvedByUsername, TimeSpan validFor)
    {
        if (approvedByUserId == Guid.Empty)
            throw new ArgumentException("ApprovedByUserId cannot be empty", nameof(approvedByUserId));
        if (string.IsNullOrWhiteSpace(approvedByUsername))
            throw new ArgumentException("ApprovedByUsername cannot be empty", nameof(approvedByUsername));

        var now = DateTime.UtcNow;
        return new PriceOverrideApproval
        {
            ApprovedByUserId = approvedByUserId,
            ApprovedByUsername = approvedByUsername.Trim(),
            IssuedAt = now,
            ExpiresAt = now.Add(validFor)
        };
    }

    /// <summary>True when the token can still be applied to a sale right now.</summary>
    public bool IsUsable(DateTime asOf) => !IsConsumed && asOf <= ExpiresAt;

    public void Consume(string salesOrderNumber)
    {
        if (IsConsumed)
            throw new InvalidOperationException("This price-override approval has already been used.");
        if (string.IsNullOrWhiteSpace(salesOrderNumber))
            throw new ArgumentException("SalesOrderNumber cannot be empty", nameof(salesOrderNumber));

        ConsumedAt = DateTime.UtcNow;
        ConsumedBySalesOrderNumber = salesOrderNumber.Trim();
    }
}
