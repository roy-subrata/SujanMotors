# Quick Sale Feature - Complete Testing Guide

## ✅ Implementation Status: COMPLETE

All backend APIs and frontend components are implemented and ready for testing.

---

## 🔧 Pre-Test Setup

### 1. Restart the Backend API
```bash
# Stop the API in Visual Studio
# Then restart it, or run:
dotnet run --project src/AutoPartShop.Api/AutoPartShop.Api.csproj
```

### 2. Verify API is Running
- Open browser: http://localhost:5292/swagger
- Check that the following endpoints exist:
  - GET `/api/customer/recent`
  - GET `/api/customer/search-by-phone`
  - GET `/api/code-generate/invoice`
  - POST `/api/salesorder/quick-sale`

### 3. Start the Angular App
```bash
cd src/AutoPartShop.WebApp
npm start
# Opens at http://localhost:4200
```

### 4. Hard Refresh the Browser
- Press `Ctrl + Shift + R` (or `Cmd + Shift + R` on Mac)
- This clears the Angular cache

---

## 🧪 Test Cases

### Test 1: Invoice Number Generation ✅

**Objective:** Verify that invoice numbers are generated automatically when the page loads

**Steps:**
1. Navigate to http://localhost:4200/sales/quick-sale
2. Look at the page header

**Expected Result:**
- Header shows: "Invoice: INV#######" (e.g., "Invoice: INV0001234")
- NO 404 error in browser console
- NO "[object Object]" displayed

**If Failed:**
- Check browser console for errors
- Verify API endpoint: GET http://localhost:5292/api/code-generate/invoice
- Check that API returns: `{ "invoiceNumber": "INV0001234" }`

---

### Test 2: Customer Selection (No "[object Object]") ✅

**Objective:** Verify customer autocomplete displays names correctly

**Steps:**
1. Click on the "Select Customer" dropdown
2. Select any customer from the list
3. Observe what appears in the input field

**Expected Result:**
- Customer's full name appears (e.g., "John Doe")
- NO "[object Object]" text

**If Failed:**
- Check that `optionLabel="fullName"` is set in the HTML
- Verify customer object has `fullName` property
- Check browser console for errors

---

### Test 3: Customer Search by Phone ✅

**Objective:** Verify phone number auto-fill works

**Steps:**
1. Enter a customer's phone number in the "Phone Number" field
2. Wait 500ms (debounce delay)
3. Observe the "Select Customer" field

**Expected Result:**
- Customer is automatically selected and populated
- Green toast message: "Customer Found: John Doe"
- Customer name appears correctly (not "[object Object]")

**If Failed:**
- Check API: GET http://localhost:5292/api/customer/search-by-phone?phone=1234567890
- Verify phone number exists in database
- Check network tab for 404 or 500 errors

---

### Test 4: Part Selection (No "[object Object]") ✅

**Objective:** Verify part autocomplete displays names correctly

**Steps:**
1. Click on the part search autocomplete
2. Type a part name or SKU
3. Select a part from the dropdown
4. Observe what appears in the input field

**Expected Result:**
- Part name appears (e.g., "Brake Pad Set")
- Part number and SKU shown in dropdown items
- NO "[object Object]" text

**If Failed:**
- Check that `optionLabel="name"` is set in the HTML
- Verify part object has `name` property
- Check that parts are loaded: GET http://localhost:5292/api/parts/active

---

### Test 5: Add Part to Cart ✅

**Objective:** Verify parts can be added to the cart

**Steps:**
1. Search and select a part
2. Click "Add" button
3. Observe the cart table

**Expected Result:**
- Part appears in cart with correct details (name, SKU, quantity, price)
- Part input field is cleared
- Subtotal updates
- Grand Total updates (including VAT)

**If Failed:**
- Check `addPartToCart()` method in TypeScript
- Verify `selectedPartModel` is being cleared
- Check cart calculations

---

### Test 6: Technician Selection (No "[object Object]") ✅

**Objective:** Verify technician autocomplete displays names correctly

**Steps:**
1. Scroll to the "Technician & Payment" section
2. Click on the "Select Technician" dropdown
3. Select a technician
4. Observe what appears in the input field

**Expected Result:**
- Technician name appears (e.g., "Mike Johnson")
- NO "[object Object]" text

**If Failed:**
- Check that `optionLabel="name"` is set in the HTML
- Verify technician object has `name` property
- Check that technicians are loaded: GET http://localhost:5292/api/technician

---

### Test 7: Add Payment ✅

**Objective:** Verify payments can be added

**Steps:**
1. Add at least one part to cart (so there's a total)
2. Click "Add" button in the Payments section
3. Select payment method (Cash/Card/etc.)
4. Enter amount
5. Observe the totals

**Expected Result:**
- Payment row appears
- Paid Amount updates
- Due Amount updates (Grand Total - Paid Amount)
- Due amount turns red if > 0

**If Failed:**
- Check `addPayment()` method
- Verify payment calculations
- Check that payment totals are reactive

---

### Test 8: Complete Quick Sale (Walk-in Customer) ✅

**Objective:** Create a complete sale without a registered customer

**Steps:**
1. **Customer Section:**
   - Phone: Enter "0000000000" (walk-in)
   - Leave "Select Customer" empty

2. **Add Parts:**
   - Search and add 2-3 parts to cart
   - Verify totals calculate correctly

3. **Payment:**
   - Add payment (Cash)
   - Enter full amount to pay (equals Grand Total)

4. **Complete:**
   - Click "Complete Sale" button

**Expected Result:**
- Green toast: "Sale completed successfully!"
- New invoice number generated
- Cart cleared
- Form reset
- Confirmation dialog: "Print Invoice?"

**API Call to Check:**
```http
POST http://localhost:5292/api/salesorder/quick-sale
Content-Type: application/json

{
  "customerName": "Walk-in Customer",
  "customerPhone": "0000000000",
  "items": [...],
  "payments": [...],
  "subtotal": 100.00,
  "vatAmount": 15.00,
  "grandTotal": 115.00,
  ...
}
```

**If Failed:**
- Check browser Network tab for API response
- Look for validation errors (400)
- Check stock availability (parts must have stock)
- Verify database has at least one warehouse

---

### Test 9: Complete Quick Sale (Registered Customer) ✅

**Objective:** Create a sale with a registered customer

**Steps:**
1. **Customer Section:**
   - Select a customer from dropdown OR
   - Enter their phone number (auto-fill)

2. **Add Parts:**
   - Add parts to cart

3. **Technician (Optional):**
   - Select a technician
   - Set payment responsibility

4. **Payment:**
   - Add payment
   - Can add multiple payments (Cash + Card)

5. **Complete:**
   - Click "Complete Sale"

**Expected Result:**
- Sale created with customer linked
- Customer balance updated (if due amount > 0)
- Invoice includes customer details
- Success message with invoice number

**If Failed:**
- Check customer credit limit
- Verify customer exists in database
- Check that customerId is sent in request

---

### Test 10: Stock Validation ✅

**Objective:** Verify system prevents overselling

**Steps:**
1. Find a part with limited stock (e.g., only 5 units)
2. Try to add 100 units to cart
3. Click "Complete Sale"

**Expected Result:**
- Error message: "Insufficient stock for [Part Name]"
- Sale is NOT created
- Stock levels unchanged

**If Failed:**
- Check stock validation in backend SalesOrderController
- Verify StockLevel repository queries
- Check warehouse stock levels

---

### Test 11: VAT Calculation ✅

**Objective:** Verify VAT is calculated correctly

**Steps:**
1. Add a part with price = 100.00
2. Observe the totals

**Expected:**
- Subtotal: 100.00
- VAT (15%): 15.00
- Grand Total: 115.00

**Steps to Disable VAT:**
1. Toggle VAT checkbox (if available)
2. Observe totals update

**Expected:**
- Subtotal: 100.00
- VAT: 0.00
- Grand Total: 100.00

---

### Test 12: Discount Application ✅

**Objective:** Verify line-item discounts work

**Steps:**
1. Add a part to cart (Price: 100.00)
2. In the cart table, change Disc % to 10%
3. Observe totals

**Expected:**
- Line Total: 90.00 (100 - 10%)
- Subtotal: 90.00
- VAT: 13.50 (15% of 90)
- Grand Total: 103.50

---

### Test 13: Multiple Payments ✅

**Objective:** Verify multiple payment methods work

**Steps:**
1. Add parts to cart (Total: 200.00)
2. Add Payment 1: Cash = 100.00
3. Add Payment 2: Card = 100.00
4. Complete sale

**Expected Result:**
- Paid Amount: 200.00
- Due Amount: 0.00
- Both payments recorded in invoice
- Sale status: COMPLETED

---

### Test 14: Partial Payment ✅

**Objective:** Verify partial payments work

**Steps:**
1. Add parts to cart (Total: 200.00)
2. Add Payment: Cash = 150.00
3. Complete sale

**Expected Result:**
- Paid Amount: 150.00
- Due Amount: 50.00
- Sale status: COMPLETED
- Customer balance increases by 50.00

---

### Test 15: Draft Save/Load ✅

**Objective:** Verify draft functionality works

**Steps:**
1. Add parts to cart
2. Add customer
3. Click "Save Draft"
4. Refresh page (F5)

**Expected Result:**
- Draft is loaded automatically
- Cart items restored
- Customer restored
- Toast message: "Draft loaded"

**Clear Draft:**
1. Click "Clear" button
2. Confirm
3. All data cleared

---

## 🐛 Common Issues & Solutions

### Issue 1: "[object Object]" Still Appearing

**Solution:**
1. Check HTML uses `optionLabel` (NOT `field`)
2. Hard refresh browser (Ctrl+Shift+R)
3. Clear browser cache
4. Check that data objects have the specified property

### Issue 2: 404 Error for Invoice Generation

**Solution:**
1. Verify API route is `/api/code-generate/invoice` (NOT `/api/generate-code`)
2. Restart the API in Visual Studio
3. Check Swagger docs for endpoint
4. Verify CodeGenerateController.cs has correct route attribute

### Issue 3: Stock Not Updating

**Solution:**
1. Check database has warehouses
2. Verify parts have stock levels
3. Check StockLevel table has records
4. Review SalesOrderController.cs stock update logic

### Issue 4: Customer Credit Limit Error

**Solution:**
1. Check customer's creditLimit and currentBalance
2. Verify due amount doesn't exceed available credit
3. Either increase credit limit or reduce due amount

### Issue 5: Build Errors Due to File Locking

**Solution:**
1. Stop the API in Visual Studio
2. Close Visual Studio
3. Run `dotnet clean` then `dotnet build`
4. Restart Visual Studio

---

## 📊 Database Verification Queries

### Check if data exists:

```sql
-- Check customers
SELECT COUNT(*) FROM Customers WHERE Isdeleted = 0;

-- Check parts
SELECT COUNT(*) FROM Parts WHERE IsActive = 1;

-- Check stock levels
SELECT p.Name, sl.QuantityOnHand, sl.QuantityAvailable, w.Name as Warehouse
FROM StockLevels sl
JOIN Parts p ON sl.PartId = p.Id
JOIN Warehouses w ON sl.WarehouseId = w.Id
WHERE sl.IsActive = 1;

-- Check technicians
SELECT * FROM Technicians WHERE IsActive = 1;

-- Check last 5 sales orders
SELECT TOP 5 * FROM SalesOrders ORDER BY CreatedDate DESC;

-- Check last 5 invoices
SELECT TOP 5 * FROM Invoices ORDER BY CreatedDate DESC;
```

---

## ✅ Success Criteria

All tests pass if:

1. ✅ Invoice number generates on page load
2. ✅ Customer selection shows full names (no "[object Object]")
3. ✅ Phone search auto-fills customer
4. ✅ Part selection shows part names (no "[object Object]")
5. ✅ Parts can be added to cart
6. ✅ Technician selection shows names (no "[object Object]")
7. ✅ Payments can be added
8. ✅ Walk-in customer sales work
9. ✅ Registered customer sales work
10. ✅ Stock validation prevents overselling
11. ✅ VAT calculation is correct
12. ✅ Discounts apply correctly
13. ✅ Multiple payments work
14. ✅ Partial payments work
15. ✅ Draft save/load works

---

## 📞 API Endpoints Summary

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/customer/recent?limit=50` | Load recent customers |
| GET | `/api/customer/search-by-phone?phone={phone}` | Search by phone |
| GET | `/api/parts/active` | Load active parts |
| GET | `/api/technician` | Load technicians |
| GET | `/api/code-generate/invoice` | Generate invoice number |
| POST | `/api/salesorder/quick-sale` | Create complete sale |

---

## 🎯 Performance Expectations

- Page load: < 2 seconds
- Invoice generation: < 500ms
- Customer search: < 300ms (with debounce)
- Part search: < 200ms (client-side filter)
- Complete sale: < 1 second

---

## 📝 Test Log Template

Use this template to track your testing:

```
Date: _____________
Tester: _____________

Test 1 - Invoice Generation: ☐ Pass ☐ Fail
Test 2 - Customer Selection: ☐ Pass ☐ Fail
Test 3 - Phone Search: ☐ Pass ☐ Fail
Test 4 - Part Selection: ☐ Pass ☐ Fail
Test 5 - Add to Cart: ☐ Pass ☐ Fail
Test 6 - Technician Selection: ☐ Pass ☐ Fail
Test 7 - Add Payment: ☐ Pass ☐ Fail
Test 8 - Walk-in Sale: ☐ Pass ☐ Fail
Test 9 - Registered Customer Sale: ☐ Pass ☐ Fail
Test 10 - Stock Validation: ☐ Pass ☐ Fail
Test 11 - VAT Calculation: ☐ Pass ☐ Fail
Test 12 - Discount: ☐ Pass ☐ Fail
Test 13 - Multiple Payments: ☐ Pass ☐ Fail
Test 14 - Partial Payment: ☐ Pass ☐ Fail
Test 15 - Draft Save/Load: ☐ Pass ☐ Fail

Overall Status: ☐ All Pass ☐ Some Failed
Notes: ________________________________
```

---

**Last Updated:** 2025-12-10
**Version:** 1.0.0
**Status:** ✅ Ready for Testing
