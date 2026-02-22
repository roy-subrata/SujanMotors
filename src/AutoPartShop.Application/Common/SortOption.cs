namespace AutoPartShop.Application.Common;

/// <summary>
/// Sort option for dynamic ordering
/// </summary>
public class SortOption
{
    public string Field { get; set; } = "";
    public string Direction { get; set; } = "asc"; // asc | desc

    public bool IsAscending => Direction.Equals("asc", StringComparison.OrdinalIgnoreCase);
}
