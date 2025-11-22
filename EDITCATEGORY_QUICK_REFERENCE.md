# EditCategory Component - Quick Reference Guide

## Component Overview

**File:** `src/AutoPartShop.Web/Components/Pages/Inventory/EditCategory.razor`
**Route:** `/inventory/categories/{id}/edit`
**Status:** ✅ Fully Functional

---

## Supported Editable Fields (5)

| # | Field | Type | API Support | Validation |
|---|-------|------|-------------|-----------|
| 1 | **Name** | String | UpdateCategoryRequest | Required |
| 2 | **Description** | Text | UpdateCategoryRequest | Optional |
| 3 | **Display Order** | Integer | UpdateCategoryRequest | Optional |
| 4 | **Is Active** | Boolean | UpdateCategoryRequest | Optional |
| 5 | **Code** | String | API Read-only | N/A |

---

## Unsupported Fields (Removed)

Fields that were in the UI but have **NO API support**:
- Display Name, Icon, Background Color
- Meta Title, Meta Description, URL Slug
- Visibility/Permissions, Tags, Related Categories
- Maximum Nesting Depth, Visible in Menu, Searchable

---

## UI States

### 1. Loading State
```
[Spinner icon] Loading category...
```
- Shown on component initialization
- Prevents form rendering until data loaded
- User cannot interact with form

### 2. Error State
```
[Error icon] Error Loading Category
Error message displayed
[Retry Button]
```
- Shown when API returns error or fails
- Shows specific error message
- User can click Retry to reload

### 3. Form Loaded State
```
[Category information form with all fields]
[Cancel] [Reset] [Save]
```
- Normal editing state
- All buttons enabled
- Form is interactive

### 4. Saving State
```
[Form remains visible]
[Spinning icon] Saving...
```
- Shown while UpdateCategoryAsync is in progress
- All buttons disabled
- Cannot cancel while saving

### 5. Delete Blocked State
```
[Yellow warning card]
Cannot Delete: This category has X subcategories
```
- Shown when category has children
- Delete button not displayed
- User must delete children first

---

## Button Actions

### Cancel Button
```
Click → HandleCancel()
        ↓
        Navigate to /inventory/categories
```

### Reset Button
```
Click → RetryLoadCategory()
        ↓
        Reload category from API
        ↓
        Reset form to original values
```

### Save Button
```
Click → HandleSave()
        ↓
        Validate (name not empty)
        ↓
        Call UpdateCategoryAsync
        ↓
        Show success notification
        ↓
        Update form with response
```

### Delete Button (if no children)
```
Click → HandleDelete()
        ↓
        Show confirmation dialog
        ↓
        User confirms
        ↓
        Call DeleteCategoryAsync
        ↓
        Show success notification
        ↓
        Navigate to /inventory/categories
```

---

## API Endpoints Used

### Load Category
```
GET /api/categories/{id}
Returns: CategoryDto
```

### Save Changes
```
PUT /api/categories/{id}
Body: UpdateCategoryRequest {
    Id,
    Name,
    Description,
    DisplayOrder,
    IsActive
}
Returns: CategoryDto
```

### Delete Category
```
DELETE /api/categories/{id}
Returns: (no content)
```

---

## Notifications

### Success Notifications (Green)
- ✅ "Category updated successfully"
- ✅ "Category deleted successfully"

### Error Notifications (Red)
- ❌ "Error: [error message]"
- ❌ "Failed to update category: [message]"
- ❌ "Failed to delete category: [message]"

### Warning Notifications (Yellow)
- ⚠️ "Category name is required"

---

## Key Component Properties

```csharp
private CategoryDto? Category;           // Current form data
private CategoryDto? OriginalCategory;   // Original data (for reset)
private bool IsLoading = true;           // Is data loading?
private bool IsSaving = false;           // Is save/delete in progress?
private string ErrorMessage = string.Empty; // Error message to display
```

---

## Data Flow

```
User navigates to /inventory/categories/{id}/edit
        ↓
OnInitializedAsync() called
        ↓
LoadCategory() async method executes
        ↓
GetCategoryByIdAsync(id) called
        ↓
Response received, Category property set
        ↓
UI re-renders with loaded data
        ↓
Form ready for editing
        ↓
User makes changes and clicks Save
        ↓
HandleSave() validates and calls UpdateCategoryAsync
        ↓
Response received, Category updated
        ↓
Success notification shown
        ↓
UI refreshed with updated data
```

---

## Error Handling Flow

```
API Call Fails
        ↓
Exception caught (ServiceException or generic)
        ↓
ErrorMessage property set
        ↓
StateHasChanged() called to re-render
        ↓
Error card displayed with message
        ↓
User sees [Retry] button
        ↓
Click Retry → RetryLoadCategory() → LoadCategory()
        ↓
Attempt to load again
```

---

## Form Validation

| Field | Required | Rule |
|-------|----------|------|
| Name | ✅ Yes | Cannot be empty/whitespace |
| Description | ❌ No | Any length allowed |
| Display Order | ❌ No | Integer ≥ 0 |
| Is Active | ❌ No | Boolean |

---

## Logging Events

All operations are logged to console/debug:

```csharp
// On successful save
[EditCategory] Category 'X' (ID: Y) updated successfully

// On error during save
[EditCategory] ServiceException: {error message}

// On cancel
[EditCategory] Navigation back to categories list

// On successful delete
[EditCategory] Category 'X' (ID: Y) deleted successfully

// On error during delete
[EditCategory] Delete ServiceException: {error message}
```

---

## Responsive Design

### Mobile (< 768px)
- Single column layout
- Stacked buttons (full width)
- Form fields use 100% width

### Tablet/Desktop (≥ 768px)
- Grid layouts (1-2 columns)
- Buttons in row
- Form fields side-by-side where applicable

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| None | No keyboard shortcuts configured |

---

## Accessibility Features

- ✅ Proper `<label>` associations with form inputs
- ✅ Semantic HTML (`<form>`, proper button types)
- ✅ ARIA descriptions for help text
- ✅ Color-coded notifications (not color-only)
- ✅ Clear error messages
- ✅ Disabled state visual feedback

---

## Performance Notes

- ✅ Async/await for non-blocking operations
- ✅ Single API call to load category
- ✅ StateHasChanged() called only when needed
- ✅ No unnecessary re-renders
- ✅ Button disabled during operations to prevent double-clicks

---

## Common Scenarios

### Scenario 1: Edit Name and Save
```
1. Navigate to edit page
2. Page loads category data
3. Modify Category Name field
4. Click Save Changes
5. Success notification shows
6. Page updates with new data
```

### Scenario 2: Delete Category
```
1. Scroll to Danger Zone
2. Click Delete Category button
3. Confirmation dialog appears
4. Click "Delete" in dialog
5. Page shows "Category deleted successfully"
6. Automatically redirected to categories list
```

### Scenario 3: Handle Error and Retry
```
1. Page tries to load but API times out
2. Error state shown: "Failed to load category"
3. User clicks Retry button
4. Page attempts to load again
5. Successfully loads if API is back online
```

### Scenario 4: Cancel Without Saving
```
1. User makes changes to form
2. User clicks Cancel button
3. Page immediately navigates back to /inventory/categories
4. Unsaved changes are discarded
```

---

## Testing Checklist

- [ ] Load existing category
- [ ] Edit name field
- [ ] Edit description field
- [ ] Edit display order
- [ ] Toggle active checkbox
- [ ] Save changes successfully
- [ ] Verify success notification
- [ ] Cancel and verify navigation
- [ ] Reset form and verify reload
- [ ] Delete category (no children)
- [ ] Verify delete confirmation dialog
- [ ] Try delete with children (should show warning)
- [ ] Test with invalid category ID
- [ ] Test network error and retry
- [ ] Test on mobile device
- [ ] Test all button disabled states during save

---

## Troubleshooting

### "Category not found" Error
- Verify the GUID in URL is correct
- Check if category was already deleted
- Try refreshing the page

### "Failed to update category" Error
- Check if category name is empty
- Verify network connectivity
- Check if another user deleted the category
- Click Reset to reload current state

### Delete button disabled with no children shown
- Refresh the page
- Check if children were added while page was open
- Navigate back and return to page

### Form not updating after save
- Check browser console for JavaScript errors
- Verify API response contains updated data
- Try clicking Reset button

---

## Related Components

- [Categories.razor](src/AutoPartShop.Web/Components/Pages/Inventory/Categories.razor) - Categories list page
- [AddCategory.razor](src/AutoPartShop.Web/Components/Pages/Inventory/AddCategory.razor) - Add new category page
- [ConfirmDeleteDialog.razor](src/AutoPartShop.Web/Components/Dialogs/ConfirmDeleteDialog.razor) - Delete confirmation dialog

---

## Related Services

- `ICategoryService` - Category API service
- `ISnackbar` - MudBlazor notification service
- `IDialogService` - MudBlazor dialog service
- `NavigationManager` - Blazor navigation
- `ILogger<EditCategory>` - Logging service

---

## Configuration

No external configuration needed. All settings are in code:
- Service dependencies injected at top of component
- API endpoints handled by CategoryService
- Notifications shown via Snackbar service
