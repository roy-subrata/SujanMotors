# Category API Quick Reference

## All Available Endpoints

### Core CRUD Operations

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/categories` | Get all categories |
| GET | `/api/categories/{id}` | Get category by ID |
| POST | `/api/categories` | Create new category |
| PUT | `/api/categories/{id}` | Update category |
| DELETE | `/api/categories/{id}` | Delete category |
| GET | `/api/categories/active` | Get active categories only |
| GET | `/api/categories/top-level` | Get root categories |
| GET | `/api/categories/{parentId}/subcategories` | Get direct children |
| GET | `/api/categories/search/{term}` | Search by name/code |
| GET | `/api/categories/paged` | Paginated results |
| PATCH | `/api/categories/{id}/activate` | Activate category |
| PATCH | `/api/categories/{id}/deactivate` | Deactivate category |

### New Hierarchy Operations

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/categories/{id}/breadcrumb` | Get navigation path |
| GET | `/api/categories/{id}/ancestors` | Get parent chain |
| GET | `/api/categories/{id}/descendants` | Get all children |
| GET | `/api/categories/{id}/depth` | Get nesting level |
| POST | `/api/categories/{id}/check-circular-reference` | Validate move operation |

---

## Code Snippets

### C# - Using the Service

```csharp
// Inject service
public MyClass(ICategoryService categoryService)
{
    _categoryService = categoryService;
}

// Get breadcrumb
var breadcrumb = await _categoryService.GetCategoryBreadcrumbAsync(categoryId);
// Output: "Engines > Diesel > Small"

// Get all ancestors
var ancestors = await _categoryService.GetCategoryAncestorsAsync(categoryId);
// Get parent, grandparent, etc.

// Get all descendants
var descendants = await _categoryService.GetAllDescendantsAsync(categoryId);
// Get all children, grandchildren, etc.

// Check depth
var depth = await _categoryService.GetCategoryDepthAsync(categoryId);
// Returns: 0 (root), 1, 2, etc.

// Validate move
var isCircular = await _categoryService.WouldCreateCircularReferenceAsync(
    categoryToMove.Id,
    newParentId
);
if (isCircular)
{
    // Show error: "This move would create a circular reference"
}
```

### JavaScript/Fetch - HTTP Calls

```javascript
// Get breadcrumb
fetch('/api/categories/{categoryId}/breadcrumb')
  .then(r => r.json())
  .then(data => console.log(data.breadcrumbPath));

// Output: { categoryId: "...", breadcrumbPath: "Engines > Diesel" }

// Get ancestors (for breadcrumb navigation)
fetch('/api/categories/{categoryId}/ancestors')
  .then(r => r.json())
  .then(ancestors => {
    // ancestors[0] = immediate parent
    // ancestors[1] = grandparent
    // etc.
  });

// Get all descendants
fetch('/api/categories/{categoryId}/descendants')
  .then(r => r.json())
  .then(allChildren => {
    // All children at all levels
  });

// Check if move is valid
fetch('/api/categories/{categoryId}/check-circular-reference?newParentId={parentId}',
  { method: 'POST' })
  .then(r => r.json())
  .then(data => {
    if (data.wouldCreateCircularReference) {
      alert('Cannot move - would create circular reference');
    }
  });
```

### Blazor Component - Show Breadcrumbs

```razor
@page "/category/{CategoryId:guid}"
@inject ICategoryService CategoryService

<div class="breadcrumb">
    @if (!string.IsNullOrEmpty(_breadcrumbPath))
    {
        @foreach (var part in _breadcrumbPath.Split(new[] { " > " }, StringSplitOptions.None))
        {
            <span>@part</span>
            <span>/</span>
        }
    }
</div>

@code {
    [Parameter]
    public Guid CategoryId { get; set; }

    private string _breadcrumbPath = "";

    protected override async Task OnInitializedAsync()
    {
        _breadcrumbPath = await CategoryService.GetCategoryBreadcrumbAsync(CategoryId);
    }
}
```

### Blazor Component - Category Tree

```razor
@page "/categories/tree"
@inject ICategoryService CategoryService

<div class="category-tree">
    @foreach (var root in _rootCategories)
    {
        <div class="tree-item">
            <span>@root.Name</span>
            <div class="children">
                @foreach (var child in root.SubCategories)
                {
                    <CategoryTreeItem Category="child" />
                }
            </div>
        </div>
    }
</div>

@code {
    private List<CategoryDto> _rootCategories = new();

    protected override async Task OnInitializedAsync()
    {
        _rootCategories = (await CategoryService.GetTopLevelCategoriesAsync())
            .ToList();
    }
}
```

---

## Constraints & Validation

### Depth Limits
```
Maximum depth: 7 levels
- Level 0: Root categories
- Level 1-6: Subcategories
- Level 7: Maximum allowed
```

### Code Rules
```
- Code: Required, max 20 chars, unique, uppercase
- Name: Required, max 100 chars
- Description: Optional
- DisplayOrder: Non-negative integer
```

### Circular References
```
✗ Cannot: Set category as parent of its ancestor
✗ Cannot: Set category as parent of itself
✓ Can: Reparent to any valid non-descendant category
```

---

## Response Examples

### Get Single Category
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Diesel Engines",
  "code": "DES-001",
  "description": "Diesel engine components",
  "parentCategoryId": "550e8400-e29b-41d4-a716-446655440001",
  "isActive": true,
  "displayOrder": 1,
  "breadcrumbPath": "Engine Parts > Diesel Engines",
  "depthLevel": 1,
  "childCount": 5,
  "createdBy": "admin",
  "modifiedBy": "admin",
  "subCategories": [
    { "id": "...", "name": "Pistons", ... },
    { "id": "...", "name": "Cylinders", ... }
  ]
}
```

### Get Breadcrumb
```json
{
  "categoryId": "550e8400-e29b-41d4-a716-446655440000",
  "breadcrumbPath": "Engine Parts > Diesel Engines > Pistons"
}
```

### Get Depth
```json
{
  "categoryId": "550e8400-e29b-41d4-a716-446655440000",
  "depthLevel": 2,
  "maxDepth": 7
}
```

### Check Circular Reference
```json
{
  "categoryId": "550e8400-e29b-41d4-a716-446655440000",
  "newParentId": "550e8400-e29b-41d4-a716-446655440099",
  "wouldCreateCircularReference": false,
  "message": "Move is allowed"
}
```

---

## Error Responses

### 400 Bad Request
```json
{
  "message": "Name and Code are required"
}
```

### 404 Not Found
```json
{
  "message": "Category not found"
}
```

### 409 Conflict
```json
{
  "message": "Category with code 'CODE' already exists"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred while [operation]"
}
```

---

## Common Workflows

### Create a Subcategory
```
1. GET /api/categories/{parentId} - Verify parent exists
2. GET /api/categories/{parentId}/depth - Check depth < 7
3. POST /api/categories - Create with parentId
4. Response includes breadcrumbPath automatically set
```

### Move a Category
```
1. GET /api/categories/{categoryId}/check-circular-reference?newParentId={id}
   - If false, proceed
   - If true, show error "Cannot move - would create circular reference"
2. PUT /api/categories/{categoryId} - Update with new parentId
3. Breadcrumb path should be recalculated in request
```

### Display Navigation
```
1. GET /api/categories/{categoryId}/breadcrumb
2. Parse breadcrumbPath by splitting on " > "
3. Convert each part to clickable breadcrumb link
4. "Engine Parts > Diesel Engines" becomes:
   - [Home] / [Engine Parts] / [Diesel Engines]
```

### Build Tree View
```
1. GET /api/categories/top-level - Get all roots
2. For each root:
   a. GET /api/categories/{rootId}/descendants
   b. Build tree structure from results
3. Display with expand/collapse per level
```

---

## Performance Tips

| Goal | Method |
|------|--------|
| Fast breadcrumbs | GET `/breadcrumb` endpoint |
| Build UI tree | Use `/descendants` (one call per root) |
| Show parent chain | Use `/ancestors` endpoint |
| Validate move | Use `/check-circular-reference` |
| Count children | Use `childCount` property (cached) |

---

## Database Migration (When Using EF Core)

Add these columns to Categories table:
```sql
ALTER TABLE Categories ADD
    BreadcrumbPath NVARCHAR(500) DEFAULT '',
    DepthLevel INT DEFAULT 0,
    ChildCount INT DEFAULT 0;

CREATE INDEX IX_Categories_Breadcrumb ON Categories(BreadcrumbPath);
CREATE INDEX IX_Categories_Depth ON Categories(DepthLevel);
```

---

## Troubleshooting

### Issue: Circular Reference Error on Valid Move
**Solution**: Verify `newParentId` is not a descendant of the category being moved.

### Issue: Breadcrumb Shows Only Category Name
**Solution**: Breadcrumb path should be set during category creation/update.

### Issue: Cannot Create Subcategory
**Solution**: Check if parent depth equals 7 (maximum).

### Issue: Child Count Not Updating
**Solution**: Child count is cached - update manually when adding/removing children.

---

## See Also
- Full documentation: [N_LEVEL_CATEGORY_ENHANCEMENTS.md](N_LEVEL_CATEGORY_ENHANCEMENTS.md)
- Entity code: `src/AutoPartShop.Domain/Entities/Category.cs`
- API code: `src/AutoPartShop.Api/Controllers/CategoriesController.cs`
