namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A logistics/handover event on a warranty claim for an item that is NOT part of our own stock —
/// typically the customer's unit sent out to a manufacturer/supplier for repair and received back.
/// (Stock-backed replacement movements are tracked separately via <see cref="StockMovement"/>.)
/// </summary>
public class WarrantyClaimEvent : AuditableEntity
{
    public Guid WarrantyClaimId { get; private set; }
    public string EventType { get; private set; } = string.Empty;    // SENT_FOR_REPAIR, RECEIVED_FROM_REPAIR
    public string PartnerType { get; private set; } = string.Empty;  // MANUFACTURER, SUPPLIER
    public string PartnerName { get; private set; } = string.Empty;
    public string ResponsibleBy { get; private set; } = string.Empty;
    public string? ReferenceNumber { get; private set; }
    public DateTime? ExpectedReturnDate { get; private set; }
    public DateTime EventDate { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public WarrantyClaim? WarrantyClaim { get; set; }

    private WarrantyClaimEvent() { }

    public static WarrantyClaimEvent Create(
        Guid warrantyClaimId,
        string eventType,
        string partnerType,
        string partnerName,
        string responsibleBy,
        string? referenceNumber,
        DateTime? expectedReturnDate,
        string? notes,
        string actor)
    {
        if (warrantyClaimId == Guid.Empty)
            throw new ArgumentException("Warranty claim ID cannot be empty", nameof(warrantyClaimId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required", nameof(eventType));
        if (string.IsNullOrWhiteSpace(partnerName))
            throw new ArgumentException("Partner name is required", nameof(partnerName));
        if (string.IsNullOrWhiteSpace(responsibleBy))
            throw new ArgumentException("Responsible person is required", nameof(responsibleBy));

        return new WarrantyClaimEvent
        {
            WarrantyClaimId = warrantyClaimId,
            EventType = eventType.Trim().ToUpperInvariant(),
            PartnerType = string.IsNullOrWhiteSpace(partnerType) ? "MANUFACTURER" : partnerType.Trim().ToUpperInvariant(),
            PartnerName = partnerName.Trim(),
            ResponsibleBy = responsibleBy.Trim(),
            ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber.Trim(),
            ExpectedReturnDate = expectedReturnDate,
            EventDate = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedBy = actor ?? "System",
            ModifiedBy = actor ?? "System"
        };
    }
}
