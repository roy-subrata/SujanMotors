namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a product category in the auto parts shop
/// </summary>
public class Category : AuditableEntity
{
    /// <summary>
    /// Category name (e.g., Engine Parts, Electrical, Brake System)
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Detailed description of the category
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Category code for reference (e.g., ENG-001, ELE-001)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Parent category ID for nested categories (nullable for top-level categories)
    /// </summary>
    public Guid? ParentCategoryId { get; private set; }

    /// <summary>
    /// Reference to parent category (if applicable)
    /// </summary>
    public Category? ParentCategory { get; private set; }

    /// <summary>
    /// Collection of sub-categories
    /// </summary>
    public ICollection<Category> SubCategories { get; private set; } = new List<Category>();

    /// <summary>
    /// Is this category active for use
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Display order in lists
    /// </summary>
    public int DisplayOrder { get; private set; } = 0;

    // Private constructor for EF Core
    private Category() { }

    /// <summary>
    /// Factory method to create a new Category
    /// </summary>
    public static Category Create(string name, string description, string code, int displayOrder = 0, Guid? parentCategoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Category code cannot be empty", nameof(code));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(name));
        }

        if (code.Length > 20)
        {
            throw new ArgumentException("Category code cannot exceed 20 characters", nameof(code));
        }

        return new()
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Code = code.Trim().ToUpperInvariant(),
            DisplayOrder = displayOrder >= 0 ? displayOrder : 0,
            ParentCategoryId = parentCategoryId,
            IsActive = true
        };
    }

    /// <summary>
    /// Update category details
    /// </summary>
    public void Update(string name, string description, int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        DisplayOrder = displayOrder >= 0 ? displayOrder : 0;
        IsActive = isActive;
    }

    /// <summary>
    /// Deactivate the category
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activate the category
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
