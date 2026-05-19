# EDGE CASE FIXES - DEVELOPER CHECKLIST

**Status:** 🔴 CRITICAL ISSUES IDENTIFIED  
**Updated:** April 23, 2026

---

## 🔴 P0: CRITICAL - FIX THIS WEEK

### [ ] 1. Refund Amount Validation
**File:** `src/AutoPartShop.Api/Controllers/CustomerPaymentController.cs`

**Issue:** Refund amount can exceed original payment amount

**Quick Check:**
```csharp
[HttpPost("refund")]
public async Task Create(CreateRefundRequest request)
{
    var original = await _repository.GetByIdAsync(request.SourcePaymentId);
    
    // ✗ MISSING: Check below
    if (Math.Abs(request.RefundAmount) > original.Amount)
        return BadRequest("Refund exceeds original payment");
    
    // ... create refund
}
```

**Test:** Try refunding $150 on $100 payment → Should fail ✓

---

### [ ] 2. Cumulative Refunds Check
**File:** `src/AutoPartShop.Api/Controllers/CustomerPaymentController.cs`

**Issue:** Multiple refunds for same payment can exceed original

**Quick Check:**
```csharp
var existingRefunds = await _repository
    .GetRefundsBySourcePaymentAsync(request.SourcePaymentId);
var totalRefunded = existingRefunds.Sum(r => r.Amount);

if (totalRefunded + request.RefundAmount > original.Amount)
    return BadRequest("Total refunds exceed payment");
```

**Test:** 
- Payment $100
- Refund 1: $70 ✓
- Refund 2: $40 → Should fail (70+40 > 100)

---

### [ ] 3. Double Refund Prevention (Return + Warranty)
**File:** `src/AutoPartShop.Api/Controllers/WarrantyClaimsController.cs`

**Issue:** Can file warranty claim on item already returned for refund

**Quick Check:**
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateWarrantyClaimRequest request, CancellationToken ct)
{
    var warranty = await _warrantyRepository.GetByIdAsync(request.WarrantyRegistrationId, ct);
    
    // ✗ MISSING: Check below
    var return = await _salesReturnRepository
        .GetBySalesOrderLineAsync(warranty.SalesOrderLineId);
    
    if (return?.Status == "PROCESSED")
        return BadRequest($"Item already returned via {return.ReturnNumber}");
    
    // ... create claim
}
```

**Test:** 
- Create return with refund
- Try to file warranty claim → Should fail ✓

---

### [ ] 4. Warranty Expiry Validation
**File:** `src/AutoPartShop.Domain/Entities/WarrantyClaim.cs`

**Issue:** Can claim warranty after expiry date

**Quick Check:**
```csharp
public void SubmitForReview()
{
    if (Status != "PENDING")
        throw new InvalidOperationException($"Cannot submit for review. Current status: {Status}");
    
    // ✗ MISSING: Add below
    if (DateTime.UtcNow > WarrantyRegistration?.WarrantyExpiryDate)
        throw new InvalidOperationException(
            $"Warranty expired on {WarrantyRegistration?.WarrantyExpiryDate:yyyy-MM-dd}");
    
    Status = "UNDER_REVIEW";
}
```

**Test:**
- Create warranty that expired yesterday
- Try to file claim → Should fail ✓

---

### [ ] 5. Return Stock Reversal on Rejection
**File:** `src/AutoPartShop.Api/Controllers/SalesReturnController.cs`

**Issue:** When return is REJECTED, stock not reversed (stays increased)

**Before:**
```
1. Return APPROVED → Stock += 5 ✓
2. Return REJECTED → Stock stays +5 ✗ (should go back)
```

**After:**
```csharp
// In SalesReturnController
if (return.Status == "APPROVED" || return.Status == "RECEIVED")
{
    // Reverse the stock movements that were added
    foreach (var line in return.LineItems)
    {
        var movement = StockMovement.Create(
            stockLevelId: stockLevel.Id,
            movementType: "OUT",  // Reverse the IN
            quantity: line.QuantityInBaseUnit,
            reason: "RETURN_REJECTION_REVERSAL",
            referenceNumber: return.ReturnNumber
        );
        await _stockMovementRepository.AddAsync(movement);
    }
    
    // Update stock level
    stockLevel.RemoveStock(line.QuantityInBaseUnit);
    await _stockLevelRepository.UpdateAsync(stockLevel);
}

return.Reject(reason);
```

**Test:**
- Approve return (stock +5)
- Reject return (stock should -5)
- Final stock = original ✓

---

### [ ] 6. Race Condition: Concurrent Return Approval & Stock Update
**File:** `src/AutoPartShop.Api/Controllers/SalesReturnController.cs`

**Issue:** Thread race when multiple clients approve same return simultaneously

**Quick Fix:**
```csharp
[HttpPut("{id}/approve")]
public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
{
    // ✗ CURRENT: No transaction isolation
    var return = await _salesReturnRepository.GetByIdAsync(id);
    return.Approve(...);
    await _salesReturnRepository.UpdateAsync(return);
    
    // Stock update not atomic with return approval
    var stock = await _stockLevelRepository.GetByIdAsync(...);
    stock.AddStock(...);
    await _stockLevelRepository.UpdateAsync(stock);
    
    // ✓ FIX: Wrap in transaction
}
```

**Better:**
```csharp
using var transaction = await _dbContext.Database.BeginTransactionAsync();
try
{
    var return = await _salesReturnRepository.GetByIdAsync(id);
    return.Approve(...);
    
    var stock = await _stockLevelRepository.GetByIdAsync(...);
    stock.AddStock(...);
    
    await _salesReturnRepository.UpdateAsync(return);
    await _stockLevelRepository.UpdateAsync(stock);
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**Test:**
- Simulate concurrent approvals of same return
- Verify stock updated exactly once ✓

---

### [ ] 7. Credit Note Race Condition
**File:** `src/AutoPartShop.Infrastructure/Repositories/CustomerCreditNoteRepository.cs`

**Issue:** Concurrent applications of same credit note can exceed available balance

**Before (Race Condition):**
```
Thread 1: ApplyToInvoice(amount=$100) when AvailableAmount=$100
Thread 2: ApplyToInvoice(amount=$100) when AvailableAmount=$100
Result: Both succeed! $200 credited from $100 note ✗
```

**Fix:**
```csharp
using (var transaction = await _context.Database.BeginTransactionAsync(
    System.Data.IsolationLevel.Serializable))
{
    // Lock row explicitly
    var creditNote = await _context.CustomerCreditNotes
        .FromSql($"SELECT * FROM CustomerCreditNotes WHERE Id = {id} FOR UPDATE")
        .FirstOrDefaultAsync();
    
    // Now thread-safe
    if (amountToApply > creditNote.AvailableAmount)
        throw new InvalidOperationException("Insufficient credit");
    
    creditNote.UsedAmount += amountToApply;
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
```

**Test:**
- Create credit note for $100
- Concurrent threads try to apply $100 each
- Only first should succeed ✓

---

### [ ] 8. Prevent Multiple Active Warranty Claims
**File:** `src/AutoPartShop.Api/Controllers/WarrantyClaimsController.cs`

**Issue:** Same warranty can have multiple active claims simultaneously

**Quick Check:**
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateWarrantyClaimRequest request, CancellationToken ct)
{
    var warranty = await _warrantyRepository.GetByIdAsync(request.WarrantyRegistrationId, ct);
    
    // ✗ MISSING: Check below
    var activeClaim = warranty.Claims
        .FirstOrDefault(c => new[] { "PENDING", "UNDER_REVIEW", "APPROVED", "IN_PROGRESS" }
            .Contains(c.Status));
    
    if (activeClaim != null)
        return BadRequest($"Warranty already has active claim: {activeClaim.ClaimNumber}");
    
    // ... create new claim
}
```

**Test:**
- File warranty claim → Status PENDING
- Try to file another claim on same warranty → Should fail ✓

---

## 🟠 P1: HIGH - FIX NEXT SPRINT

### [ ] 9. Payment Status State Machine
**File:** `src/AutoPartShop.Domain/Entities/CustomerPayment.cs`

**Issue:** Invalid status transitions allowed (e.g., COMPLETED → PENDING)

**Implementation:**
```csharp
private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
{
    ["PENDING"] = new() { "PROCESSING", "CANCELLED" },
    ["PROCESSING"] = new() { "COMPLETED", "FAILED", "CANCELLED" },
    ["COMPLETED"] = new() { "REFUNDED", "SETTLED" },
    ["FAILED"] = new() { "PENDING", "CANCELLED" },
    ["REFUNDED"] = new() { "SETTLED", "CANCELLED" },
    ["SETTLED"] = new() { },  // Final
    ["CANCELLED"] = new() { }  // Final
};

public void SetStatus(string newStatus)
{
    if (!ValidTransitions[Status].Contains(newStatus))
        throw new InvalidOperationException(
            $"Cannot transition from {Status} to {newStatus}");
    
    Status = newStatus;
}
```

---

### [ ] 10. Refund Amount Condition-Based Factor
**File:** `src/AutoPartShop.Domain/Entities/SalesReturnLine.cs`

**Issue:** Damaged goods refunded at full price instead of reduced price

**Before:**
```csharp
public decimal RefundAmount => Quantity * UnitPrice;  // Ignores condition
```

**After:**
```csharp
private static readonly Dictionary<string, decimal> ConditionFactors = new()
{
    ["UNOPENED"] = 1.0m,   // 100%
    ["OPENED"] = 0.75m,    // 75%
    ["DAMAGED"] = 0.30m    // 30%
};

public decimal RefundAmount => Quantity * UnitPrice * ConditionFactors[Condition];
```

**Test:**
- Return UNOPENED: $100 × 1.0 = $100 ✓
- Return DAMAGED: $100 × 0.3 = $30 ✓

---

### [ ] 11. Refund After Reconciliation Prevention
**File:** `src/AutoPartShop.Api/Controllers/CustomerPaymentController.cs`

**Issue:** Can refund payment that's already been reconciled/settled

**Quick Check:**
```csharp
[HttpPost("refund")]
public async Task Create(CreateRefundRequest request)
{
    var original = await _repository.GetByIdAsync(request.SourcePaymentId);
    
    // ✗ MISSING: Check below
    if (original.IsReconciled)
        return BadRequest("Cannot refund reconciled payment");
    
    // ... create refund
}
```

**Test:**
- Reconcile payment
- Try to refund → Should fail ✓

---

### [ ] 12. Stock Lot Creation for Returns
**File:** `src/AutoPartShop.Api/Controllers/SalesReturnController.cs`

**Issue:** Returned items not tracked via stock lots - lost traceability

**Add:**
```csharp
// When return PROCESSED:
var returnLot = StockLot.Create(
    lotNumber: await _codeGenerateService.GenerateAsync("RETNLOT"),
    partId: line.PartId,
    warehouseId: return.WarehouseId,
    supplierId: null,  // Unknown from whence returned
    goodsReceiptLineId: null,
    quantityReceived: line.QuantityInBaseUnit,
    costPrice: line.UnitPriceInBaseUnit,
    receivingDate: DateTime.UtcNow,
    manufacturerLotNumber: $"RETURN-{return.ReturnNumber}",
    expiryDate: null,
    currency: return.Currency,
    notes: $"Returned item - Return#{return.ReturnNumber}, Condition: {line.Condition}",
    unitId: line.UnitId,
    quantityReceivedInBaseUnit: line.QuantityInBaseUnit,
    costPriceInBaseUnit: line.UnitPriceInBaseUnit
);

await _stockLotRepository.AddAsync(returnLot);
```

---

### [ ] 13. FIFO Enforcement in Stock Lot Selection
**File:** `src/AutoPartShop.Api/Services/StockManagementService.cs`

**Issue:** Stock lots not picked in FIFO order - COGS wrong

**Add:**
```csharp
public async Task SelectStockLotForDispatch(Guid partId, Guid warehouseId, int quantityNeeded)
{
    var availableLots = await _stockLotRepository
        .GetAvailableLots(partId, warehouseId)
        .OrderBy(l => l.ReceivingDate)  // FIFO
        .ToListAsync();
    
    int quantityRemaining = quantityNeeded;
    foreach (var lot in availableLots)
    {
        if (quantityRemaining <= 0) break;
        
        int quantityToTake = Math.Min(lot.QuantityAvailable, quantityRemaining);
        lot.DecrementQuantity(quantityToTake);
        quantityRemaining -= quantityToTake;
        
        await _stockLotRepository.UpdateAsync(lot);
    }
    
    if (quantityRemaining > 0)
        throw new InvalidOperationException("Insufficient stock available");
}
```

**Test:**
- Lot 1 (2020-01-01): 50 units, $10/unit
- Lot 2 (2020-02-01): 30 units, $12/unit
- Dispatch: 60 units
- Result: Lot 1 all + 10 from Lot 2 (FIFO) ✓

---

### [ ] 14. Stock Lot Expiry Check on Sale
**File:** `src/AutoPartShop.Api/Controllers/SalesOrderController.cs`

**Issue:** Expired stock lot still sold to customer

**Add:**
```csharp
[HttpPost("create")]
public async Task Create(CreateSalesOrderRequest request)
{
    foreach (var line in request.Lines)
    {
        var part = await _partRepository.GetByIdAsync(line.PartId);
        if (part.HasExpiry)
        {
            var lot = await _stockLotRepository.GetByIdAsync(line.StockLotId);
            if (lot?.ExpiryDate < DateTime.UtcNow)
                return BadRequest(
                    $"Stock lot {lot.LotNumber} expired on {lot.ExpiryDate:yyyy-MM-dd}");
        }
    }
    
    // ... create order
}
```

**Test:**
- Stock lot expires today
- Try to sell → Should fail ✓

---

### [ ] 15. Stock Movement Before Payment Settlement
**File:** `src/AutoPartShop.Api/Controllers/SalesReturnController.cs`

**Issue:** Stock increased on return approval, but payment not yet settled

**Current Flow (WRONG):**
```
1. Return APPROVED → Stock += 5 ← Premature
2. Payment PENDING → May fail
3. If payment fails → Stock stays increased ✗
```

**Better Flow:**
```
1. Return APPROVED → Status changed, stock NOT moved yet
2. Payment COMPLETED → Now increase stock
3. If payment fails → Stock never increased ✓
```

**Implementation:**
```csharp
// When return PROCESSED and payment COMPLETED:
var stock = await _stockLevelRepository.GetByIdAsync(...);
stock.AddStock(line.QuantityInBaseUnit);

var movement = StockMovement.Create(...);
await _stockMovementRepository.AddAsync(movement);
```

**Ensure:** Stock movements only created after payment COMPLETED, not APPROVED

---

---

## 🟡 P2: MEDIUM - PLAN FOR FUTURE

### [ ] 16. Warranty Status Update to CLAIMED
When warranty claim IN_PROGRESS, mark warranty as CLAIMED

### [ ] 17. Currency Conversion Validation
When refunding in different currency, validate exchange rate

### [ ] 18. Credit Note Cancellation Logic
Handle remaining balance when credit note cancelled

### [ ] 19. Stock Transfer In-Transit State
Track transferred stock while in-transit, not just received

### [ ] 20. Warranty Auto-Void on Return
Auto-void warranty when item returned in PROCESSED status

---

## TEST EXECUTION CHECKLIST

### Unit Tests
- [ ] Refund validation tests
- [ ] Status transition tests  
- [ ] Condition-based refund factor tests
- [ ] Warranty expiry validation tests

### Integration Tests
- [ ] Full return workflow (create → approve → receive → process)
- [ ] Full warranty workflow (register → claim → approve → complete)
- [ ] Concurrent operations (returns, payments, sales)
- [ ] Stock reconciliation end-to-end

### Load Tests
- [ ] 100 concurrent refund creations (only 1 should succeed)
- [ ] 50 concurrent credit note applications
- [ ] Verify no race conditions

### Regression Tests
- [ ] Run all existing tests
- [ ] Verify no breaking changes

---

## SIGN-OFF

- [ ] Backend Lead Review
- [ ] QA Lead Review  
- [ ] Database Team Review
- [ ] Ready for Implementation

---

**Document prepared:** April 23, 2026  
**Target Sprint:** Current  
**Estimated Effort:** 40-60 story points for all P0 + P1 items
