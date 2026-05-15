# Recursive Category Creation Guide

## Overview

You can now create categories at **any depth level** (up to 7 levels maximum). The system supports creating subcategories of subcategories recursively!

---

## How It Works

### Before (Limited)
```
Only allowed:
├─ Engine Parts (Root)
└─ Diesel Engines (Subcategory of Root only)

Could NOT create:
├─ Engine Parts
│  └─ Diesel Engines
│     └─ Pistons (couldn't be a subcategory of Diesel!)
```

### Now (Recursive)
```
You can now create any depth:
├─ Engine Parts (Level 1, Depth 0)
│  ├─ Diesel Engines (Level 2, Depth 1) ✅
│  │  ├─ Pistons (Level 3, Depth 2) ✅
│  │  │  └─ Standard Pistons (Level 4, Depth 3) ✅
│  │  │     └─ Size 50mm (Level 5, Depth 4) ✅
│  │  │        └─ Chinese Made (Level 6, Depth 5) ✅
│  │  │           └─ Batch 2024 (Level 7, Depth 6) ✅
│  │  │              └─ CANNOT GO DEEPER (Max Level 7)
```

---

## Step-by-Step: Create Multi-Level Categories

### Step 1: Navigate to Add Category
Go to: **Inventory → Categories → Add Category**

### Step 2: For Root Category
```
1. Category Name: "Engine Parts"
2. Category Code: "ENG-001"
3. Parent Category: "-- No Parent (Root Category) --" ← Select this
4. Description: "All engine-related components"
5. Active: ✅ Check
6. Click "Create Category"
```

**Result**: Creates root category (Level 1)

---

### Step 3: Create First Subcategory
```
1. Category Name: "Diesel Engines"
2. Category Code: "DIES-001"
3. Parent Category: "└─ Engine Parts (ENG-001) - Level 1" ← Select this now!
4. Description: "Diesel engine components"
5. Active: ✅ Check
6. Click "Create Category"
```

**Result**: Creates subcategory under "Engine Parts" (Level 2)

You'll see:
```
✓ Parent Category Information Card (blue)
✓ Parent Category: Engine Parts
✓ Breadcrumb Path: Engine Parts
✓ Current Level: 1 / 7
✓ Child Categories: 1
```

---

### Step 4: Create Second-Level Subcategory
```
1. Category Name: "Pistons"
2. Category Code: "PIST-001"
3. Parent Category: "└─   Diesel Engines (DIES-001) - Level 2" ← Select this!
4. Description: "Piston components for diesel engines"
5. Active: ✅ Check
6. Click "Create Category"
```

**Result**: Creates subcategory under "Diesel Engines" (Level 3)

You'll see:
```
✓ Parent Category Information Card (blue)
✓ Parent Category: Diesel Engines
✓ Breadcrumb Path: Engine Parts > Diesel Engines
✓ Current Level: 2 / 7
✓ Child Categories: 0
```

---

### Step 5: Continue Creating Deeper Levels
Just keep selecting the deepest child as the parent:

```
Level 4: Create "Piston Rods" with parent "Pistons"
Level 5: Create "Standard Rods" with parent "Piston Rods"
Level 6: Create "Steel Variant" with parent "Standard Rods"
Level 7: Create "Premium Grade" with parent "Steel Variant"
```

Each time, the parent info card shows:
- Breadcrumb path building up
- Current level incrementing
- Child count increasing

---

## Parent Category Dropdown

### Dropdown Layout
The dropdown now shows **ALL categories** with visual indentation:

```
-- No Parent (Root Category) --
Engine Parts (ENG-001) - Level 1
└─   Diesel Engines (DIES-001) - Level 2
└─     Pistons (PIST-001) - Level 3
└─       Piston Rods (PRODS-001) - Level 4
└─         Standard Rods (STDRD-001) - Level 5
└─           Steel Variant (STEEL-001) - Level 6
Electrical (ELE-001) - Level 1
└─   Battery Systems (BATT-001) - Level 2
```

**Key Features**:
- ✅ All categories visible (not just root)
- ✅ Indented display shows hierarchy
- ✅ Shows depth indicator (└─)
- ✅ Shows level number (Level 1, 2, 3...)
- ✅ Shows category code in parentheses

---

## Depth Limits

### Maximum Depth: 7 Levels

```
Level 1 (Depth 0): Engine Parts
Level 2 (Depth 1): Diesel Engines
Level 3 (Depth 2): Pistons
Level 4 (Depth 3): Piston Rods
Level 5 (Depth 4): Standard Rods
Level 6 (Depth 5): Steel Variant
Level 7 (Depth 6): Premium Grade ← LAST ALLOWED LEVEL

Level 8 (Depth 7): CANNOT CREATE ❌
```

### Validation Messages

**At Max Depth (Level 7)**:
```
Parent Category Information (Red Alert)
⚠️ Cannot Create Subcategory
This parent category has reached the maximum depth level (7)
```
- Button disabled? No, you'll get error message
- Error: "Cannot create subcategory! Parent category is at level 7. Maximum allowed level is 7."

**Approaching Max (Level 6)**:
```
Parent Category Information (Yellow Alert)
⚠️ Approaching Maximum Depth
New category will be at level 7 / 7
```
- Still allowed to create
- But warns you this is the last level

---

## Parent Category Information Card

When you select a parent category, a blue info card appears showing:

```
Parent Category Information
├─ Parent Category: Diesel Engines
├─ Breadcrumb Path: Engine Parts > Diesel Engines
├─ Current Level: 2 / 7
├─ Child Categories: 3
└─ Warnings (if applicable):
   ├─ ⚠️ If at max depth: CANNOT CREATE
   └─ ⚠️ If approaching max: APPROACHING MAXIMUM
```

### Understanding the Info
- **Parent Category**: The name of selected parent
- **Breadcrumb Path**: Full path from root to parent
- **Current Level**: Parent's level in hierarchy (1-7)
- **Child Categories**: How many children parent already has

---

## Breadcrumb Paths

Your new categories will have automatically generated breadcrumb paths:

```
Root category:
  BreadcrumbPath = "Engine Parts"

First subcategory:
  BreadcrumbPath = "Engine Parts > Diesel Engines"

Second subcategory:
  BreadcrumbPath = "Engine Parts > Diesel Engines > Pistons"

Third subcategory:
  BreadcrumbPath = "Engine Parts > Diesel Engines > Pistons > Piston Rods"
```

Used for:
- Navigation breadcrumb trails in UI
- Category path display
- Search results
- API responses

---

## Common Workflow Examples

### Example 1: Create Simple 2-Level Hierarchy
```
Goal: Create "Engine Parts" → "Diesel Engines"

1. Create "Engine Parts" (no parent)
   ├─ Code: ENG-001
   └─ Level: 1 / 7

2. Create "Diesel Engines" (parent: Engine Parts)
   ├─ Code: DIES-001
   ├─ Parent Info shows: "Level 1 / 7"
   └─ Level: 2 / 7
```

### Example 2: Create Deep 5-Level Hierarchy
```
Goal: Engine Parts → Diesel → Pistons → Rods → Standard

1. Create "Engine Parts" (no parent, Level 1)
2. Create "Diesel Engines" (parent: Engine Parts, Level 2)
3. Create "Pistons" (parent: Diesel Engines, Level 3)
4. Create "Piston Rods" (parent: Pistons, Level 4)
5. Create "Standard Rods" (parent: Piston Rods, Level 5)

Final breadcrumb: "Engine Parts > Diesel Engines > Pistons > Piston Rods > Standard Rods"
```

### Example 3: Multiple Categories at Same Level
```
Engine Parts (Level 1)
├─ Diesel Engines (Level 2)
│  ├─ Pistons (Level 3)
│  └─ Valves (Level 3) ← Create this
│
└─ Petrol Engines (Level 2) ← Create this

1. First create Diesel Engines
2. Then create Pistons (under Diesel Engines)
3. Then create Valves (under Diesel Engines) ← Same parent as Pistons
4. Create Petrol Engines (under Engine Parts) ← Same parent as Diesel
```

---

## Code Changes Made

### AddCategory.razor Updates

**1. Load ALL Categories (Not Just Root)**
```csharp
// OLD: Only loaded top-level
var response = await CategoryService.GetTopLevelCategoriesAsync();

// NEW: Load all categories
var response = await CategoryService.GetAllCategoriesAsync();
```

**2. Sort by Hierarchy**
```csharp
// NEW method: SortCategoriesByHierarchy()
// Sorts all categories by depth level for proper dropdown display
// Shows root first, then children, then grandchildren, etc.
```

**3. Recursive Helper**
```csharp
// NEW method: AddCategoryAndChildren()
// Recursively adds category and all its children
// Enables any-depth hierarchy creation
```

**4. Dropdown Shows Depth**
```html
<!-- NEW: Show indentation and depth in dropdown -->
└─ Diesel Engines (DIES-001) - Level 2
└─   Pistons (PIST-001) - Level 3
└─     Piston Rods (PRODS-001) - Level 4
```

**5. Depth Validation**
```csharp
// NEW: Check before creating
if (parentCategory.DepthLevel >= MaxCategoryDepth - 1)
{
    ErrorMessage = $"Cannot create subcategory! Max depth reached.";
    return;
}
```

**6. Parent Info Card**
```html
<!-- NEW: Show parent info when selected -->
Parent Category Information
├─ Breadcrumb Path: Engine Parts > Diesel Engines
├─ Current Level: 2 / 7
├─ Child Categories: 3
└─ Warnings (if at max depth)
```

---

## Testing the Feature

### Test Case 1: Create 3-Level Hierarchy
```
✓ Create "Engines" (root)
✓ Create "Diesel" (parent: Engines)
✓ Create "Pistons" (parent: Diesel)
✓ Verify breadcrumb: "Engines > Diesel > Pistons"
✓ Verify depth: Level 3 / 7
```

### Test Case 2: Depth Limit Validation
```
✓ Create categories up to Level 6
✓ Try to create Level 8 (should fail)
✓ Verify error: "Cannot create subcategory! Parent category is at level 7"
```

### Test Case 3: Multiple Children at Same Level
```
✓ Create "Engines" (root)
✓ Create "Diesel" (parent: Engines)
✓ Create "Petrol" (parent: Engines) ← Same parent as Diesel
✓ Create "Pistons" (parent: Diesel)
✓ Create "Rods" (parent: Diesel) ← Same parent as Pistons
✓ Verify both siblings show under parent
```

### Test Case 4: Dropdown Shows All Levels
```
✓ Create 4-level hierarchy
✓ Open Add Category page
✓ Check dropdown shows all 4 levels
✓ Verify indentation is correct
✓ Verify depth indicators are shown
```

---

## FAQ

### Q: Can I create categories at any level?
**A**: Yes! You can create a subcategory under any existing category, up to level 7.

### Q: What's the maximum depth?
**A**: 7 levels maximum. If parent is at level 7, you cannot create children.

### Q: Can I edit the parent later?
**A**: Currently, parent category cannot be changed after creation. This prevents data corruption. Plan to implement in future version.

### Q: How are breadcrumbs generated?
**A**: Automatically! Based on the parent's breadcrumb path + category name, e.g., "Parent Path > New Category".

### Q: Will the dropdown be too long?
**A**: With indentation and visual hierarchy, it's easier to navigate. Consider adding search if it gets very large.

### Q: Can I delete a category with children?
**A**: No, you must delete all children first. The system prevents orphaning categories.

---

## Before and After

### BEFORE (Limited to 2 Levels)
```
❌ Only Root (Level 1)
❌ Only Subcategory (Level 2)
❌ Cannot go deeper

Dropdown showed:
-- No Parent (Root Category) --
Engine Parts
Electrical
Suspension
```

### AFTER (Supports Up to 7 Levels)
```
✅ Root (Level 1)
✅ Subcategory (Level 2)
✅ Sub-subcategory (Level 3)
✅ And deeper... up to Level 7

Dropdown shows:
-- No Parent (Root Category) --
Engine Parts
└─   Diesel Engines
└─     Pistons
└─       Piston Rods
└─         Standard Rods
Electrical
└─   Battery Systems
Suspension
```

---

## Key Improvements

| Feature | Before | After |
|---------|--------|-------|
| Max Depth | 2 levels | 7 levels |
| Category Selection | Root only | All categories |
| Hierarchy Display | Flat list | Indented tree |
| Depth Info | Not shown | Shows Level X/7 |
| Breadcrumb | Not shown | Shows full path |
| Validation | Min | Full depth checking |
| Parent Info | None | Detailed info card |

---

## Summary

You now have a **fully recursive category creation system** that:
- ✅ Supports creating categories at any depth (up to 7 levels)
- ✅ Shows full hierarchy in dropdown with indentation
- ✅ Validates depth limits before creation
- ✅ Displays parent information and breadcrumb paths
- ✅ Prevents exceeding maximum depth
- ✅ Warns when approaching maximum depth

**Start creating your multi-level categories today!** 🚀
