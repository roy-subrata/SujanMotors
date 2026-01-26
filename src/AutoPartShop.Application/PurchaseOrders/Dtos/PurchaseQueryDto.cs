

using AutoPartShop.Domain.Common;

public class PurcahseQueryDto : BaseQuery
{
    public string? SupplierId{get;set;}
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; } = string.Empty;
}