# EditCategory Field Matrix - Complete Reference

---

## Form Structure Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      EDIT CATEGORY PAGE                      │
├─────────────────────────────────────────────────────────────┤
│ [Cancel Button]  [Reset Button]  [Save Changes Button]      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  BASIC INFORMATION SECTION                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Category Name *          │ Category Code            │   │
│  │ [________________]       │ [________________]  (RO)  │   │
│  │                          │                          │   │
│  │ Parent Category          │ Category Level           │   │
│  │ [________________]  (RO) │ [________________]  (RO)  │   │
│  │                          │                          │   │
│  │ Description                                        │   │
│  │ [_____________________________________]            │   │
│  │                                                      │   │
│  │ STATISTICS                                          │   │
│  │ Subcategories: 3  Created By: Admin  Modified: Dev │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                               │
│  DISPLAY & VISIBILITY SECTION                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ☑  Active Category                                 │   │
│  │    When inactive, this category will not appear    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                               │
│  ADDITIONAL SETTINGS SECTION                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Display Order                                      │   │
│  │ [3]                                                │   │
│  │ Controls the order in which category appears      │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                               │
│  DANGER ZONE SECTION (Conditional)                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ DELETE THIS CATEGORY                               │   │
│  │ Be careful! This action cannot be undone.          │   │
│  │ [Delete Category Button]                           │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                               │
│  [Cancel]  [Reset]  [Save Changes]                          │
│                                                               │
└─────────────────────────────────────────────────────────────┘

Legend:
[________________] = Editable input field
[________________] (RO) = Read-only field
☑ = Checkbox
* = Required field
```

---

## Field Validation Matrix

```
┌──────────────────────────────────────────────────────────────────────┐
│ FIELD ANALYSIS - API SUPPORT & FUNCTIONALITY                         │
├──────┬──────────────────┬────────────┬────────┬──────────┬──────────┤
│ No.  │ Field Name       │ Section    │ API    │ Edit     │ Status   │
├──────┼──────────────────┼────────────┼────────┼──────────┼──────────┤
│  1   │ Name             │ Basic      │ ✅ Yes │ ✅ Yes   │ ✅ Works │
│  2   │ Code             │ Basic      │ ✅ Yes │ ❌ No    │ ✅ Works │
│  3   │ Parent Category  │ Basic      │ ✅ Yes │ ❌ No    │ ✅ Works │
│  4   │ Category Level   │ Basic      │ ✅ Yes │ ❌ No    │ ✅ Works │
│  5   │ Description      │ Basic      │ ✅ Yes │ ✅ Yes   │ ✅ Works │
│  6   │ Subcategories*   │ Basic      │ ✅ Yes │ ❌ No    │ ✅ Works │
│  7   │ Created By       │ Basic      │ ✅ Yes │ ❌ No    │ ✅ Works │
│  8   │ Modified By      │ Basic      │ ✅ Yes │ ❌ No    │ ✅ Works │
│  9   │ Is Active        │ Display    │ ✅ Yes │ ✅ Yes   │ ✅ Works │
│ 10   │ Display Order    │ Settings   │ ✅ Yes │ ✅ Yes   │ ✅ Works │
│ 11   │ Display Name *   │ Display    │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 12   │ Icon *           │ Display    │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 13   │ Color *          │ Display    │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 14   │ Visible in Menu *│ Display    │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 15   │ Searchable *     │ Display    │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 16   │ Meta Title *     │ SEO        │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 17   │ Meta Desc. *     │ SEO        │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 18   │ URL Slug *       │ SEO        │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 19   │ Visibility *     │ Permissions│ ❌ No  │ ❌ No    │ ❌ Removed│
│ 20   │ Tags *           │ Settings   │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 21   │ Max Nesting *    │ Settings   │ ❌ No  │ ❌ No    │ ❌ Removed│
│ 22   │ Related Cats *   │ Settings   │ ❌ No  │ ❌ No    │ ❌ Removed│
├──────┼──────────────────┼────────────┼────────┼──────────┼──────────┤
│      │ TOTALS:          │            │  10    │   5      │          │
│      │ Supported:       │            │ 91%    │  50%     │ ✅ 10/10 │
│      │ Removed:         │            │        │          │ ❌ 12/22 │
└──────┴──────────────────┴────────────┴────────┴──────────┴──────────┘

Legend:
✅ = Supported / Works
❌ = Not Supported / Removed
* = Originally in template (unsupported)
RO = Read-only
```

---

## Data Binding Status

```
┌──────────────────────────────────────────────────────────────┐
│ FORM DATA BINDING DETAILS                                    │
├─────────────────────┬─────────────────┬──────┬──────────────┤
│ HTML Element        │ Binding         │ Type │ Updates API  │
├─────────────────────┼─────────────────┼──────┼──────────────┤
│ Category Name Input │ @bind="Name"    │ 2-way│ ✅ Yes       │
│ Category Code Input │ @value="Code"   │ 1-way│ ❌ No (RO)   │
│ Parent Category     │ No binding (RO) │ 1-way│ ❌ No (RO)   │
│ Category Level      │ Calculated      │ 1-way│ ❌ No (RO)   │
│ Description Textarea│ @bind="Desc"    │ 2-way│ ✅ Yes       │
│ Display Order Input │ @bind="Order"   │ 2-way│ ✅ Yes       │
│ Active Checkbox     │ @bind="Active"  │ 2-way│ ✅ Yes       │
└─────────────────────┴─────────────────┴──────┴──────────────┘

2-way Binding = Data flows both ways (user input → API)
1-way Binding = Data flows one way (API → display only)
```

---

## Form State Transitions

```
┌─────────────────────────────────────────────────────────────┐
│                    COMPONENT STATE MACHINE                   │
└─────────────────────────────────────────────────────────────┘

                    [INITIALIZING]
                          │
                          ↓
                    [LOADING DATA]
                      ↙    ↓    ↘
                   ✅    ❌    ⏱️
                   │     │     │
                   ↓     ↓     ↓
              [READY] [ERROR] [TIMEOUT]
                │       │       │
                └───────┼───────┘
                        │
                   [RETRY] ← ← ← ← ←
                        │
                        ↓
                  [FORM LOADED]
                        │
          ┌─────────────┼─────────────┐
          ↓             ↓             ↓
       [EDITING]   [SAVING]     [DELETING]
          │             │             │
          ├──────┐      ├──────┐      ├──────┐
          │      │      │      │      │      │
        [CANCEL][SAVE][SUBMIT][DELETE] [CONFIRM]
          │      │      │      │      │      │
          ↓      ↓      ↓      ↓      ↓      ↓
       [EXIT]   [SUCCESS]    [ERROR] [SUCCESS]
              or [ERROR]            or [ERROR]
          │       │                    │
          └───────┼────────────────────┘
                  ↓
           [BACK TO LIST]


State Properties:
- IsLoading: true/false - Data loading state
- IsSaving: true/false  - Save/Delete operation state
- ErrorMessage: ""      - Error message (empty = no error)
- Category: null/?      - Form data (null = not loaded)
```

---

## API Request/Response Schema

```
┌──────────────────────────────────────────────────────────────┐
│ SAVE OPERATION - UpdateCategoryAsync                         │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│ REQUEST (UpdateCategoryRequest):                            │
│ {                                                            │
│   "id": "550e8400-e29b-41d4-a716-446655440000",            │
│   "name": "Engine Components",                              │
│   "description": "Premium engine parts...",                 │
│   "displayOrder": 3,                                        │
│   "isActive": true                                          │
│ }                                                            │
│                                                               │
│ RESPONSE (CategoryDto):                                      │
│ {                                                            │
│   "id": "550e8400-e29b-41d4-a716-446655440000",            │
│   "name": "Engine Components",                              │
│   "description": "Premium engine parts...",                 │
│   "code": "CAT-001",                                       │
│   "parentCategoryId": null,                                │
│   "isActive": true,                                         │
│   "displayOrder": 3,                                        │
│   "createdBy": "admin@example.com",                         │
│   "modifiedBy": "user@example.com",                         │
│   "subCategories": [                                        │
│     { "id": "...", "name": "Spark Plugs", ... },          │
│     { "id": "...", "name": "Air Filters", ... }            │
│   ]                                                          │
│ }                                                            │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

## Button State Matrix

```
┌─────────────────────────────────────────────────────────────┐
│ BUTTON ENABLE/DISABLE STATES                                │
├──────────────┬────────┬────────┬────────┬────────┬────────┤
│ Scenario     │ Cancel │ Reset  │ Save   │ Delete │ Visible│
├──────────────┼────────┼────────┼────────┼────────┼────────┤
│ Loading      │ ❌     │ ❌     │ ❌     │ ❌     │ 🔒     │
│ Form Ready   │ ✅     │ ✅     │ ✅     │ ✅     │ 👁️     │
│ Saving       │ ❌     │ ❌     │ ❌     │ ❌     │ 👁️     │
│ Deleting     │ ❌     │ ❌     │ ❌     │ ❌     │ 👁️     │
│ Error State  │ ❌     │ ❌     │ ❌     │ ❌     │ 🔒     │
│ Has Children │ ✅     │ ✅     │ ✅     │ N/A    │ N/A    │
├──────────────┼────────┼────────┼────────┼────────┼────────┤

Legend:
✅ = Enabled
❌ = Disabled
🔒 = Hidden (loading/error)
👁️ = Visible
N/A = Not applicable
```

---

## Validation Flow

```
USER CLICKS SAVE
        │
        ↓
VALIDATE: Name not empty?
        │
     YES│  NO
        │   │
        ↓   ↓
     [CALL API] [SHOW WARNING]
        │       "Category name required"
        │
    API RESPONSE
        │
     SUCCESS│  ERROR
        │      │
        ↓      ↓
    [SHOW SUCCESS] [SHOW ERROR]
    "Category updated" "[Error message]"
        │               │
        ↓               ↓
   [UPDATE FORM] [KEEP FORM AS IS]
        │               │
        └───────┬───────┘
                │
            [DONE]
```

---

## Statistics Display

```
STATISTICS SECTION
┌────────────────────────────────────────────────────┐
│                                                    │
│  Subcategories:    3                              │
│  Created By:       admin@example.com              │
│  Modified By:      user@example.com               │
│                                                    │
└────────────────────────────────────────────────────┘

Data Binding:
- Subcategories:  Category.SubCategories?.Count ?? 0
- Created By:     Category.CreatedBy
- Modified By:    Category.ModifiedBy ?? Category.CreatedBy
```

---

## Delete Flow

```
USER CLICKS DELETE BUTTON
        │
        ├─── Has Children?
        │     │
        │  YES└─→ [SHOW WARNING] "Cannot delete with children"
        │
        NO
        │
        ↓
[SHOW CONFIRMATION DIALOG]
"Delete this category?"
        │
    ┌───┴────┐
    │        │
   YES      NO
    │        │
    ↓        ↓
[DELETE] [CANCEL]
    │
    ↓
API Call DeleteCategoryAsync
    │
 SUCCESS│  ERROR
    │      │
    ↓      ↓
[NOTIFY] [ERROR]
"Deleted"  "Failed: X"
    │       │
    ↓       ↓
[NAVIGATE] [STAY]
/categories /edit page
```

---

## Component Lifecycle

```
┌─────────────────────────────────────────────────────────┐
│ COMPONENT LIFECYCLE                                     │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ 1. OnInitializedAsync() [async]                        │
│    └─→ LoadCategory()                                  │
│        └─→ GetCategoryByIdAsync(id)                   │
│            └─→ Set Category property                   │
│            └─→ Set IsLoading = false                  │
│            └─→ StateHasChanged()                       │
│                                                         │
│ 2. Render() - Form displayed with data               │
│                                                         │
│ 3. User Interaction                                   │
│    ├─→ EditingForm (@bind updates)                   │
│    ├─→ ClickSave (@onclick="HandleSave")             │
│    │   └─→ UpdateCategoryAsync()                     │
│    │       └─→ Update Category                        │
│    │       └─→ Show notification                      │
│    ├─→ ClickCancel (@onclick="HandleCancel")         │
│    │   └─→ Navigate to /categories                   │
│    ├─→ ClickReset (@onclick="RetryLoadCategory")    │
│    │   └─→ LoadCategory() [reload]                   │
│    ├─→ ClickDelete (@onclick="HandleDelete")         │
│    │   └─→ ShowDialog()                              │
│    │   └─→ DeleteCategoryAsync()                     │
│    │       └─→ Navigate to /categories               │
│                                                         │
│ 4. Component Disposed                                │
│    └─→ Cleanup (automatic)                           │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## Error Message Examples

```
Error During Load:
├─ "Invalid category ID format" (bad GUID)
├─ "Category not found" (doesn't exist)
├─ "Failed to load category: Network error" (API issue)
└─ "An error occurred: Connection timeout"

Error During Save:
├─ "Category name is required" (validation)
├─ "Failed to update category: Duplicate name" (business rule)
├─ "Failed to update category: Not found" (deleted by other user)
└─ "An error occurred: Server error"

Error During Delete:
├─ "Failed to delete category: Has children" (validation)
├─ "Failed to delete category: Locked by another user"
└─ "An error occurred: Server error"
```

---

## Summary Statistics

```
┌─────────────────────────────────┐
│ COMPONENT METRICS               │
├─────────────────────────────────┤
│ Total Fields in Template:  10   │
│ Editable Fields:           5    │
│ Read-only Fields:          5    │
│ API Methods Called:        3    │
│ Event Handlers:            5    │
│ Component Properties:      4    │
│ States:                    3    │
│ Sections:                  4    │
│ Buttons:                   4    │
├─────────────────────────────────┤
│ Status: ✅ PRODUCTION READY     │
└─────────────────────────────────┘
```
