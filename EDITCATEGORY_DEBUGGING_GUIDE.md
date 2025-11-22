# EditCategory - Data Population Debugging Guide

---

## 🔍 How to Verify All Properties Are Loaded

### Method 1: Visual UI Inspection

Navigate to: `/inventory/categories/{category-id}/edit`

**Check these fields appear with data:**

```
✓ Name field           → Should show category name
✓ Code field           → Should show category code
✓ Description          → Should show category description
✓ Display Order        → Should show a number
✓ Active Checkbox      → Should be checked or unchecked
✓ Created By           → Should show username
✓ Modified By          → Should show username
✓ Subcategories count  → Should show number (0 or higher)
```

If all these show data → **✅ All properties loaded successfully**

---

### Method 2: Browser Developer Tools (F12)

**Step 1: Open DevTools**
- Press `F12` or right-click → Inspect

**Step 2: Find the Category Object**
- Open Console tab
- Type: `window.__INITIAL_STATE__` or similar (depends on Blazor setup)

OR

**Step 3: Check Network Tab**
- Look for API call to `GET /api/categories/{id}`
- Response should contain all 10 properties:
```json
{
  "id": "550e8400-...",
  "name": "Engine Components",
  "description": "...",
  "code": "CAT-001",
  "parentCategoryId": null,
  "displayOrder": 1,
  "isActive": true,
  "createdBy": "admin@example.com",
  "modifiedBy": "user@example.com",
  "subCategories": [
    { "id": "...", "name": "...", ... },
    { "id": "...", "name": "...", ... }
  ]
}
```

✅ **If all 10 fields are in the response → data is loaded**

---

### Method 3: Application Logs

**Check the application console output for:**

```log
[EditCategory] Category 'Engine Components' (ID: 550e8400-...) updated successfully
[EditCategory] Navigation back to categories list
[EditCategory] Delete ServiceException: ...
```

These log messages confirm the component is loading and processing data.

---

### Method 4: Add Debug Output to Component

If you want to see what's loaded, add this to the EditCategory.razor file temporarily:

```razor
@* Add this in the form section to debug *@
@if (Category != null)
{
    <div class="card bg-blue-50 border-2 border-blue-400 p-4" style="display:none;">
        <h4>DEBUG: Category Properties</h4>
        <pre style="font-size: 11px;">
Id:                @Category.Id
Name:              @Category.Name
Code:              @Category.Code
Description:       @(Category.Description?.Substring(0, 50) ?? "null")
ParentCategoryId:  @Category.ParentCategoryId
DisplayOrder:      @Category.DisplayOrder
IsActive:          @Category.IsActive
CreatedBy:         @Category.CreatedBy
ModifiedBy:        @Category.ModifiedBy
SubCategories:     @Category.SubCategories?.Count ?? 0
        </pre>
    </div>
}
```

**Set `display:block` to see the debug info, then set back to `display:none`**

---

## 📍 Property Display Locations - Quick Reference

| Property | Line # | UI Element | Shows As |
|----------|--------|-----------|----------|
| Id | 259 | (Internal) | Not shown |
| **Name** | **96** | **Input field** | **Text box** |
| **Code** | **100** | **Disabled input** | **Read-only text** |
| **Description** | **114** | **Textarea** | **Multi-line text** |
| ParentCategoryId | 101 | Disabled select | Dropdown |
| Category Level | 115 | Disabled input | Read-only text |
| **DisplayOrder** | **157** | **Number input** | **Numeric field** |
| **IsActive** | **143** | **Checkbox** | **Checked/unchecked** |
| **CreatedBy** | **126** | **Stat box** | **Boxed text** |
| **ModifiedBy** | **130** | **Stat box** | **Boxed text** |
| **SubCategories** | **122, 164, 184** | **Multiple** | **Count/Logic** |

---

## 🔄 Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      USER NAVIGATION                             │
│                 /inventory/categories/{id}/edit                  │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ↓
                    ┌────────────────┐
                    │ OnInitialized  │
                    │   Async        │
                    └────────┬───────┘
                             │
                             ↓
              ┌──────────────────────────────┐
              │    LoadCategory() Method     │
              │                              │
              │  IsLoading = true            │
              │  StateHasChanged()           │
              └────────────┬─────────────────┘
                           │
                           ↓
        ┌──────────────────────────────────────┐
        │   API Call: GetCategoryByIdAsync()   │
        │   GET /api/categories/{id}           │
        └────────────────┬─────────────────────┘
                         │
                    RESPONSE RECEIVED
                         │
            ┌────────────┴────────────┐
            │                         │
        SUCCESS                   FAILURE
            │                         │
            ↓                         ↓
    ┌─────────────────┐      ┌──────────────────┐
    │ Category = API  │      │ ErrorMessage =   │
    │ Response        │      │ error details    │
    │                 │      │                  │
    │ Properties:     │      │ UI shows error   │
    │ - Id            │      │ with Retry btn   │
    │ - Name          │      └──────────────────┘
    │ - Code          │
    │ - Description   │
    │ - ParentId      │
    │ - DisplayOrder  │
    │ - IsActive      │
    │ - CreatedBy     │
    │ - ModifiedBy    │
    │ - SubCategories │
    └────────┬────────┘
             │
    IsLoading = false
    StateHasChanged()
             │
             ↓
   ┌──────────────────┐
   │  UI RENDERS      │
   │ with all data    │
   └────────┬─────────┘
            │
    ┌───────┴──────────────────────────┐
    │   ALL PROPERTIES DISPLAYED       │
    │                                  │
    │  [96]  Name:      ___________    │
    │  [100] Code:      ___________    │
    │  [114] Desc:      ___________    │
    │  [115] Level:     Root (Level 1) │
    │  [143] Active:    ☑ Checkbox    │
    │  [157] Order:     [1]            │
    │  [122] SubCats:   3              │
    │  [126] Created:   admin@...      │
    │  [130] Modified:  user@...       │
    │  [164] Delete:    [Enabled/Dis]  │
    │                                  │
    └──────────────────────────────────┘
```

---

## ✅ What Each Property Does

### **Input Fields (Two-way binding)**

| Property | Binding | Purpose | On Save |
|----------|---------|---------|---------|
| Name | `@bind` | Edit category name | Sent to API |
| Description | `@bind` | Edit category notes | Sent to API |
| DisplayOrder | `@bind` | Set display order | Sent to API |
| IsActive | `@bind` | Toggle visibility | Sent to API |

### **Display Fields (Read-only)**

| Property | Display | Purpose | Usage |
|----------|---------|---------|-------|
| Code | `@value` | Show code | User reference |
| CreatedBy | `@value` | Show creator | Audit info |
| ModifiedBy | `@value` | Show modifier | Audit info |
| SubCategories | `@value` | Show count | Logic + Display |

### **Calculated Fields**

| Property | Source | Purpose | Usage |
|----------|--------|---------|-------|
| Category Level | ParentCategoryId | Show depth | Display only |
| Delete Button | SubCategories | Enable/Disable | Delete logic |

---

## 🐛 Troubleshooting - Properties Not Showing

### **Issue: Name field is empty**

**Possible causes:**
1. Category name is actually empty in database
2. API not returning the name property
3. Data binding broken

**Fix:**
- Check database: `SELECT Name FROM Categories WHERE Id = '{id}'`
- Check API response in Network tab
- Verify `@bind="Category.Name"` is correct

---

### **Issue: Subcategories count shows 0 when there should be more**

**Possible causes:**
1. SubCategories list not populated by API
2. Parent-child relationship not set up correctly
3. API not returning SubCategories collection

**Fix:**
- Check database: `SELECT * FROM Categories WHERE ParentCategoryId = '{parentId}'`
- Check API response: Look for `"subCategories": [ ... ]`
- Verify parent category ID is correct

---

### **Issue: Created By shows empty or wrong user**

**Possible causes:**
1. CreatedBy not set in database
2. API not returning CreatedBy property
3. User record deleted or modified

**Fix:**
- Check database: `SELECT CreatedBy FROM Categories WHERE Id = '{id}'`
- Check API response for CreatedBy field
- Verify user still exists in system

---

### **Issue: Delete button shows when it shouldn't**

**Possible causes:**
1. SubCategories not populated correctly
2. Count logic checking wrong property
3. API not returning SubCategories

**Fix:**
- Check if category has actual children in database
- Check API response for SubCategories array
- Verify line 164 condition: `@if (Category.SubCategories?.Count == 0)`

---

## 🧪 Complete Testing Procedure

### **Step 1: Navigate to Edit Page**
```
Go to: http://localhost:5000/inventory/categories/{a-real-category-id}/edit
```

### **Step 2: Check for Loading State**
- Should briefly show "Loading category..." with spinner
- Then form appears

### **Step 3: Verify All Fields**
```
☐ Name field shows category name
☐ Code field shows category code
☐ Description shows full text
☐ Display Order shows number
☐ Active checkbox is checked/unchecked correctly
☐ Subcategories count shows (0 or higher)
☐ Created By shows username
☐ Modified By shows username (or creator if null)
☐ Category Level shows "Root (Level 1)" or "Level 2+"
☐ Delete button is enabled/disabled correctly based on subcategories
```

### **Step 4: Test Data Binding**
```
☐ Change Name field → should update Category.Name
☐ Change Description → should update Category.Description
☐ Change Display Order → should update Category.DisplayOrder
☐ Toggle Active checkbox → should update Category.IsActive
```

### **Step 5: Test Save**
```
☐ Edit a field
☐ Click "Save Changes"
☐ Should show "Category updated successfully" notification
☐ API should be called with new values
☐ Page should remain on edit page
```

### **Step 6: Check Error Handling**
```
☐ Clear the Name field
☐ Try to save
☐ Should show warning: "Category name is required"
☐ Form should not submit
```

---

## 📊 Data Population Checklist

Before submitting, verify:

- [ ] **1. Load Success**: Category loaded without errors
- [ ] **2. Name Field**: Shows actual category name
- [ ] **3. Code Field**: Shows actual category code
- [ ] **4. Description**: Shows full description text
- [ ] **5. Display Order**: Shows numeric value
- [ ] **6. Active Status**: Checkbox reflects correct state
- [ ] **7. Subcategories**: Count is accurate
- [ ] **8. Created By**: Shows actual creator username
- [ ] **9. Modified By**: Shows modifier or creator
- [ ] **10. Delete Logic**: Button enabled/disabled correctly
- [ ] **11. Data Binding**: Editing fields updates values
- [ ] **12. Save Works**: Changes persist to database
- [ ] **13. Error Message**: Validation errors show correctly
- [ ] **14. No Console Errors**: Browser console is clean

---

## 📝 Summary

**All 10 properties are properly:**
- ✅ Loaded from API
- ✅ Stored in Category object
- ✅ Displayed in UI
- ✅ Used for logic and validation
- ✅ Sent to API on save

**If you follow the debugging steps above, you'll confirm all data is populated correctly.**
