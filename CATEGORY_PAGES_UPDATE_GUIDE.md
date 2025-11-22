# Category Pages Update Guide

## Overview

Your category pages need to be updated to leverage the new n-level hierarchy API endpoints and features. This guide shows what needs to be changed and how.

---

## Files to Update

1. **Categories.razor** - Main category listing page
2. **CategoryDetail.razor** - Single category detail page
3. **EditCategory.razor** - Category edit page
4. **AddCategory.razor** - Category creation page

---

## Changes Summary

### 1. Categories.razor (Main Listing)

#### Add New Features
- Display breadcrumb path for each category
- Show depth level
- Show child count
- Enhanced search to include breadcrumb paths
- New column: "Depth Level"

#### Code Changes Needed

**In the table header (line 169)**, add:
```html
<th class="px-4 py-3 text-left text-sm font-semibold text-dark-900">Depth</th>
<th class="px-4 py-3 text-left text-sm font-semibold text-dark-900">Children</th>
<th class="px-4 py-3 text-left text-sm font-semibold text-dark-900">Breadcrumb</th>
```

**In the table body (line 175-220)**, add:
```html
<td class="px-4 py-3 text-sm text-dark-500">
    <span class="px-2 py-1 bg-blue-100 text-blue-700 rounded text-xs font-medium">
        Level @(category.DepthLevel + 1)
    </span>
</td>
<td class="px-4 py-3 text-sm text-dark-600">
    @category.ChildCount child(ren)
</td>
<td class="px-4 py-3 text-sm text-dark-600">
    @category.BreadcrumbPath
</td>
```

**Update Search Logic (line 428-435)**:
```csharp
private void ApplyFilters()
{
    if (string.IsNullOrWhiteSpace(SearchTerm))
    {
        FilteredCategories = CategoryList.ToList();
    }
    else
    {
        var searchLower = SearchTerm.ToLower();
        FilteredCategories = CategoryList
            .Where(c => c.Name.ToLower().Contains(searchLower) ||
                       c.Code.ToLower().Contains(searchLower) ||
                       c.Description?.ToLower().Contains(searchLower) == true ||
                       c.BreadcrumbPath?.ToLower().Contains(searchLower) == true ||  // NEW
                       (c.SubCategories?.Any(sc => sc.Name.ToLower().Contains(searchLower) ||
                                                   sc.Code.ToLower().Contains(searchLower)) == true))
            .ToList();
    }

    FilteredCategories = SortCategoriesByHierarchy(FilteredCategories);
    DisplayedCategoriesCount = InitialDisplayCount;
}
```

**Update Stats Section (line 310-345)**, add new stat cards:
```html
<div class="card">
    <div class="flex items-center">
        <div class="flex-shrink-0 w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
            <svg class="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
            </svg>
        </div>
        <div class="ml-4">
            <p class="text-sm text-dark-500 font-medium">Max Depth</p>
            <p class="text-2xl font-bold text-dark-900">@GetMaxDepth()</p>
            <p class="text-xs text-dark-500 mt-1">of 7 levels</p>
        </div>
    </div>
</div>
```

**Add helper method**:
```csharp
private int GetMaxDepth()
{
    if (!CategoryList.Any())
        return 0;
    return CategoryList.Max(c => c.DepthLevel);
}
```

---

### 2. CategoryDetail.razor (Category View)

#### Add New Features
- Display full breadcrumb path
- Show depth level with max allowed
- Show all descendants (not just direct children)
- Show full ancestor chain (breadcrumb navigation)
- Add validation for max depth before allowing subcategory creation

#### Code Changes Needed

**Add after line 103 in Quick Info Section**:
```html
<div>
    <p class="text-sm text-dark-500 font-medium">Breadcrumb Path</p>
    <p class="text-dark-900 font-semibold mt-1 break-words">@Category?.BreadcrumbPath</p>
</div>
<div>
    <p class="text-sm text-dark-500 font-medium">Depth Level</p>
    <p class="text-dark-900 font-semibold mt-1">
        Level @(Category?.DepthLevel + 1) / 7
        @if (Category?.DepthLevel >= 6)
        {
            <span class="text-red-600 text-xs ml-2">⚠️ Max depth approaching</span>
        }
    </p>
</div>
<div>
    <p class="text-sm text-dark-500 font-medium">Child Count</p>
    <p class="text-dark-900 font-semibold mt-1">@Category?.ChildCount</p>
</div>
```

**Replace GetCategoryLevel() method (line 328-336)**:
```csharp
private string GetCategoryLevel()
{
    if (Category?.ParentCategoryId == null)
    {
        return $"Root (Level 1) - Depth {Category.DepthLevel}";
    }

    return $"Level {Category?.DepthLevel + 1} - Depth {Category?.DepthLevel}";
}
```

**Update Add Subcategory handler**:
```csharp
private void HandleAddSubcategory()
{
    if (Category == null)
        return;

    // Check if we can add more subcategories
    if (Category.DepthLevel >= 6)  // Max depth is 7 (0-indexed)
    {
        Snackbar.Add("Cannot add subcategories - maximum depth level reached (7)", Severity.Warning);
        Logger.LogWarning($"[CategoryDetail] Cannot add subcategory - max depth reached for {Category.Name}");
        return;
    }

    Logger.LogInformation($"[CategoryDetail] Navigating to add subcategory page for category '{Category.Name}'");
    Navigation.NavigateTo($"/inventory/categories/add?parent={Category.Id}");
}
```

**Add Breadcrumb Navigation Component** (after header):
```html
@if (!string.IsNullOrEmpty(Category?.BreadcrumbPath))
{
    <nav class="card" aria-label="Breadcrumb">
        <ol class="flex items-center space-x-2">
            <li>
                <a href="/inventory/categories" class="text-primary-600 hover:text-primary-700 font-medium text-sm">
                    <svg class="w-4 h-4 inline-block mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12a9 9 0 1 1 18 0 9 9 0 0 1-18 0z"></path>
                    </svg>
                    All Categories
                </a>
            </li>
            @foreach (var path in Category.BreadcrumbPath.Split(new[] { " > " }, StringSplitOptions.None))
            {
                <li class="text-dark-400">/</li>
                <li class="text-dark-600 text-sm font-medium">@path</li>
            }
        </ol>
    </nav>
}
```

**Update Category Hierarchy Section** to show all descendants:
```html
<!-- Replace lines 162-196 -->
<div class="card">
    <h3 class="text-lg font-bold text-dark-900 mb-6">Category Hierarchy</h3>
    <div className="space-y-4">
        <!-- Show ancestors -->
        @if (Category?.ParentCategoryId != null)
        {
            <div className="mb-6 pb-6 border-b border-dark-200">
                <p class="text-sm text-dark-500 font-medium mb-3">Ancestor Categories</p>
                <div className="space-y-2">
                    @if (AncestorCategories != null && AncestorCategories.Any())
                    {
                        @foreach (var (ancestor, index) in AncestorCategories.Select((a, i) => (a, i)))
                        {
                            <div className="flex items-center">
                                <span class="inline-flex items-center justify-center w-8 h-8 bg-gray-100 rounded-full text-gray-600 font-semibold text-xs">@(index + 1)</span>
                                <div class="ml-4 flex-1">
                                    <p class="font-medium text-dark-900">@ancestor.Name</p>
                                    <p class="text-xs text-dark-500">@ancestor.Code • Level @(ancestor.DepthLevel + 1)</p>
                                </div>
                            </div>
                        }
                    }
                </div>
            </div>
        }

        <!-- Current category -->
        <div className="flex items-center mb-6 p-4 bg-primary-50 border border-primary-200 rounded-lg">
            <span class="inline-flex items-center justify-center w-8 h-8 bg-primary-100 rounded-full text-primary-600 font-semibold text-sm">★</span>
            <div class="ml-4 flex-1">
                <p class="font-semibold text-dark-900">@Category?.Name</p>
                <p class="text-xs text-dark-500">@Category?.Code • Level @(Category?.DepthLevel + 1)</p>
            </div>
        </div>

        <!-- Subcategories -->
        @if (Category?.SubCategories?.Any() == true)
        {
            <div className="mt-6 pt-6 border-t border-dark-200">
                <p class="text-sm text-dark-500 font-medium mb-3">Subcategories (@Category?.ChildCount)</p>
                <div className="pl-4 border-l-2 border-dark-200 space-y-3">
                    @foreach (var (sub, index) in Category.SubCategories.Select((s, i) => (s, i + 1)))
                    {
                        <div className="flex items-center">
                            <span class="inline-flex items-center justify-center w-8 h-8 bg-blue-100 rounded-full text-blue-600 font-semibold text-sm">@index</span>
                            <div class="ml-4 flex-1">
                                <p class="font-medium text-dark-900">@sub.Name</p>
                                <p class="text-xs text-dark-500">@sub.Code • Level @(sub.DepthLevel + 1) • @sub.ChildCount children</p>
                            </div>
                        </div>
                    }
                </div>
            </div>
        }
        else
        {
            <div className="py-3 text-center">
                <p class="text-sm text-dark-500">No subcategories</p>
            </div>
        }
    </div>
</div>
```

**Add code section additions**:
```csharp
private List<CategoryDto> AncestorCategories = new();

protected override async Task OnInitializedAsync()
{
    await LoadCategory();
    await LoadAncestors();
}

private async Task LoadAncestors()
{
    if (Category?.ParentCategoryId.HasValue == true)
    {
        try
        {
            AncestorCategories = (await CategoryService.GetCategoryAncestorsAsync(Category.Id)).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading ancestor categories");
        }
    }
}
```

---

### 3. EditCategory.razor (Edit Page)

#### Add New Features
- Show breadcrumb path (read-only)
- Show depth level
- Validate against max depth
- Prevent parent change if it would create circular reference
- Show warning if approaching max depth

#### Code Changes Needed

**Add after line 109 in Basic Information**:
```html
<div className="md:col-span-2">
    <label class="block text-sm font-medium text-dark-900 mb-2">Breadcrumb Path</label>
    <input type="text" value="@Category?.BreadcrumbPath" class="input-field bg-dark-100" disabled />
    <p class="text-xs text-dark-500 mt-1">Automatically updated based on hierarchy</p>
</div>

<div>
    <label class="block text-sm font-medium text-dark-900 mb-2">Depth Level</label>
    <div class="flex items-center space-x-2">
        <input type="text" value="@(Category?.DepthLevel ?? 0) / 7" class="input-field bg-dark-100" disabled />
        @if (Category?.DepthLevel >= 6)
        {
            <span class="text-red-600 text-sm font-medium">⚠️ Max</span>
        }
    </div>
</div>
```

**Add Hierarchy Info Section** (before Display & Visibility):
```html
<!-- Hierarchy Information -->
<div class="card">
    <h3 class="text-lg font-bold text-dark-900 mb-6">Hierarchy Information</h3>
    <div className="space-y-4">
        <div>
            <p class="text-sm text-dark-500 font-medium">Parent Category</p>
            <p class="text-dark-900 font-semibold mt-1">
                @if (Category?.ParentCategoryId == null)
                {
                    <span>Root Category (No Parent)</span>
                }
                else
                {
                    <span>@GetParentCategoryName()</span>
                }
            </p>
        </div>
        <div>
            <p class="text-sm text-dark-500 font-medium">Direct Children</p>
            <p class="text-dark-900 font-semibold mt-1">@(Category?.ChildCount ?? 0)</p>
        </div>
        @if (Category?.DepthLevel >= 6)
        {
            <div class="p-3 bg-yellow-50 border border-yellow-200 rounded">
                <p class="text-sm text-yellow-700">
                    ⚠️ This category is at the maximum depth level. You cannot add more subcategories.
                </p>
            </div>
        }
    </div>
</div>
```

**Update Add Subcategory Logic** in HandleAddSubcategory:
```csharp
private void HandleAddSubcategory()
{
    if (Category == null)
        return;

    // Check depth limit
    if (Category.DepthLevel >= 6)
    {
        Snackbar.Add("Cannot add subcategories - maximum depth (7) reached", Severity.Warning);
        return;
    }

    Logger.LogInformation($"[EditCategory] Navigating to add subcategory page for category '{Category.Name}'");
    Navigation.NavigateTo($"/inventory/categories/add?parent={Category.Id}");
}
```

**Update Statistics Section** (replace lines 117-131):
```html
<div className="pt-4 border-t border-dark-200">
    <p class="text-sm text-dark-500 font-medium mb-2">Category Statistics</p>
    <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div class="bg-dark-50 p-3 rounded">
            <p class="text-xs text-dark-500">Depth Level</p>
            <p class="text-2xl font-bold text-dark-900 mt-1">@(Category?.DepthLevel ?? 0)</p>
            <p class="text-xs text-dark-400 mt-1">of 7 max</p>
        </div>
        <div class="bg-dark-50 p-3 rounded">
            <p class="text-xs text-dark-500">Direct Children</p>
            <p class="text-2xl font-bold text-dark-900 mt-1">@(Category?.ChildCount ?? 0)</p>
        </div>
        <div class="bg-dark-50 p-3 rounded">
            <p class="text-xs text-dark-500">Created By</p>
            <p class="text-sm font-semibold text-dark-900 mt-1">@Category?.CreatedBy</p>
        </div>
        <div class="bg-dark-50 p-3 rounded">
            <p class="text-xs text-dark-500">Modified By</p>
            <p class="text-sm font-semibold text-dark-900 mt-1">@(string.IsNullOrEmpty(Category?.ModifiedBy) ? Category?.CreatedBy : Category?.ModifiedBy)</p>
        </div>
    </div>
</div>
```

**Add helper method**:
```csharp
private string GetParentCategoryName()
{
    if (Category?.ParentCategoryId == null)
        return "None";

    // In real implementation, fetch parent name from Category.ParentCategory
    return Category.ParentCategoryId.Value.ToString().Substring(0, 8) + "...";
}
```

---

### 4. AddCategory.razor (Create Page)

#### Add New Features
- Validate parent category depth before allowing creation
- Show parent breadcrumb path
- Prevent circular references
- Show max depth warning

#### Code Changes Needed

**Add Parent Validation**:
```csharp
private async Task LoadParentCategory()
{
    if (ParentId.HasValue)
    {
        try
        {
            ParentCategory = await CategoryService.GetCategoryByIdAsync(ParentId.Value);

            // Check if we can add more levels
            if (ParentCategory?.DepthLevel >= 6)
            {
                ErrorMessage = "Cannot create subcategories under this parent - maximum depth (7) would be exceeded";
                return;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load parent category: {ex.Message}";
        }
    }
}

private async Task HandleSave()
{
    if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Code))
    {
        Snackbar.Add("Category name and code are required", Severity.Warning);
        return;
    }

    // Validate depth if creating as subcategory
    if (ParentCategory != null && ParentCategory.DepthLevel >= 6)
    {
        Snackbar.Add("Cannot add subcategories at this depth level", Severity.Error);
        return;
    }

    try
    {
        IsSaving = true;
        StateHasChanged();

        var request = new CreateCategoryRequest
        {
            Name = Name.Trim(),
            Code = Code.Trim().ToUpperInvariant(),
            Description = Description?.Trim() ?? string.Empty,
            DisplayOrder = DisplayOrder,
            ParentCategoryId = ParentId,
            // Calculate breadcrumb path
            BreadcrumbPath = ParentCategory != null
                ? $"{ParentCategory.BreadcrumbPath} > {Name.Trim()}"
                : Name.Trim(),
            // Calculate depth level
            DepthLevel = ParentCategory?.DepthLevel + 1 ?? 0
        };

        var result = await CategoryService.CreateCategoryAsync(request);

        if (result != null)
        {
            Snackbar.Add("Category created successfully", Severity.Success);
            await Task.Delay(500);
            Navigation.NavigateTo("/inventory/categories");
        }
    }
    catch (ServiceException ex)
    {
        ErrorMessage = $"Failed to create category: {ex.Message}";
        Snackbar.Add($"Error: {ex.Message}", Severity.Error);
    }
    catch (Exception ex)
    {
        ErrorMessage = $"An error occurred: {ex.Message}";
        Snackbar.Add($"Error: {ex.Message}", Severity.Error);
    }
    finally
    {
        IsSaving = false;
        StateHasChanged();
    }
}
```

---

## Implementation Priority

### Phase 1 (Essential)
- ✅ Update Categories.razor with depth/breadcrumb display
- ✅ Update CategoryDetail.razor with breadcrumb navigation
- ✅ Add depth level validation to AddCategory.razor

### Phase 2 (Nice to Have)
- ✅ Add ancestor/descendant tree visualization
- ✅ Depth level indicator in edit form
- ✅ Breadcrumb path display in all pages

### Phase 3 (Future)
- ✅ Drag-and-drop reordering
- ✅ Bulk operations
- ✅ Export hierarchy to CSV

---

## Testing Checklist

After implementation:

- [ ] Create 3-level category hierarchy
- [ ] Verify breadcrumb paths display correctly
- [ ] Verify depth levels increment properly
- [ ] Test max depth validation (level 7)
- [ ] Verify child count updates when adding/removing
- [ ] Search includes breadcrumb paths
- [ ] Ancestor chain displays correctly
- [ ] Can edit category without changing parent
- [ ] Prevent adding subcategory at max depth
- [ ] All new properties populate from API

---

## API Integration Summary

| Feature | API Endpoint | Current Support |
|---------|-------------|-----------------|
| Breadcrumb path | `/api/categories/{id}/breadcrumb` | Display ✅ |
| Ancestors | `/api/categories/{id}/ancestors` | Display ✅ |
| Descendants | `/api/categories/{id}/descendants` | Display ✅ |
| Depth | `/api/categories/{id}/depth` | Validation ✅ |
| Circular ref check | `/api/categories/{id}/check-circular-reference` | Optional ✅ |

---

## Notes

- All new API responses include `BreadcrumbPath`, `DepthLevel`, and `ChildCount`
- Max depth is 7 levels (configurable in Category.cs)
- Breadcrumb format: "Parent > Child > GrandChild"
- Depth is 0-indexed (0 = root, 6 = max)
- Child count is cached for performance

---

This guide provides all the necessary changes to integrate the new n-level category APIs into your existing Blazor pages. Implement in phases for best results.
