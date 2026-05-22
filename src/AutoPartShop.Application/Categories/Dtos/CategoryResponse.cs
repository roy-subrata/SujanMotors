
namespace AutoPartShop.Application.Categories.Dtos
{
    public class CategoryResponse
    {
        /// <summary>
        /// Category ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Category name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Category description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Category code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Parent category ID (if this is a subcategory)
        /// </summary>
        public Guid? ParentCategoryId { get; set; }

        /// <summary>
        /// Whether the category is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Display order in lists
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Subcategories (only populated when requested)
        /// </summary>
        public IList<CategoryResponse> SubCategories { get; set; } = new List<CategoryResponse>();

        /// <summary>
        /// Breadcrumb path for navigation (e.g., "Engines > Diesel > Small")
        /// </summary>
        public string BreadcrumbPath { get; set; } = string.Empty;

        /// <summary>
        /// Depth level in the hierarchy (0 = root)
        /// </summary>
        public int DepthLevel { get; set; } = 0;

        /// <summary>
        /// Count of direct child categories
        /// </summary>
        public int ChildCount { get; set; } = 0;

    }
}