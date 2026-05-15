# Stock UI Testing Guide

## Quick Test - Are the tabs working?

### 1. Backend Verification ✅
```bash
# Test All Stock API
curl http://localhost:5292/api/stock/levels
# Should return 4 items

# Test Low Stock API
curl http://localhost:5292/api/stock/levels/low-stock
# Should return 2 items
```

### 2. Frontend Test

**Open**: http://localhost:4200/inventory/stock

**Expected Results**:

| Tab | Count | Items |
|-----|-------|-------|
| **All Stock Levels** | 4 items | Fan Belt, PRESSURE PLATE, Mobile Filter, Air Filter |
| **Low Stock Alerts** | 2 items | PRESSURE PLATE (Qty: 1), Looking Glass (Qty: 10) |

### 3. Browser Console Check

**Press F12 → Console tab**

When you click **Low Stock Alerts** tab, you should see:
```
🔄 Tab changed: {index: 1}
📊 Active Tab: 1
📦 All Stock Count: 4 items
⚠️  Low Stock Count: 2 items
✅ Now showing LOW STOCK: 2 items
```

### 4. If Still Showing Same Data

**Problem**: Angular not picking up code changes

**Solution**:
1. Stop Angular dev server (Ctrl+C)
2. Restart: `npm start`
3. Wait for "✔ Compiled successfully"
4. Hard refresh browser: Ctrl+Shift+R

### 5. Visual Confirmation

**All Stock Tab** should show:
- ✅ 4 rows in table
- Badge shows: "4"

**Low Stock Tab** should show:
- ✅ 2 rows in table (different items!)
- Badge shows: "2"
- Items have ⚠️ warning indicators

---

## If Low Stock is Empty (0 items)

This means reorder levels weren't set. Run:

```bash
# Set PRESSURE PLATE reorder level
curl -X PUT http://localhost:5292/api/stock/levels/152c70bd-805f-4b00-b3db-54c6210358c5 \
  -H "Content-Type: application/json" \
  -d '{"reorderLevel": 5, "reorderQuantity": 10}'

# Set Looking Glass reorder level
curl -X PUT http://localhost:5292/api/stock/levels/934bc574-d1ff-44fa-9478-8888bf2bf68c \
  -H "Content-Type: application/json" \
  -d '{"reorderLevel": 15, "reorderQuantity": 20}'
```

Then refresh the page.

---

## Current Status

### Backend ✅ Working
- All Stock API: Returns 4 items
- Low Stock API: Returns 2 items
- Different data confirmed

### Frontend ❓ Needs Restart
- Code updated with console logging
- Tab switching logic fixed
- **Requires Angular dev server restart** to apply changes

### What Changed
1. Fixed `onTabChange()` method to properly handle tabs 0 and 1
2. Added console logging for debugging
3. Set reorder levels for 2 items to trigger low stock alerts

---

**Next Steps**:
1. Restart Angular dev server
2. Open browser console (F12)
3. Switch between tabs
4. Verify console logs and table data
