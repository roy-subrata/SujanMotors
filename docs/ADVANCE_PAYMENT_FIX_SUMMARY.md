# Advance Payment Accounting Fix - Summary

## Problem Statement
The system was **double-counting** advance payments:
- When customer paid 4000 as advance → TotalPaid = 4000 ✓
- When customer used 1000 from advance → TotalPaid = 5000 ✗ (WRONG!)
- **Issue**: No new money was received, but TotalPaid increased

## Root Cause
The `TotalPaid` calculation was summing **ALL completed payments**, including:
1. Original ADVANCE payments (new money received) ✓
2. REGULAR payments from new cash/card (new money received) ✓
3. Payments created from advance (NOT new money - just allocation) ✗

When applying advance credit, the system created a new CustomerPayment record with:
- `PaymentType = REGULAR`
- `SourceAdvancePaymentId = {original advance ID}`
- `Status = COMPLETED`

This payment was being counted in TotalPaid, causing double-counting.

---

## Solution Applied

### 1. Customer Entity Fix
**File**: `src/AutoPartShop.Domain/Entities/Customer.cs`

**Before**:
```csharp
public decimal TotalPaid =>
    CustomerPayments?
        .Where(p => p.Status == "COMPLETED")
        .Sum(p => p.Amount) ?? 0;
```

**After**:
```csharp
// TotalPaid = ONLY new money received (excludes payments created from advance)
public decimal TotalPaid =>
    CustomerPayments?
        .Where(p => p.Status == "COMPLETED" &&
                   (p.PaymentType == CustomerPaymentType.ADVANCE ||  // Original advances
                    p.SourceAdvancePaymentId == null))                // New regular payments
        .Sum(p => p.Amount) ?? 0;
```

### 2. Supplier Payment Summary Fix
**File**: `src/AutoPartShop.Api/Services/SupplierPaymentSummaryService.cs`

Applied same logic to `CalculateTotalPaid()` method.

### 3. Invoice AmountPaid (NO CHANGE)
**File**: `src/AutoPartShop.Domain/Entities/Invoice.cs`

Invoice.AmountPaid **correctly** counts ALL payments (including advance-sourced) because:
- The invoice doesn't care WHERE money came from
- It only needs to know if it's been covered/paid
- This determines invoice status (ISSUED → PAID)

---

## Correct Accounting Behavior

### Scenario 1: Customer Makes Advance Payment
```
Action: Customer pays 4000 BDT as advance
Result:
  - Advance Balance: +4000 BDT
  - Total Paid: +4000 BDT (new money received)
  - Customer Balance: -4000 BDT (customer has credit with us)
```

### Scenario 2: Customer Uses Advance in Quick Sale
```
Action: Sale of 1000 BDT, customer uses advance balance
Result:
  - Advance Balance: -1000 BDT → Now 3000 BDT
  - Total Paid: NO CHANGE (still 4000 BDT - no new money)
  - Customer Balance: +1000 (invoice) -1000 (advance used) = 0
  - Invoice Status: PAID ✓
  - Outstanding Amount: 0 BDT ✓
```

### Scenario 3: Customer Makes New Cash Payment
```
Action: Sale of 2000 BDT, customer pays 500 BDT cash, rest on credit
Result:
  - Advance Balance: NO CHANGE (3000 BDT)
  - Total Paid: +500 BDT → Now 4500 BDT (new money received)
  - Customer Balance: +2000 (invoice) -500 (cash) = +1500 BDT due
  - Invoice Status: PARTIALLY_PAID
  - Outstanding Amount: 1500 BDT
```

### Scenario 4: Mixed Payment (Advance + Cash)
```
Action: Sale of 2000 BDT, uses 1000 advance + 500 cash, rest on credit
Result:
  - Advance Balance: -1000 → Now 2000 BDT
  - Total Paid: +500 BDT → Now 5000 BDT (only new cash counted)
  - Customer Balance: +2000 (invoice) -1000 (advance) -500 (cash) = +500 BDT due
  - Invoice Status: PARTIALLY_PAID
  - Outstanding Amount: 500 BDT
```

---

## Key Principles

1. **Total Paid** = Sum of all NEW money received into the system
   - Original advance payments
   - Cash/card/online payments
   - **EXCLUDES** payments created from advance allocation

2. **Advance Balance** = Unused advance credit (RemainingAmount)
   - Decreases when advance is used
   - Shows available credit for future purchases

3. **Customer Balance** = What customer owes us
   - Increases when invoices are created
   - Decreases when payments are made (new or from advance)

4. **Invoice Status** = Based on ALL payments (including advance-sourced)
   - Doesn't matter if payment came from advance or new cash
   - Only matters if invoice is covered or not

5. **No Double-Counting**
   - Money is counted ONCE when first received
   - Moving money from advance to invoice is just allocation
   - Only new payments increase "Total Paid"

---

## Files Modified

1. `src/AutoPartShop.Domain/Entities/Customer.cs`
   - Fixed TotalPaid to exclude advance-sourced payments

2. `src/AutoPartShop.Api/Services/SupplierPaymentSummaryService.cs`
   - Fixed CalculateTotalPaid to exclude advance-sourced payments

3. `src/AutoPartShop.Api/Controllers/SalesOrderController.cs`
   - Fixed quick sale advance payment flow
   - Fixed invoice status update after advance payment
   - Removed duplicate customer balance updates

4. `src/AutoPartShop.Domain/Entities/Supplier.cs`
   - Added AdvanceAmount computed property

5. `src/AutoPartShop.Api/Controllers/CustomerController.cs`
   - Updated to use Customer.AdvanceAmount property

---

## Testing Checklist

### Test 1: Advance Payment Creation
- [ ] Create advance payment for customer
- [ ] Verify Advance Balance increases
- [ ] Verify Total Paid increases by same amount
- [ ] Verify Customer Balance decreases (credit)

### Test 2: Quick Sale with Full Advance Payment
- [ ] Create sale, check "Use Advance Balance"
- [ ] Pay full amount using advance
- [ ] Verify Invoice Status = "PAID"
- [ ] Verify Advance Balance decreased by amount used
- [ ] Verify Total Paid did NOT increase
- [ ] Verify Outstanding Amount = 0

### Test 3: Quick Sale with Partial Advance + Cash
- [ ] Create sale with mixed payment
- [ ] Verify Advance Balance decreased by advance amount only
- [ ] Verify Total Paid increased by cash amount only (not advance)
- [ ] Verify Invoice Status = "PARTIALLY_PAID" or "PAID"
- [ ] Verify Outstanding Amount is correct

### Test 4: Customer Payment Summary
- [ ] Check customer payment summary page
- [ ] Verify Total Paid shows only new money received
- [ ] Verify Advance Balance shows remaining credit
- [ ] Verify Outstanding Balance is correct

### Test 5: Sales Order with Advance
- [ ] Create sales order (not quick sale)
- [ ] Apply advance credit to invoice
- [ ] Verify same accounting behavior as quick sale

### Test 6: Supplier Advance Payments
- [ ] Test same scenarios for suppliers
- [ ] Verify supplier payment summary is correct
- [ ] Verify no double-counting in supplier metrics

---

## Database Impact

**No migration required** - These are computed property changes only.

The fixes change how existing data is **calculated**, not how it's stored:
- All CustomerPayment records remain unchanged
- All SupplierPayment records remain unchanged
- Only the computed totals change

---

## Next Steps

1. **Stop API** (if running)
2. **Rebuild API**: `dotnet build`
3. **Restart API**
4. **Test all scenarios** above
5. **Verify reports and summaries** show correct amounts

---

## Support

If you encounter any issues:
1. Check that API was rebuilt after these changes
2. Verify browser cache is cleared for frontend
3. Test with new transactions first (existing data should self-correct)
4. Check payment records have correct SourceAdvancePaymentId values
