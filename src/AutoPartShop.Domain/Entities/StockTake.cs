namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A physical inventory count (stock take / cycle count) for one warehouse.
/// Snapshots expected quantities at creation so counting happens against a stable baseline
/// while trading continues; on approval the per-line variances are applied as stock
/// adjustments in a single transaction referencing this document's number.
/// Lifecycle: COUNTING → REVIEW → COMPLETED, or CANCELLED (from COUNTING/REVIEW).
/// </summary>
public class StockTake : AuditableEntity
{
    /// <summary>Optimistic-concurrency token — blocks double-approval from two sessions.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public string StockTakeNumber { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public Guid? CategoryId { get; private set; }  // Optional cycle-count scope: only parts in this category
    public string Status { get; private set; } = "COUNTING";  // COUNTING, REVIEW, COMPLETED, CANCELLED
    public DateTime SnapshotDate { get; private set; }  // When expected quantities were captured
    public DateTime? SubmittedDate { get; private set; }   // COUNTING → REVIEW
    public DateTime? CompletedDate { get; private set; }   // REVIEW → COMPLETED (variances applied)
    public DateTime? CancelledDate { get; private set; }
    public string CompletedBy { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public Warehouse? Warehouse { get; set; }
    public Category? Category { get; set; }
    public ICollection<StockTakeLine> Lines { get; set; } = new List<StockTakeLine>();

    private StockTake() { }

    public static StockTake Create(string stockTakeNumber, Guid warehouseId, Guid? categoryId = null, string notes = "")
    {
        if (string.IsNullOrWhiteSpace(stockTakeNumber))
            throw new ArgumentException("StockTakeNumber cannot be empty", nameof(stockTakeNumber));

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty", nameof(warehouseId));

        return new StockTake
        {
            StockTakeNumber = stockTakeNumber.Trim().ToUpper(),
            WarehouseId = warehouseId,
            CategoryId = categoryId,
            Status = "COUNTING",
            SnapshotDate = DateTime.UtcNow,
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    /// <summary>Counting finished — lock counts and move to variance review.</summary>
    public void SubmitForReview()
    {
        if (Status != "COUNTING")
            throw new InvalidOperationException($"Only a COUNTING stock take can be submitted for review. Current: {Status}");

        if (!Lines.Any(l => l.CountedQuantity.HasValue))
            throw new InvalidOperationException("At least one line must be counted before submitting for review");

        Status = "REVIEW";
        SubmittedDate = DateTime.UtcNow;
    }

    /// <summary>Reopen counting from review (e.g. a variance needs a recount).</summary>
    public void ReopenCounting()
    {
        if (Status != "REVIEW")
            throw new InvalidOperationException($"Only a stock take in REVIEW can be reopened for counting. Current: {Status}");

        Status = "COUNTING";
        SubmittedDate = null;
    }

    /// <summary>Variances applied as adjustments — terminal state.</summary>
    public void Complete(string completedBy)
    {
        if (Status != "REVIEW")
            throw new InvalidOperationException($"Only a stock take in REVIEW can be completed. Current: {Status}");

        if (string.IsNullOrWhiteSpace(completedBy))
            throw new ArgumentException("CompletedBy cannot be empty", nameof(completedBy));

        Status = "COMPLETED";
        CompletedDate = DateTime.UtcNow;
        CompletedBy = completedBy.Trim();
    }

    public void Cancel()
    {
        if (Status is "COMPLETED" or "CANCELLED")
            throw new InvalidOperationException($"Cannot cancel a {Status} stock take");

        Status = "CANCELLED";
        CancelledDate = DateTime.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
