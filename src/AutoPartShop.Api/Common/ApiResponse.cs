namespace AutoPartShop.Api.Common;

public sealed class ApiResponse<T>
{
    public T Data { get; init; }

    private ApiResponse(T data) => Data = data;

    public static ApiResponse<T> Ok(T data) => new(data);
}

public sealed class PagedApiResponse<T>
{
    public IEnumerable<T> Data { get; init; } = [];
    public PaginationMeta Pagination { get; init; } = new();

    public static PagedApiResponse<T> Create(IEnumerable<T> data, int totalCount, int page, int pageSize) => new()
    {
        Data = data,
        Pagination = new PaginationMeta
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            HasNextPage = page * pageSize < totalCount,
            HasPreviousPage = page > 1
        }
    };
}

public sealed class PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}
