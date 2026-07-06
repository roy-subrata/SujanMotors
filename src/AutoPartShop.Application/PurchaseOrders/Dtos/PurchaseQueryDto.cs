

using AutoPartShop.Application.Common;

public class PurcahseQueryDto : BaseQuery
{
    public string? SupplierId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; } = string.Empty;

    /// <summary>
    /// When true, only returns POs that still have at least one line with outstanding
    /// quantity to receive (ordered - received - in-flight PENDING/VERIFIED GRNs > 0).
    /// Used by the Goods Receipt picker so fully-reserved POs aren't offered.
    /// </summary>
    public bool? HasReceivableQuantity { get; set; }
}