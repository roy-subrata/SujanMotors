namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a product category in the auto parts shop with support for n-level hierarchy
/// </summary>
public class Category : AuditableEntity
{
    /// <summary>
    /// Maximum depth allowed in the category hierarchy (root = level 0)
    /// </summary>
    public const int MaxCategoryDepth = 7;

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

    /// <summary>
    /// Breadcrumb path for navigation (e.g., "Engines > Diesel > Small Diesel")
    /// </summary>
    public string BreadcrumbPath { get; private set; } = string.Empty;

    /// <summary>
    /// Depth level in the hierarchy (0 = root, 1 = first level under root, etc.)
    /// </summary>
    public int DepthLevel { get; private set; } = 0;

    /// <summary>
    /// Count of direct child categories (cached for performance)
    /// </summary>
    public int ChildCount { get; private set; } = 0;

    // Private constructor for EF Core
    private Category() { }

    /// <summary>
    /// Factory method to create a new Category
    /// </summary>
    /// <param name="name">Category name</param>
    /// <param name="description">Category description</param>
    /// <param name="code">Category code (unique identifier)</param>
    /// <param name="displayOrder">Display order in lists</param>
    /// <param name="parentCategoryId">Parent category ID (for subcategories)</param>
    /// <param name="breadcrumbPath">Breadcrumb path (auto-populated if parent is provided)</param>
    /// <param name="depthLevel">Depth level (auto-calculated if parent is provided)</param>
    public static Category Create(
        string name,
        string description,
        string code,
        int displayOrder = 0,
        Guid? parentCategoryId = null,
        string? breadcrumbPath = null,
        int depthLevel = 0)
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

        if (displayOrder < 0)
        {
            throw new ArgumentException("Display order cannot be negative", nameof(displayOrder));
        }

        if (depthLevel < 0 || depthLevel > MaxCategoryDepth)
        {
            throw new ArgumentException($"Category depth cannot exceed {MaxCategoryDepth} levels", nameof(depthLevel));
        }

        return new()
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Code = code.Trim().ToUpperInvariant(),
            DisplayOrder = displayOrder,
            ParentCategoryId = parentCategoryId,
            BreadcrumbPath = breadcrumbPath?.Trim() ?? name.Trim(),
            DepthLevel = depthLevel,
            ChildCount = 0,
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

    /// <summary>
    /// Update the breadcrumb path for the category
    /// </summary>
    public void UpdateBreadcrumbPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Breadcrumb path cannot be empty", nameof(path));
        }

        BreadcrumbPath = path.Trim();
    }

    /// <summary>
    /// Update the depth level of the category
    /// </summary>
    public void UpdateDepthLevel(int level)
    {
        if (level < 0 || level > MaxCategoryDepth)
        {
            throw new ArgumentException($"Category depth cannot exceed {MaxCategoryDepth} levels", nameof(level));
        }

        DepthLevel = level;
    }

    /// <summary>
    /// Update the child count (cached value)
    /// </summary>
    public void UpdateChildCount(int count)
    {
        if (count < 0)
        {
            throw new ArgumentException("Child count cannot be negative", nameof(count));
        }

        ChildCount = count;
    }

    /// <summary>
    /// Increment the child count
    /// </summary>
    public void IncrementChildCount()
    {
        ChildCount++;
    }

    /// <summary>
    /// Decrement the child count
    /// </summary>
    public void DecrementChildCount()
    {
        if (ChildCount > 0)
        {
            ChildCount--;
        }
    }

    /// <summary>
    /// Check if changing to a new parent would create a circular reference
    /// This would be implemented at the service/repository level with access to all categories
    /// </summary>
    /// <param name="newParentId">The proposed new parent category ID</param>
    /// <param name="allCategories">All categories for circular reference checking</param>
    /// <returns>True if would create circular reference, false otherwise</returns>
    public bool WouldCreateCircularReference(Guid? newParentId, IEnumerable<Category> allCategories)
    {
        if (newParentId == null)
            return false;

        if (newParentId == Id)
            return true;

        // Check if newParent is a descendant of this category
        var descendants = GetAllDescendants(allCategories).Select(c => c.Id).ToList();
        return descendants.Contains(newParentId.Value);
    }

    /// <summary>
    /// Get all descendant categories recursively
    /// </summary>
    private IEnumerable<Category> GetAllDescendants(IEnumerable<Category> allCategories)
    {
        var result = new List<Category>();

        foreach (var child in SubCategories)
        {
            result.Add(child);
            result.AddRange(child.GetAllDescendants(allCategories));
        }

        return result;
    }

    /// <summary>
    /// Get the category hierarchy path as a list of names
    /// </summary>
    public IEnumerable<string> GetHierarchyPath()
    {
        var path = new List<string>();

        if (!string.IsNullOrEmpty(BreadcrumbPath))
        {
            path.AddRange(BreadcrumbPath.Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries));
        }

        return path;
    }
}
