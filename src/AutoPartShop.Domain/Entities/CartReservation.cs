namespace AutoPartShop.Domain.Entities;

public class CartReservation : BaseEntity
{
    public string SessionId { get; private set; } = string.Empty;
    public Guid PartId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsReleased { get; private set; }

    // Navigation
    public Product? Part { get; set; }

    private CartReservation() { }

    public static CartReservation Create(
        string sessionId,
        Guid partId,
        int quantity,
        int ttlMinutes = 30,
        Guid? productVariantId = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be empty", nameof(sessionId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        var now = DateTime.UtcNow;
        return new CartReservation
        {
            SessionId = sessionId.Trim(),
            PartId = partId,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(ttlMinutes),
            IsReleased = false
        };
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public void Release() => IsReleased = true;

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(newQuantity));
        Quantity = newQuantity;
    }

    public void ExtendTtl(int minutes = 30) => ExpiresAt = DateTime.UtcNow.AddMinutes(minutes);
}
