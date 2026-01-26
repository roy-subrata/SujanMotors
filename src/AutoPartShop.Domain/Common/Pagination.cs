namespace AutoPartShop.Domain.Common;

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

/// <summary>
/// Sort option for dynamic ordering
/// </summary>
public class SortOption
{
    public string Field { get; set; } = "";
    public string Direction { get; set; } = "asc"; // asc | desc

    public bool IsAscending => Direction.Equals("asc", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Generic paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of data items</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Data { get; set; } = [];
    public PaginationMeta Pagination { get; set; } = new();

    public static PagedResult<T> Create(IEnumerable<T> data, int totalCount, int pageNumber, int pageSize)
    {
        return new PagedResult<T>
        {
            Data = data.ToList(),
            Pagination = new PaginationMeta
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        };
    }

    public static PagedResult<T> Create(IEnumerable<T> data, int totalCount, BaseQuery query)
    {
        return Create(data, totalCount, query.PageNumber, query.PageSize);
    }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMeta
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
