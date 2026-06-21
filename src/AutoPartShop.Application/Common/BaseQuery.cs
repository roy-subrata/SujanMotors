namespace AutoPartShop.Application.Common;

/// <summary>
/// Base query class for paginated requests with search and sorting support.
/// PageNumber and PageSize are clamped to safe bounds so a client can never request
/// an unbounded page (which would let a single call pull the whole table into memory).
/// </summary>
public abstract class BaseQuery
{
    /// <summary>Largest page a client may request. Protects the API from "give me everything" calls.</summary>
    public const int MaxPageSize = 200;

    private int _pageNumber = 1;
    private int _pageSize = 10;

    public string Search { get; set; } = "";

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : (value > MaxPageSize ? MaxPageSize : value);
    }

    public IReadOnlyList<SortOption> Sorts { get; set; } = [];

    /// <summary>
    /// Calculate skip count for pagination
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;
}
