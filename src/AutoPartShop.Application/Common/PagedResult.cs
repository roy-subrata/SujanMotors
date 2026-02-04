namespace AutoPartShop.Application.Common;

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
