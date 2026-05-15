# CategoryDetail.razor - Button Reference Guide 📍

**Quick Reference for All Buttons and Their Navigation Routes**

---

## Button Overview

### Header Section (Top Right)

| # | Button | Icon | Tooltip | Action |
|---|--------|------|---------|--------|
| 1 | **Print** | 🖨️ | Print category details | Shows info message (Placeholder) |
| 2 | **Export** | 📥 | Export category data | Shows info message (Placeholder) |
| 3 | **Edit** | ✏️ | Navigate to: /inventory/categories/{id}/edit | Opens edit page |

### Quick Info Card (Left Sidebar)

| # | Button | Icon | Tooltip | Action |
|---|--------|------|---------|--------|
| 4 | **Add Subcategory** | ➕ | Navigate to: /inventory/categories/add?parent={id} | Opens add page with parent set |
| 5 | **View All Parts** | 📦 | Navigate to: /inventory/products?category={id} | Shows products filtered by category |

---

## Full Navigation Map

```
CategoryDetail Page (/inventory/categories/{id})
│
├─ Print Button
│  └─ Current Page (No navigation)
│
├─ Export Button
│  └─ Current Page (No navigation)
│
├─ Edit Category Button
│  └─ /inventory/categories/{id}/edit (EditCategory page)
│
├─ Add Subcategory Button
│  └─ /inventory/categories/add?parent={id} (AddCategory page with parent parameter)
│
└─ View All Parts Button
   └─ /inventory/products?category={id} (Products list filtered by category)
```

---

## Button Details

### 1. Print Button
- **Location:** Top right, gray button
- **Purpose:** Print category details
- **Tooltip:** "Print category details"
- **Navigation:** None (stays on current page)
- **Status:** ⏳ Placeholder implementation
- **Handler:** `HandlePrint()` (Line 307)

### 2. Export Button
- **Location:** Top right, gray button
- **Purpose:** Export category data
- **Tooltip:** "Export category data"
- **Navigation:** None (stays on current page)
- **Status:** ⏳ Placeholder implementation
- **Handler:** `HandleExport()` (Line 316)

### 3. Edit Category Button ⭐
- **Location:** Top right, blue button (primary color)
- **Purpose:** Edit category details
- **Tooltip:** "Navigate to: /inventory/categories/{id}/edit"
- **Navigation:** ✅ `/inventory/categories/{id}/edit`
- **Page Opens:** EditCategory.razor (edit mode)
- **Status:** ✅ Fully functional
- **Handler:** `HandleEdit()` (Line 280)
- **What You Can Do:**
  - Edit name, description, display order
  - Toggle active/inactive status
  - Save changes to database
  - Delete category (if no subcategories)

### 4. Add Subcategory Button ⭐
- **Location:** Left sidebar, blue button
- **Purpose:** Create a new subcategory
- **Tooltip:** "Navigate to: /inventory/categories/add?parent={id}"
- **Navigation:** ✅ `/inventory/categories/add?parent={id}`
- **Page Opens:** AddCategory page with parent pre-selected
- **Status:** ✅ Fully functional
- **Handler:** `HandleAddSubcategory()` (Line 289)
- **What You Can Do:**
  - Create a new category as a child of current category
  - Parent category ID is automatically set
  - Fill in name, code, description
  - Save to database

### 5. View All Parts Button ⭐
- **Location:** Left sidebar, gray button
- **Purpose:** See all products in this category
- **Tooltip:** "Navigate to: /inventory/products?category={id}"
- **Navigation:** ✅ `/inventory/products?category={id}`
- **Page Opens:** Products list filtered by category
- **Status:** ✅ Fully functional
- **Handler:** `HandleViewParts()` (Line 298)
- **What You Can Do:**
  - See all products in this category
  - Filter by category ID (query parameter)
  - Browse products, search, sort
  - Manage inventory

---

## Navigation Flow Diagram

```
┌─────────────────────────────────────────┐
│     CategoryDetail Page                  │
│  /inventory/categories/{id}             │
│                                         │
│  [Print]  [Export]  [Edit] ────────┐  │
│                                    │  │
│  [Add Subcategory] ────────┐      │  │
│                            │      │  │
│  [View All Parts] ──┐      │      │  │
│                     │      │      │  │
└──────────────────────┼──────┼──────┼──┘
                       │      │      │
                       │      │      ▼
                       │      │  EditCategory
                       │      │  /inventory/categories/{id}/edit
                       │      │
                       │      ▼
                       │  AddCategory
                       │  /inventory/categories/add?parent={id}
                       │
                       ▼
                   Products List
                   /inventory/products?category={id}
```

---

## Keyboard & Accessibility

All buttons have:
- ✅ `title` attribute for tooltip on hover
- ✅ Semantic HTML `<button>` elements
- ✅ Proper ARIA labels (implicit from button text)
- ✅ Keyboard accessible (Tab navigation works)
- ✅ Click handlers attached with `@onclick`

---

## Implementation Code References

### Handler Methods (in @code block, starting Line 280)

```csharp
private void HandleEdit()              // Line 280 - Navigate to edit page
private void HandleAddSubcategory()    // Line 289 - Navigate to add page with parent
private void HandleViewParts()         // Line 298 - Navigate to products list
private void HandlePrint()             // Line 307 - Show info message (placeholder)
private void HandleExport()            // Line 316 - Show info message (placeholder)
```

### Button HTML Locations

```html
Print Button          → Line 54
Export Button         → Line 60
Edit Category Button  → Line 66
Add Subcategory       → Line 126
View All Parts        → Line 132
```

---

## State & Availability

All buttons are always visible and enabled when:
- ✅ Category is loaded successfully
- ✅ Category object is not null
- ✅ Page is showing the content state (not loading or error)

Buttons are hidden when:
- ❌ Page is loading (spinner shows)
- ❌ Error occurred (error message shows)
- ❌ Category not found

---

## Testing Checklist

- [ ] Hover over Edit button → Tooltip shows "/inventory/categories/{id}/edit"
- [ ] Click Edit button → Navigates to edit page
- [ ] Hover over Add Subcategory → Tooltip shows "/inventory/categories/add?parent={id}"
- [ ] Click Add Subcategory → Navigates with parent parameter
- [ ] Hover over View Parts → Tooltip shows "/inventory/products?category={id}"
- [ ] Click View Parts → Navigates with category filter
- [ ] Hover over Print → Shows "Print category details"
- [ ] Hover over Export → Shows "Export category data"
- [ ] All buttons disabled when loading/error states shown

---

## Summary

| Button | Route | Status | Functional |
|--------|-------|--------|-----------|
| Print | Current | ⏳ Placeholder | ❌ |
| Export | Current | ⏳ Placeholder | ❌ |
| Edit | /inventory/categories/{id}/edit | ✅ Active | ✅ |
| Add Sub | /inventory/categories/add?parent={id} | ✅ Active | ✅ |
| View Parts | /inventory/products?category={id} | ✅ Active | ✅ |

---

**Last Updated:** 2025-11-19
**All buttons have tooltips and navigation references!** 🎯
