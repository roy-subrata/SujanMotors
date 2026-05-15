namespace AutoPartShop.Application.Common;

/// <summary>
/// Base query class for paginated requests with search and sorting support
/// </summary>
public abstract class BaseQuery
{
    public string Search { get; set; } = "";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public IReadOnlyList<SortOption> Sorts { get; set; } = [];

    /// <summary>
    /// Calculate skip count for pagination
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;
}
