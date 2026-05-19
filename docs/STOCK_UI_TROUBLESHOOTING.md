# Stock UI Troubleshooting Guide

## Issue: Price History and Stock Data Not Showing in UI

### ✅ Verified Working Components

1. **Backend API** - All endpoints return data correctly:
   - `GET /api/stocklot/warehouse/{partId}/{warehouseId}` ✓
   - `GET /api/stocklot/price-history/{partId}` ✓
   - `GET /api/parts` ✓
   - `GET /api/warehouses` ✓

2. **Test Data Available**:
   - Air Filter (ID: 97f92b3f-8672-4871-a413-d949e4397705)
   - Warehouse: Malawoori (ID: 48ac7a39-fe3b-4dab-b85a-9e148443d4cf)
   - Stock Lot: LOT1799579 (10 units @ 450.00 INR)

3. **Frontend Components** - All properly implemented:
   - `StockLotsByWarehouseComponent` ✓
   - `StockPriceHistoryComponent` ✓
   - Services configured correctly ✓

---

## 🔍 Step-by-Step Troubleshooting

### Step 1: Check Browser Console

1. Open the Angular app: `http://localhost:4200`
2. Navigate to: **Inventory > Stock Management**
3. Click on tab: **Stock by Lot (Price Tracking)**
4. Open Browser DevTools (F12)
5. Go to **Console** tab

**Look for errors like:**
- ❌ CORS errors
- ❌ 404 Not Found
- ❌ TypeScript compilation errors
- ❌ Module not found errors

---

### Step 2: Check Network Tab

1. In DevTools, go to **Network** tab
2. Click on **Stock by Lot** tab
3. Select a Part and Warehouse
4. Click **Load Lots** button

**Expected Network Calls:**
```
GET http://localhost:5292/api/parts          → Should return 200 OK
GET http://localhost:5292/api/warehouses     → Should return 200 OK
GET http://localhost:5292/api/stocklot/warehouse/{partId}/{warehouseId}  → Should return 200 OK
```

**If you see:**
- ❌ 404: API route might be wrong
- ❌ CORS error: Backend CORS not configured
- ❌ Pending forever: API not running
- ❌ Empty response `[]`: No data in database

---

### Step 3: Verify Angular App is Running

**Check Angular CLI output:**
```bash
cd src/AutoPartShop.WebApp
npm start
```

**Expected output:**
```
** Angular Live Development Server is listening on localhost:4200 **
✔ Browser application bundle generation complete.
```

**If not running, start it:**
```bash
cd d:\AI\SujanMotors\src\AutoPartShop.WebApp
npm install
npm start
```

---

### Step 4: Verify API is Running

**Test API directly:**
```bash
curl http://localhost:5292/api/parts
```

**Should return JSON array of parts**

**If not running:**
1. Open Visual Studio 2022
2. Open `AutoPartShop.sln`
3. Set `AutoPartShop.Api` as startup project
4. Press F5 to run

---

### Step 5: Check Component Registration

Verify the tabs are showing:

1. Navigate to: `http://localhost:4200/inventory/stock`
2. You should see 5 tabs:
   - All Stock Levels
   - Low Stock Alerts
   - Movement History
   - **Stock by Lot**
   - **Price History**

**If tabs are not showing:**
- Angular app might not be compiled
- Component imports might be incorrect

---

### Step 6: Test with Known Data

Use these IDs to test manually:

**Part ID (Air Filter):** `97f92b3f-8672-4871-a413-d949e4397705`
**Warehouse ID (Malawoori):** `48ac7a39-fe3b-4dab-b85a-9e148443d4cf`

**Direct API Test:**
```bash
curl "http://localhost:5292/api/stocklot/warehouse/97f92b3f-8672-4871-a413-d949e4397705/48ac7a39-fe3b-4dab-b85a-9e148443d4cf"
```

**Expected Response:**
```json
[
  {
    "lotNumber": "LOT1799579",
    "partName": "Air Filter",
    "warehouseName": "Malawoori",
    "quantityReceived": 10,
    "quantityAvailable": 10,
    "costPrice": 450.0,
    "totalCost": 4500.0
  }
]
```

---

## 🔧 Common Fixes

### Fix 1: CORS Error

**Symptom:** Browser console shows CORS policy error

**Solution:** Add CORS to `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");
```

---

### Fix 2: Angular Not Updated

**Symptom:** Old code still running

**Solution:** Rebuild Angular app:
```bash
cd src/AutoPartShop.WebApp
npm run build
# OR restart dev server
npm start
```

---

### Fix 3: Dropdowns Empty

**Symptom:** Part/Warehouse dropdowns show no options

**Solution:** Check API responses:
```bash
curl http://localhost:5292/api/parts
curl http://localhost:5292/api/warehouses
```

If empty, add test data via Swagger UI.

---

### Fix 4: No Stock Lots in Database

**Symptom:** API returns `[]` (empty array)

**Solution:** Create stock lots via Goods Receipt workflow:
1. Create Purchase Order
2. Create Goods Receipt
3. **Verify** Goods Receipt
4. **Accept** Goods Receipt (this creates Stock Lots!)

---

## 📊 Manual UI Test

1. **Navigate to Stock Page:**
   ```
   http://localhost:4200/inventory/stock
   ```

2. **Click "Stock by Lot" Tab**

3. **Select Part:** Air Filter
   **Select Warehouse:** Malawoori

4. **Click "Load Lots"**

5. **Expected Result:**
   - Table shows 1 row
   - Lot Number: LOT1799579
   - Quantity: 10 units
   - Unit Cost: ₹450.00
   - Total Cost: ₹4,500.00

6. **Click "Price History" Tab**

7. **Select Part:** Air Filter

8. **Click "Load History"**

9. **Expected Result:**
   - Statistics show:
     - Latest Price: ₹450.00
     - Average Price: ₹450.00
     - Min Price: ₹450.00
     - Max Price: ₹450.00
   - Table shows 1 purchase lot

---

## 🐛 Debugging Checklist

- [ ] Backend API running on port 5292?
- [ ] Angular app running on port 4200?
- [ ] Browser console has no errors?
- [ ] Network tab shows successful API calls (200 OK)?
- [ ] Parts and Warehouses dropdowns populated?
- [ ] At least 1 Stock Lot exists in database?
- [ ] CORS configured correctly?
- [ ] Angular app rebuilt after code changes?

---

## 📞 Quick Test Commands

```bash
# Test API is running
curl http://localhost:5292/api/parts | head -c 100

# Test stock lots exist
curl "http://localhost:5292/api/stocklot/warehouse/97f92b3f-8672-4871-a413-d949e4397705/48ac7a39-fe3b-4dab-b85a-9e148443d4cf"

# Test price history
curl "http://localhost:5292/api/stocklot/price-history/97f92b3f-8672-4871-a413-d949e4397705"
```

---

## 🎯 If Still Not Working

**Provide this information:**
1. Screenshot of browser console (F12 → Console tab)
2. Screenshot of Network tab showing API calls
3. Output of: `curl http://localhost:5292/api/parts`
4. Screenshot of the Stock page showing the tabs
5. Any error messages from Angular CLI terminal

---

**Last Updated:** 2025-12-10
**Status:** Backend ✅ Working | Frontend ❓ Needs browser debugging
