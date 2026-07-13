# COMPREHENSIVE EDGE CASE ANALYSIS
## Warranty Flow, Refund Processing, Item Returns & Stock Tracking

**Document Date:** April 23, 2026  
**Project:** AutoPartShop ERP System  
**Scope:** All critical flows affecting financial & inventory integrity

---

## TABLE OF CONTENTS
1. [WARRANTY FLOW - Edge Cases](#1-warranty-flow---edge-cases)
2. [REFUND PROCESSING - Edge Cases](#2-refund-processing---edge-cases)
3. [ITEM RETURNS FLOW - Edge Cases](#3-item-returns-flow---edge-cases)
4. [STOCK TRACKING - Edge Cases](#4-stock-tracking---edge-cases)
5. [INTEGRATED FLOW - Edge Cases](#5-integrated-flow---edge-cases)
6. [CRITICAL ISSUES FOUND](#6-critical-issues-found)
7. [RECOMMENDATIONS](#7-recommendations)

---

## 1. WARRANTY FLOW - EDGE CASES

### 1.1 Warranty Expiry Issues

#### **EDGE CASE 1.1.1: Warranty Claim After Expiry**
- **Description:** Customer files warranty claim AFTER warranty expiry date
- **Current Status:** ⚠️ **HIGH RISK** - No expiry date validation in WarrantyClaim creation
- **Issue Location:** [WarrantyClaimsController.cs](src/AutoPartShop.Api/Controllers/WarrantyClaimsController.cs)
- **Problem:**
  ```csharp
  // Current: No check if warranty is EXPIRED before allowing claim
  public void SubmitForReview()
  {
      if (Status != "PENDING")
          throw new InvalidOperationException($"Cannot submit for review. Current status: {Status}");
      Status = "UNDER_REVIEW";  // ✗ Missing: Check if warranty expired
  }
  ```
- **Business Impact:** Unauthorized claims for expired warranties
- **Risk Level:** HIGH - Financial loss
- **Fix Required:** YES
- **Suggested Fix:**
  ```csharp
  public void SubmitForReview()
  {
      if (Status != "PENDING")
          throw new InvalidOperationException($"Cannot submit for review. Current status: {Status}");
      
      // ADD THIS: Validation against warranty expiry
      if (DateTime.UtcNow > WarrantyRegistration?.WarrantyExpiryDate)
          throw new InvalidOperationException($"Warranty expired on {WarrantyRegistration?.WarrantyExpiryDate:yyyy-MM-dd}");
      
      Status = "UNDER_REVIEW";
  }
  ```

#### **EDGE CASE 1.1.2: Warranty Status Transition Not Updated to CLAIMED**
- **Description:** After warranty claim is approved & IN_PROGRESS, warranty registration status should be marked as "CLAIMED"
- **Current Status:** ⚠️ **MEDIUM RISK** - Status transition logic missing
- **Issue Location:** [WarrantyClaim.cs](src/AutoPartShop.Domain/Entities/WarrantyClaim.cs)
- **Problem:**
  ```csharp
  // Current: No automatic update to warranty registration when claim moves to IN_PROGRESS
  public void StartServiceWithoutTechnician()
  {
      Status = "IN_PROGRESS";  // ✗ Does NOT update WarrantyRegistration.Status to CLAIMED
  }
  ```
- **Business Impact:** Cannot easily identify which warranties have active claims
- **Risk Level:** MEDIUM - Reporting & audit accuracy
- **Fix Required:** YES
- **Suggested Fix:**
  - Add method to WarrantyClaim to update warranty registration status
  - Call this when claim moves to IN_PROGRESS status

#### **EDGE CASE 1.1.3: Multiple Active Claims Per Warranty**
- **Description:** Same warranty registration has multiple active/pending claims simultaneously
- **Current Status:** ⚠️ **HIGH RISK** - No business rule validation
- **Problem:** No check in [WarrantyClaimsController](src/AutoPartShop.Api/Controllers/WarrantyClaimsController.cs) to prevent multiple active claims
- **Business Impact:** 
  - Duplicate refunds/replacements for same item
  - Warranty status becomes invalid
- **Risk Level:** HIGH - Financial loss + Audit failure
- **Fix Required:** YES
- **Suggested Fix:**
  ```csharp
  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateWarrantyClaimRequest request, CancellationToken ct)
  {
      var warranty = await _warrantyRepository.GetByIdAsync(request.WarrantyRegistrationId, ct);
      
      // ADD: Check for existing active claim
      var activeClaim = warranty.Claims
          .FirstOrDefault(c => new[] { "PENDING", "UNDER_REVIEW", "APPROVED", "IN_PROGRESS" }
              .Contains(c.Status));
      
      if (activeClaim != null)
          return BadRequest($"Warranty already has active claim: {activeClaim.ClaimNumber}. " +
              $"Close existing claim before filing new one.");
      
      // ... rest of creation logic
  }
  ```

#### **EDGE CASE 1.1.4: Warranty Void After Claim Approval**
- **Description:** Warranty voided/cancelled AFTER claim already approved but before completion
- **Current Status:** ⚠️ **HIGH RISK** - No audit trail or prevention
- **Problem:** 
  - User can modify warranty before claim completes
  - Claim completion becomes invalid
- **Business Impact:** Inconsistent state - warranty voided but claim still processing
- **Risk Level:** HIGH - Data integrity
- **Fix Required:** YES
- **Suggested Fix:** Add business rule: Cannot void warranty if it has active claims (APPROVED or IN_PROGRESS)

#### **EDGE CASE 1.1.5: Grace Period Not Enforced**
- **Description:** Claims allowed immediately after warranty expiry or near-expiry acceptance too late
- **Current Status:** ✓ **ACCEPTABLE** - Depends on business policy (usually 30-60 days grace)
- **Problem:** No configurable grace period
- **Suggestion:** Add WarrantyClaimGracePeriodDays configuration

---

### 1.2 Warranty Claim Refund Issues

#### **EDGE CASE 1.2.1: Warranty Refund Without Linking to Original Sale**
- **Description:** Refund created for warranty claim but not linked back to original invoice/sales order
- **Current Status:** ⚠️ **MEDIUM RISK** - CustomerPayment can be created without SalesOrder reference
- **Problem:**
  ```csharp
  // In CustomerPayment entity:
  public Guid? WarrantyClaimId { get; private set; }  // Optional - may not be set
  public Guid? InvoiceId { get; private set; }        // Optional - may not be set
  ```
- **Business Impact:** Cannot reconcile warranty refunds back to original sales
- **Risk Level:** MEDIUM - Audit trail broken
- **Fix Required:** YES - Enforce referential integrity

#### **EDGE CASE 1.2.2: Warranty Service Cost Not Offset Against Refund**
- **Description:** Part has high warranty service cost but customer refunds at full sale price
- **Current Status:** ⚠️ **HIGH RISK** - No deduction logic
- **Problem:**
  ```csharp
  public decimal ServiceCost { get; private set; } = 0;  // Tracked but not used in refund calc
  ```
- **Scenario:** Item sold for $100, warranty processed with $50 service cost, customer gets $100 refund
- **Business Impact:** Financial loss
- **Risk Level:** HIGH - Profit margin erosion
- **Fix Required:** YES
- **Suggested Logic:**
  ```csharp
  decimal refundableAmount = salePrice - (serviceCost * 0.5);  // Example: 50% service cost deduction
  ```

#### **EDGE CASE 1.2.3: Warranty Refund Without Return of Defective Item**
- **Description:** Warranty claim approved with REFUND service type but defective item not physically returned
- **Current Status:** ⚠️ **HIGH RISK** - No validation
- **Problem:** Lost inventory asset - refund given without item return
- **Business Impact:** 
  - Double loss: Refund given + inventory not recovered
  - Item can be resold illegally
- **Risk Level:** HIGH - Financial loss
- **Fix Required:** YES
- **Suggested Logic:** 
  - Make return of part mandatory for REFUND claims
  - Create stock movement: RETURN on claim completion
  - Link with SalesReturn or separate WarrantyReturn entity

---

### 1.3 Warranty & Sale Order Line Relationship

#### **EDGE CASE 1.3.1: Warranty Created for Non-Warranted Part**
- **Description:** Warranty registration created for part where HasWarranty=false
- **Current Status:** ✓ **PROTECTED** - Validation exists in WarrantyService.CreateWarrantyForSalesOrderLineAsync()
  ```csharp
  if (!part.HasWarranty || !part.WarrantyPeriodMonths.HasValue)
      throw new InvalidOperationException($"Part {part.Name} does not have warranty");
  ```
- **Status:** SECURE ✓

#### **EDGE CASE 1.3.2: Warranty Linked to Cancelled Sales Order**
- **Description:** Sales order cancelled but warranty registration still ACTIVE
- **Current Status:** ⚠️ **HIGH RISK** - No cascade update
- **Problem:** Orphaned warranty - can still file claims for cancelled orders
- **Business Impact:** Warranty honor obligation on orders that never completed
- **Risk Level:** HIGH - Compliance issue
- **Fix Required:** YES
- **Suggested Fix:** Cascade update - mark warranty VOID when SO cancelled

---

## 2. REFUND PROCESSING - EDGE CASES

### 2.1 Payment Refund Issues

#### **EDGE CASE 2.1.1: Refund Amount Greater Than Original Payment**
- **Description:** Customer payment refund processed for more than original receipt
- **Current Status:** ⚠️ **HIGH RISK** - No validation in CustomerPaymentController
- **Problem:**
  ```csharp
  public static CustomerPayment Create(... decimal amount, ...)
  {
      if (amount < 0 && method != "REFUND")
          throw new ArgumentException("Negative amounts only for REFUND");
      // ✗ But no check if REFUND amount > original payment
  }
  ```
- **Scenario:** Original payment: $100, Refund created for: $150
- **Business Impact:** Cash outflow exceeds receipt
- **Risk Level:** CRITICAL - Financial loss
- **Fix Required:** YES
- **Suggested Fix:**
  ```csharp
  // In refund creation logic:
  var originalPayment = await _repository.GetByIdAsync(sourcePaymentId);
  if (Math.Abs(refundAmount) > originalPayment.Amount)
      throw new InvalidOperationException($"Refund amount cannot exceed original payment");
  ```

#### **EDGE CASE 2.1.2: Multiple Refunds for Single Payment**
- **Description:** Same payment refunded multiple times
- **Current Status:** ⚠️ **HIGH RISK** - No aggregate validation
- **Problem:**
  ```csharp
  // No tracking of: Total refunded amount vs Original payment
  ```
- **Scenario:** 
  - Payment received: $100
  - Refund 1: $70
  - Refund 2: $50 (should only be allowed for $30)
- **Business Impact:** Over-refunding
- **Risk Level:** CRITICAL - Financial loss
- **Fix Required:** YES
- **Suggested Fix:**
  ```csharp
  // Track total refunded amount
  var totalRefunds = await _repository
      .GetRefundsBySourcePaymentAsync(sourcePaymentId)
      .SumAsync(p => p.Amount);
  
  if (totalRefunds + newRefundAmount > originalPayment.Amount)
      throw new InvalidOperationException("Refund exceeds original payment amount");
  ```

#### **EDGE CASE 2.1.3: Partial Refund Without Partial Payment Linkage**
- **Description:** Create partial refund but don't link to original payment or invoice
- **Current Status:** ⚠️ **HIGH RISK** - Linkage is optional
- **Problem:**
  ```csharp
  public Guid? SourceAdvancePaymentId { get; set; }  // Optional - may not track relationship
  ```
- **Business Impact:** Cannot reconcile partial refunds
- **Risk Level:** HIGH - Reconciliation failure
- **Fix Required:** YES - Make SourcePaymentId mandatory for refunds

#### **EDGE CASE 2.1.4: Refund After Payment Reconciliation**
- **Description:** Payment reconciled (settled) but then refund processed
- **Current Status:** ⚠️ **HIGH RISK** - No prevention
- **Problem:**
  ```csharp
  public bool IsReconciled { get; private set; } = false;
  // ✗ No check: Can create refund on reconciled payment
  ```
- **Business Impact:** Breaks reconciliation - settled amounts change
- **Risk Level:** HIGH - Audit failure
- **Fix Required:** YES
- **Suggested Fix:**
  ```csharp
  if (originalPayment.IsReconciled)
      throw new InvalidOperationException(
          $"Cannot refund reconciled payment. Reverse reconciliation first.");
  ```

#### **EDGE CASE 2.1.5: Refund in Different Currency Than Original Payment**
- **Description:** Original payment in USD, refund in EUR without proper conversion
- **Current Status:** ⚠️ **MEDIUM RISK** - Currency field exists but no validation
- **Problem:** Currency mismatch in payment/refund pair
- **Business Impact:** 
  - Exchange rate loss not accounted for
  - Reconciliation incorrect
- **Fix Required:** YES
- **Suggested Logic:**
  ```csharp
  if (refundPayment.Currency != originalPayment.Currency)
  {
      // Must use exchange rate from refund date
      var exchangeRate = await _exchangeRateService
          .GetRateAsync(originalPayment.Currency, refundPayment.Currency);
      
      var convertedAmount = originalPayment.Amount / exchangeRate;
      if (Math.Abs(refundAmount - convertedAmount) > 0.01m)  // 1 cent tolerance
          return BadRequest("Currency conversion mismatch");
  }
  ```

---

### 2.2 Payment Status Transitions

#### **EDGE CASE 2.2.1: Invalid Status Transition Allowed**
- **Description:** Payment status transitions allowed: COMPLETED → PENDING (reversal without explicit void)
- **Current Status:** ⚠️ **HIGH RISK** - No state machine
- **Problem:** No validation of valid status transitions
- **Valid Transitions:**
  ```
  PENDING → PROCESSING → COMPLETED → SETTLED (✓ forward only)
  
  Invalid Reverse:
  COMPLETED → PENDING (✗ should not be allowed)
  
  Special Cases:
  COMPLETED → FAILED (✗ completed can't become failed)
  COMPLETED → REFUNDED (✓ valid - refund initiated)
  ```
- **Fix Required:** YES
- **Suggested Implementation:** State machine pattern
  ```csharp
  private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
  {
      ["PENDING"] = new() { "PROCESSING", "CANCELLED" },
      ["PROCESSING"] = new() { "COMPLETED", "FAILED", "CANCELLED" },
      ["COMPLETED"] = new() { "REFUNDED", "SETTLED", "CANCELLED" },
      ["FAILED"] = new() { "PENDING", "CANCELLED" },
      ["REFUNDED"] = new() { "SETTLED", "CANCELLED" },
      ["SETTLED"] = new() { },  // Final state
      ["CANCELLED"] = new() { }  // Final state
  };
  ```

#### **EDGE CASE 2.2.2: Settlement Date Without IsReconciled Flag**
- **Description:** Payment marked SettledDate but IsReconciled=false
- **Current Status:** ⚠️ **HIGH RISK** - Inconsistent state possible
- **Problem:** Logical inconsistency in flags
- **Fix Required:** YES
- **Suggested Logic:**
  ```csharp
  public void SettlePayment(string settledBy)
  {
      Status = "SETTLED";
      SettledDate = DateTime.UtcNow;
      SettledBy = settledBy;
      IsReconciled = true;  // Must set both together
  }
  ```

---

### 2.3 Credit Note Issues

#### **EDGE CASE 2.3.1: Credit Note Applied To Multiple Invoices Simultaneously**
- **Description:** Single credit note applied to Invoice A and B in parallel transactions
- **Current Status:** ⚠️ **HIGH RISK** - Race condition possible
- **Problem:**
  ```csharp
  public void ApplyToInvoice(Guid invoiceId, Guid salesOrderId, decimal amountToApply)
  {
      if (amountToApply > AvailableAmount)
          throw new InvalidOperationException(...);
      
      UsedAmount += amountToApply;  // ✗ Race condition if called twice simultaneously
      InvoiceId = invoiceId;        // ✗ Overwrites previous application
  }
  ```
- **Scenario:**
  - Credit Note Available: $100
  - Thread 1: Apply $100 to Invoice A
  - Thread 2: Apply $100 to Invoice B (should fail but doesn't)
  - Result: $200 credited from $100 note
- **Business Impact:** CRITICAL - Over-crediting
- **Risk Level:** CRITICAL - Concurrency issue
- **Fix Required:** YES - Use pessimistic locking or transaction isolation
  ```csharp
  using (var transaction = await _context.Database.BeginTransactionAsync())
  {
      // Lock row
      var cn = await _context.CustomerCreditNotes
          .FromSqlInterpolated($"SELECT * FROM CustomerCreditNotes WHERE Id = {creditNoteId} FOR UPDATE")
          .FirstOrDefaultAsync();
      
      // Now apply safely
      // ...
  }
  ```

#### **EDGE CASE 2.3.2: Credit Note Expiry Not Enforced**
- **Description:** Credit note used after ExpiryDate
- **Current Status:** ✓ **PROTECTED** - Validation exists
  ```csharp
  if (Status == "EXPIRED")
      throw new InvalidOperationException("Cannot apply an expired credit note");
  ```
- **Problem:** But Status must be manually updated to EXPIRED - no automatic expiry check
- **Fix Required:** YES - Add automatic expiry check or scheduled job

#### **EDGE CASE 2.3.3: Credit Note Issued But Never Used / Audit Trail Lost**
- **Description:** Credit note created but InvoiceId/SalesOrderId never populated
- **Current Status:** ⚠️ **MEDIUM RISK** - Fields optional
- **Problem:**
  ```csharp
  public Guid? InvoiceId { get; set; }        // Optional
  public Guid? SalesOrderId { get; set; }     // Optional
  ```
- **Impact:** Cannot track which transaction credit was used for
- **Fix Required:** YES
- **Suggested Fix:** Add CreditNoteUsage entity tracking each application separately

#### **EDGE CASE 2.3.4: Credit Note Cancelled With Remaining Balance**
- **Description:** Credit note with $30 remaining balance cancelled - where does $30 go?
- **Current Status:** ⚠️ **HIGH RISK** - No reversal logic
- **Problem:**
  ```csharp
  // No method to handle cancellation with remaining balance
  public void Cancel()  // ✗ Missing this method
  {
      Status = "CANCELLED";
      // But what about UsedAmount and AvailableAmount?
      // Should it be:
      // - Written off?
      // - Refunded to customer?
      // - Restored to original sales return?
  }
  ```
- **Business Impact:** Accounting reconciliation fails
- **Risk Level:** HIGH - Audit trail
- **Fix Required:** YES - Define business rule for cancellation handling

---

## 3. ITEM RETURNS FLOW - EDGE CASES

### 3.1 Over-Return Issues

#### **EDGE CASE 3.1.1: Return Quantity Exceeds Ordered Quantity**
- **Description:** Customer returns 15 units when only 10 units ordered
- **Current Status:** ✓ **PROTECTED** - Validation exists
  ```csharp
  if (line.Quantity > orderLine.Quantity)
      return BadRequest($"Return quantity ({line.Quantity}) exceeds ordered qty...");
  ```
- **Status:** SECURE ✓

#### **EDGE CASE 3.1.2: Cumulative Returns Across Multiple Returns Exceed Order**
- **Description:** First return: 5 units, Second return: 7 units, Ordered: 10 units (Total return = 12) 
- **Current Status:** ✓ **PROTECTED** - Validation exists
  ```csharp
  var alreadyReturned = activeReturns
      .SelectMany(r => r.LineItems)
      .Where(rl => rl.SalesOrderLineId == line.SalesOrderLineId)
      .Sum(rl => rl.Quantity);
  
  if (alreadyReturned + line.Quantity > orderLine.Quantity)
      return BadRequest($"Total return quantity would exceed ordered...");
  ```
- **Status:** SECURE ✓

#### **EDGE CASE 3.1.3: Return Quantity Greater Than Rejected Return Quantity Check Lost**
- **Description:** Goods Receipt recorded with 10 accepted but 15 returns filed
- **Current Status:** ⚠️ **HIGH RISK** - Not validated against GRN
- **Problem:** Return validation only checks against SO, not against actual received quantity
- **Scenario:**
  - PO: 100 units
  - GRN: 80 received, 10 rejected (70 accepted)
  - SO sold from these: 65 units
  - Return filed: 70 units (exceeds what was sold from this batch)
- **Fix Required:** YES - Validate against actual goods received

---

### 3.2 Return Status & Workflow Issues

#### **EDGE CASE 3.2.1: Return Rejected After Quantity Deducted From Stock**
- **Description:** Return APPROVED & stock updated, then return REJECTED but stock not reversed
- **Current Status:** ⚠️ **CRITICAL RISK** - Stock reversal missing
- **Problem:**
  ```csharp
  // When return APPROVED: Stock increased (return received)
  // When return REJECTED: ✗ Stock NOT decreased back
  ```
- **Scenario:**
  1. Return created: 5 units → Status: PENDING, Stock: No change
  2. Return APPROVED → Stock INCREASED by 5 units ✓
  3. Return REJECTED → Stock should DECREASE by 5 units ✗ (NOT HAPPENING)
  4. Result: Extra 5 units in stock
- **Business Impact:** Inventory overstated
- **Risk Level:** CRITICAL - Inventory inaccuracy
- **Fix Required:** YES
- **Suggested Implementation:**
  ```csharp
  public void Reject(string reason = "")
  {
      if (Status == "APPROVED" || Status == "RECEIVED")
      {
          // MUST reverse stock movement
          foreach (var line in LineItems)
          {
              // Create reverse stock movement: OUT (to compensate for the IN that happened)
              var reverseMovement = StockMovement.Create(
                  stockLevelId,
                  movementType: "OUT",
                  quantity: -line.QuantityInBaseUnit,
                  reason: "RETURN_REJECTION_REVERSAL",
                  referenceNumber: ReturnNumber
              );
              await _stockMovementRepository.AddAsync(reverseMovement);
          }
      }
      Status = "REJECTED";
  }
  ```

#### **EDGE CASE 3.2.2: Only PROCESSED Returns Should Generate Credit Notes**
- **Description:** Credit note created for PENDING or APPROVED returns (not received yet)
- **Current Status:** ⚠️ **HIGH RISK** - No workflow dependency
- **Problem:** Credit note issued before physical goods confirmed received
- **Business Impact:** Refund given for items not yet in warehouse
- **Risk Level:** HIGH - Financial loss
- **Fix Required:** YES
- **Suggested Rule:** Credit notes may only be generated when return Status = "PROCESSED"

#### **EDGE CASE 3.2.3: Return Amount Calculated from Damaged Condition Goods Different**
- **Description:** Return has DAMAGED condition items but refunded at full UNOPENED price
- **Current Status:** ⚠️ **HIGH RISK** - No condition-based pricing
- **Problem:**
  ```csharp
  public string Condition { get; private set; } = string.Empty;  // UNOPENED, OPENED, DAMAGED
  
  public decimal RefundAmount => Quantity * UnitPrice;  // ✗ Ignores Condition
  ```
- **Scenario:**
  - Item sale price: $100 (UNOPENED)
  - Return condition: DAMAGED
  - Refund: Should be $100 × 0.5 = $50
  - Actual refund: $100 (WRONG)
- **Business Impact:** Over-refunding damaged goods
- **Risk Level:** HIGH - Profit erosion
- **Fix Required:** YES
- **Suggested Implementation:**
  ```csharp
  private static readonly Dictionary<string, decimal> ConditionRefundFactors = new()
  {
      ["UNOPENED"] = 1.0m,   // 100% refund
      ["OPENED"] = 0.75m,    // 75% refund
      ["DAMAGED"] = 0.30m    // 30% refund
  };
  
  public decimal RefundAmount => Quantity * UnitPrice * ConditionRefundFactors[Condition];
  ```

---

### 3.3 Return & Warranty Integration

#### **EDGE CASE 3.3.1: Item Returned As Warranty Claim But Already Refunded**
- **Description:** 
  - Item returned via SalesReturn (refund given)
  - Then warranty claim filed for same item
  - Two refunds issued for single item
- **Current Status:** ⚠️ **CRITICAL RISK** - No cross-flow validation
- **Problem:** No check to prevent warranty claim after sales return
- **Business Impact:** Double refund
- **Risk Level:** CRITICAL - Financial loss
- **Fix Required:** YES
- **Suggested Fix:**
  ```csharp
  // When creating warranty claim:
  var existingReturn = await _salesReturnRepository
      .GetBySalesOrderLineAsync(warranty.SalesOrderLineId);
  
  if (existingReturn?.Status == "PROCESSED")
      throw new InvalidOperationException(
          $"Item already returned and refunded via {existingReturn.ReturnNumber}");
  ```

#### **EDGE CASE 3.3.2: Return Created For Item Under Warranty - Should Warranty Be Voided?**
- **Description:** Customer returns item within warranty period - should warranty be voided?
- **Current Status:** ⚠️ **POLICY DECISION NEEDED** - No auto-void logic
- **Problem:** Item returned = customer no longer has the item, so warranty is moot
- **Logic Question:** 
  - Should warranty auto-void when return APPROVED?
  - Or should it stay ACTIVE until manually closed?
- **Recommended Business Rule:**
  ```
  IF SalesReturn.Status = PROCESSED THEN
     WarrantyRegistration.Status = VOID
     WarrantyRegistration.VoidReason = "Item Returned - Return #" + ReturnNumber
  ```
- **Fix Required:** YES

---

### 3.4 Return & Inventory Integration

#### **EDGE CASE 3.4.1: Stock Increased Before Return Payment Processed**
- **Description:** Return stock movement created when status = APPROVED, but payment not yet settled
- **Current Status:** ⚠️ **HIGH RISK** - Premature stock increase
- **Problem:**
  ```
  Timeline:
  1. Return PENDING → No stock change
  2. Return APPROVED → Stock INCREASED ← Problem: Premature
  3. Payment PENDING → Stock now reflects not-yet-refunded goods
  4. Payment FAILED → Stock should decrease but doesn't
  ```
- **Business Impact:** Stock overstated until payment confirmed
- **Risk Level:** HIGH - Inventory accuracy
- **Fix Required:** YES
- **Suggested Fix:** Stock movement only when Status = "PROCESSED"

#### **EDGE CASE 3.4.2: Multi-Unit Return Not Converted to Base Unit for Stock**
- **Description:** Item normally sold in Dozen but returned in Pieces
- **Current Status:** ⚠️ **HIGH RISK** - Conversion logic may fail
- **Problem:**
  ```csharp
  public int QuantityInBaseUnit { get; private set; }  // Should always be set
  ```
- **Scenario:**
  - Part: Base unit = Piece, Sold unit = Dozen
  - SO line: 2 Dozen (= 24 Pieces)
  - Return: 18 Pieces (should = 1.5 Dozen)
  - Stock movement: Must use 18 Pieces (base unit), not 1.5 Dozen
- **Fix Required:** YES - Ensure QuantityInBaseUnit always populated during return creation

#### **EDGE CASE 3.4.3: Return From Multiple Warehouses Creates Stock Imbalance**
- **Description:** Item sold from Warehouse A, return received in Warehouse B
- **Current Status:** ⚠️ **MEDIUM RISK** - Possible confusion
- **Problem:**
  - Warehouse A: Stock decreased (sale)
  - Warehouse B: Stock increased (return received)
  - May not match if items not transferred between warehouses
- **Fix Required:** YES - Implement inter-warehouse transfer or manual reconciliation

#### **EDGE CASE 3.4.4: Return of Stock Lot Not Properly Linked**
- **Description:** Return doesn't reference which stock lot was returned
- **Current Status:** ⚠️ **HIGH RISK** - Lot traceability lost
- **Problem:**
  - Part purchased from Supplier A (Lot 1) & Supplier B (Lot 2)
  - Item returned but lot unknown
  - Cannot track which supplier's goods are defective
- **Business Impact:** FIFO/LIFO and supplier quality tracking broken
- **Risk Level:** HIGH - Quality control & supplier management
- **Fix Required:** YES
- **Suggested Enhancement:** Add StockLotId field to SalesReturnLine

---

## 4. STOCK TRACKING - EDGE CASES

### 4.1 Stock Movement Issues

#### **EDGE CASE 4.1.1: Negative Stock Possible in Edge Cases**
- **Description:** Stock level goes negative due to return or reversal
- **Current Status:** ⚠️ **HIGH RISK** - RemoveStock has guard but only after retrieval
- **Problem:**
  ```csharp
  public void RemoveStock(int quantity, ...)
  {
      if (quantity > QuantityAvailable)
          throw new InvalidOperationException("Insufficient stock available");
      
      QuantityOnHand -= quantity;  // But this can be called multiple times in parallel
  }
  ```
- **Risk:** Race condition in concurrent scenarios
- **Fix Required:** YES - Use database-level constraint or pessimistic locking

#### **EDGE CASE 4.1.2: Stock Movement Type "RETURN" Ambiguous**
- **Description:** MovementType "RETURN" doesn't specify: Return FROM customer or Return TO supplier
- **Current Status:** ⚠️ **HIGH RISK** - Context lost
- **Problem:**
  ```csharp
  string MovementType = "RETURN";  // Could be:
  // - Sales Return (stock +)
  // - Purchase Return (stock -)
  // - Warranty Return (stock +)
  ```
- **Business Impact:** Cannot audit trail specific reason for return
- **Fix Required:** YES
- **Suggested Fix:** Use more specific types:
  ```
  "SALES_RETURN"
  "PURCHASE_RETURN"
  "WARRANTY_RETURN"
  "DAMAGE_ADJUSTMENT"
  "LOSS_ADJUSTMENT"
  ```

#### **EDGE CASE 4.1.3: Stock Movement Not Approved Until Later**
- **Description:** Stock movement created with ApprovedBy = empty, approved days later
- **Current Status:** ⚠️ **MEDIUM RISK** - Consistency
- **Problem:**
  ```csharp
  var movement = StockMovement.Create(...);
  // ApprovedBy = "" (empty)
  await _repository.AddAsync(movement);
  
  // Days later...
  movement.Approve("Manager");  // ✓ Can still approve
  ```
- **Business Impact:** May cause reporting issues if queries filter on ApprovedBy
- **Recommendation:** Auto-approve system movements, require approval for manual movements

#### **EDGE CASE 4.1.4: Quantity vs QuantityInBaseUnit Mismatch**
- **Description:** Quantity and QuantityInBaseUnit don't match due to unit conversion error
- **Current Status:** ⚠️ **HIGH RISK** - Unit conversion logic complex
- **Problem:**
  ```csharp
  // In GRN processing:
  var conversionFactor = await _unitConversionService
      .GetConversionFactorAsync(grnLine.UnitId.Value, part.BaseUnitId.Value);
  
  if (conversionFactor <= 0)
      throw new InvalidOperationException("Invalid unit conversion factor");
  
  receivedBaseQuantity = (int)Math.Round(grnLine.ReceivedQuantity * conversionFactor);
  // ✗ If conversion factor rounds incorrectly, base unit qty wrong
  ```
- **Scenario:**
  - Received: 10 (unit: dozen)
  - Base unit: piece
  - Conversion: 10 × 12 = 120
  - But if rounding: 10 × 12.4 = 124 (wrong conversion factor)
- **Fix Required:** YES - Validate conversion factors strict

#### **EDGE CASE 4.1.5: Stock Movement Reversal Creates Negative**
- **Description:** GRN received (stock +100), then GRN reversed (stock -100), result can go negative
- **Current Status:** ⚠️ **HIGH RISK** - No reversal quantity validation
- **Problem:**
  ```
  Scenario:
  1. Stock = 0
  2. GRN: 100 units → Stock = 100
  3. Sales: 80 units → Stock = 20
  4. GRN Reversal: -100 units → Stock = -80 (NEGATIVE!)
  ```
- **Business Impact:** Invalid negative stock
- **Risk Level:** HIGH - Inventory accuracy
- **Fix Required:** YES - Check available quantity before reversal

---

### 4.2 Stock Lot Issues

#### **EDGE CASE 4.2.1: Stock Lot Not Created for Return Items**
- **Description:** When return stock is added back, no stock lot created to track it
- **Current Status:** ⚠️ **HIGH RISK** - Lost traceability
- **Problem:** Stock lot created only for GRN but not for returns
- **Impact:** Cannot track returned items separately - may sell returned goods as new
- **Fix Required:** YES - Create stock lot for returned inventory

#### **EDGE CASE 4.2.2: Stock Lot Expiry Not Verified Before Sale**
- **Description:** Expired stock lot still available for sale
- **Current Status:** ⚠️ **HIGH RISK** - No expiry validation
- **Problem:**
  ```csharp
  // Stock lot has expiryDate but sales orders don't check it
  ```
- **Business Impact:** Sell expired goods
- **Risk Level:** HIGH - Compliance & customer satisfaction
- **Fix Required:** YES - Add expiry check in sales order line creation

#### **EDGE CASE 4.2.3: Stock Lot Quantity Not Properly Decremented on Sale**
- **Description:** Stock lot quantity not reduced when SO line dispatches
- **Current Status:** ⚠️ **MEDIUM RISK** - Lot quantity tracking
- **Problem:** StockLot.QuantityAvailable may not match actual quantities
- **Fix Required:** YES - Update stock lot when SO line dispatched

#### **EDGE CASE 4.2.4: Multiple Stock Lots for Same Part - FIFO Not Enforced**
- **Description:** Part has 10 stock lots, sales pick randomly instead of oldest (FIFO)
- **Current Status:** ⚠️ **HIGH RISK** - No FIFO enforcement
- **Problem:** 
  - Lot 1 (oldest): 50 units, Purchase price: $10
  - Lot 2 (new): 30 units, Purchase price: $12
  - Sale: System picks Lot 2 instead of Lot 1
- **Business Impact:** 
  - COGS calculation wrong (higher cost picked)
  - Old inventory never sells
- **Risk Level:** HIGH - Financial accuracy
- **Fix Required:** YES - Implement FIFO logic
  ```csharp
  var targetLot = stockLots
      .OrderBy(l => l.ReceivingDate)  // FIFO
      .FirstOrDefault(l => l.QuantityAvailable > 0);
  ```

---

### 4.3 Stock & Payment Reconciliation

#### **EDGE CASE 4.3.1: Stock Increased Before Payment Received**
- **Description:** Sales return stock added back, but refund payment still PENDING
- **Current Status:** ⚠️ **HIGH RISK** - Premature stock increase
- **Problem:**
  ```
  Desired flow:
  1. Return APPROVED
  2. Payment COMPLETED ✓
  3. Then increase stock
  
  Actual flow:
  1. Return APPROVED → Stock increased ✓
  2. Payment may FAIL later ✗
  ```
- **Business Impact:** Stock overstated if payment fails
- **Risk Level:** HIGH
- **Fix Required:** YES - Coordinate stock movement with payment settlement

#### **EDGE CASE 4.3.2: Stock Lot Cost Basis Differs From Invoice Cost**
- **Description:** 
  - Part purchased: Cost = $10/unit (in stock lot)
  - Invoice shows: $12/unit
  - Sale booked at invoice cost but COGS from lot shows different
- **Current Status:** ⚠️ **HIGH RISK** - Accounting mismatch
- **Fix Required:** YES - Ensure consistency

---

### 4.4 Warehouse Transfer Issues

#### **EDGE CASE 4.4.1: Stock Transfer Not Rolled Back on Cancellation**
- **Description:** Stock transferred from WH-A to WH-B, then transfer cancelled but stock stays in WH-B
- **Current Status:** ⚠️ **HIGH RISK** - No reversal logic
- **Problem:** No transfer reversal mechanism
- **Fix Required:** YES - Implement transfer cancellation with stock reversal

#### **EDGE CASE 4.4.2: Stock Transfer Between Warehouses Creates In-Transit Loss**
- **Description:** 50 units transferred but only 40 received - 10 units lost in transit
- **Current Status:** ⚠️ **HIGH RISK** - No tracking
- **Problem:** Stock movements don't account for in-transit state
- **Fix Required:** YES
- **Suggested States:**
  ```
  WH-A: Stock OUT (50 units) → Status: IN_TRANSIT
  WH-B: Stock IN-TRANSIT (50 units awaiting receipt)
  Transfer:
    - Received: 40 units → Stock IN (40)
    - Shortage: 10 units (discrepancy report)
  ```

---

## 5. INTEGRATED FLOW - EDGE CASES

### 5.1 Return → Stock → Refund Integration

#### **EDGE CASE 5.1.1: Race Condition - Concurrent Return & Sale**
- **Description:**
  1. Customer submits return for 5 units
  2. Simultaneously, new sale of 3 units from same stock
  3. Final stock unclear
- **Current Status:** ⚠️ **CRITICAL RISK** - No transaction isolation
- **Problem:**
  ```
  Thread 1: Return APPROVED → Stock += 5
  Thread 2: Sale ORDER → Stock -= 3
  
  Possible race condition: Stock calculation wrong
  ```
- **Business Impact:** Overselling or understock
- **Risk Level:** CRITICAL
- **Fix Required:** YES - Use pessimistic locking or SERIALIZABLE transaction isolation

#### **EDGE CASE 5.1.2: Return Approved & Stock Updated But Refund Creation Fails**
- **Description:**
  1. Return APPROVED → Stock increased ✓
  2. Credit note creation fails ✗
  3. Refund missing but stock already restored
- **Current Status:** ⚠️ **CRITICAL RISK** - No transaction atomicity
- **Problem:** No rollback of stock if refund creation fails
- **Fix Required:** YES - Use database transactions
  ```csharp
  using var transaction = await _context.Database.BeginTransactionAsync();
  try
  {
      // Approve return & update stock
      return.Approve(...);
      
      // Create credit note
      var creditNote = CustomerCreditNote.Create(...);
      
      // Both succeed or both fail
      await transaction.CommitAsync();
  }
  catch
  {
      await transaction.RollbackAsync();
      throw;
  }
  ```

#### **EDGE CASE 5.1.3: Multiple Returns From Same Sales Order Cascade Effect**
- **Description:**
  - SO: 100 units, sold for $100 each
  - Return 1: 30 units ($3,000 credit) - APPROVED, stock updated
  - Return 2: 20 units ($2,000 credit) - filed but qty check may be wrong
  - Return 3: 60 units ($6,000 credit) - should reject (30 + 20 + 60 > 100)
- **Current Status:** ✓ **PROTECTED** - See edge case 3.1.2
- **But:** Must test with multiple concurrent returns

---

### 5.2 Warranty Claim → Refund → Stock Integration

#### **EDGE CASE 5.2.1: Warranty REPLACEMENT Service - Stock Movement Not Created**
- **Description:**
  - Warranty claim REPLACEMENT approved
  - Replacement item should be sent to customer
  - But stock movement (OUT) not created
- **Current Status:** ⚠️ **HIGH RISK** - Integration missing
- **Problem:** When claim moves to IN_PROGRESS with REPLACEMENT type:
  ```csharp
  // ✗ No stock movement created for replacement item
  // ✗ No tracking of which item shipped as replacement
  ```
- **Business Impact:** Stock inaccuracy
- **Fix Required:** YES
- **Suggested Logic:**
  ```csharp
  if (ServiceType == "REPLACEMENT" && Status == "IN_PROGRESS")
  {
      // Create stock movement: OUT (replacement item)
      // Create stock movement: IN (defective item received)
  }
  ```

#### **EDGE CASE 5.2.2: Warranty REFUND Service - Defective Item Not Marked As Returned**
- **Description:**
  - Warranty claim REFUND approved
  - Customer refunded but defective part not tracked as returned
  - Item could be re-sold as new
- **Current Status:** ⚠️ **CRITICAL RISK** - Lost control of inventory
- **Problem:** No mandatory defective item return for REFUND warranty
- **Fix Required:** YES
- **Suggested Logic:**
  ```csharp
  if (ServiceType == "REFUND")
  {
      // Must create SalesReturn or WarrantyReturn to track defective item
      // Cannot approve refund without return tracking
  }
  ```

---

### 5.3 Audit Trail & Reconciliation

#### **EDGE CASE 5.3.1: Order → Sale → Return → Refund Audit Trail Broken**
- **Description:** Cannot trace single item through entire lifecycle
- **Current Status:** ⚠️ **MEDIUM RISK** - Audit trail exists but fragmented
- **References:**
  - SalesReturn → SalesOrder (linked via SalesOrderId)
  - CustomerPayment → SalesOrder (optional via InvoiceId)
  - But no common linking to parent SO for multiple refunds/returns
- **Fix Required:** YES
- **Suggested Use Case Identifier:** Add SalesOrderId to CustomerPayment when created for SO return

#### **EDGE CASE 5.3.2: Credit Note Amount ≠ Return Line Items Total**
- **Description:**
  - Return 3 items: $50 + $30 + $20 = $100
  - Credit note issued for: $110
  - Discrepancy: $10
- **Current Status:** ⚠️ **HIGH RISK** - No validation
- **Problem:**
  ```csharp
  // No check: CreditNote.TotalAmount == Sum(ReturnLines.RefundAmount)
  ```
- **Business Impact:** Overstated or understated refunds
- **Fix Required:** YES
  ```csharp
  var calculatedRefund = salesReturn.LineItems
      .Sum(l => l.RefundAmount);
  
  if (Math.Abs(creditNote.TotalAmount - calculatedRefund) > 0.01m)
      throw new InvalidOperationException("Credit note amount doesn't match return items");
  ```

#### **EDGE CASE 5.3.3: Warranty Claim Cost vs Refund Tracking Inconsistent**
- **Description:**
  - Warranty claim: ServiceCost = $50 (technician labor + parts)
  - Refund: $100 (customer full refund)
  - No tracking of who bears the service cost loss
- **Current Status:** ⚠️ **MEDIUM RISK** - Financial reporting
- **Fix Required:** YES - Separate accounting GL entries:
  - Refund to customer (asset/cash decrease)
  - Service cost expense (expense account)
  - May need warranty reserve/allowance

---

## 6. CRITICAL ISSUES FOUND

### 🔴 CRITICAL (Must Fix Immediately)

| # | Issue | Location | Impact | Priority |
|---|-------|----------|--------|----------|
| 1 | Refund > Original Payment allowed | CustomerPaymentController | Financial loss | P0 |
| 2 | Multiple refunds for single payment not prevented | CustomerPaymentController | Over-refunding | P0 |
| 3 | Double refund: Return + Warranty Claim | SalesReturnController / WarrantyClaimsController | Financial loss | P0 |
| 4 | Race condition in concurrent returns | SalesReturnController | Inventory inaccuracy | P0 |
| 5 | Credit note applied to multiple invoices (race) | CustomerCreditNoteRepository | Over-crediting | P0 |
| 6 | Stock reversal missing on return rejection | SalesReturnController | Inventory overstated | P0 |
| 7 | Warranty expiry not validated on claim | WarrantyClaimsController | Unauthorized claims | P0 |
| 8 | Warranty claim allowed after order cancellation | WarrantyClaimsController | Claims on invalid orders | P0 |

### 🟠 HIGH (Fix in Current Sprint)

| # | Issue | Location | Impact | Priority |
|---|-------|----------|--------|----------|
| 1 | No state machine for payment status | CustomerPaymentController | Invalid transitions | P1 |
| 2 | Multiple active warranty claims per warranty | WarrantyClaimsController | Duplicate service | P1 |
| 3 | Warranty refund not linked to original sale | CustomerPayment entity | Audit trail broken | P1 |
| 4 | Refund after reconciliation allowed | CustomerPaymentController | Reconciliation breaks | P1 |
| 5 | Return condition not factored in refund | SalesReturnLine | Over-refunding damaged goods | P1 |
| 6 | Stock lot not created for returns | SalesReturnController | Traceability lost | P1 |
| 7 | FIFO not enforced in stock picking | StockManagementService | COGS wrong | P1 |
| 8 | No expiry check for stock lots | SalesOrderController | Risk of expired goods | P1 |
| 9 | Return stock moved before payment settled | SalesReturnController | Stock overstated if payment fails | P1 |

### 🟡 MEDIUM (Plan for Next Phase)

| # | Issue | Location | Impact | Priority |
|---|-------|----------|--------|----------|
| 1 | Multiple stock lots - FIFO enforcement | StockManagementService | Inventory selection | P2 |
| 2 | Credit note cancellation no reversal logic | CustomerCreditNote | Unreconciled balance | P2 |
| 3 | No automatic warranty expiry status update | WarrantyRegistration | Reporting incomplete | P2 |
| 4 | Stock transfer in-transit state missing | StockTransferController | Loss tracking | P2 |
| 5 | Currency conversion not validated | CustomerPaymentController | Exchange rate loss | P2 |

---

## 7. RECOMMENDATIONS

### 7.1 Immediate Actions (This Sprint)

1. **Implement Transaction Isolation for Critical Operations**
   ```
   Files affected:
   - SalesReturnController
   - CustomerPaymentController
   - CustomerCreditNoteRepository
   
   Action: Wrap in SERIALIZABLE transactions with row locking
   ```

2. **Add Comprehensive Validation for Refund Amounts**
   ```csharp
   // File: CustomerPayment.cs
   
   // Validate refund <= original payment
   // Validate cumulative refunds <= original payment
   // Validate currency consistency
   ```

3. **Implement State Machine for Payment Statuses**
   ```csharp
   // File: CustomerPayment.cs
   
   // Add ValidTransitions dictionary
   // Add CanTransitionTo() method
   // Use in all status change operations
   ```

4. **Create Return Stock Reversal Logic**
   ```csharp
   // File: SalesReturnController
   
   // When return REJECTED: Create reverse stock movement
   // When return CANCELLED: Reverse any stock movements made
   ```

5. **Enforce Warranty Expiry Validation**
   ```csharp
   // File: WarrantyClaim.cs
   
   // Add expiry check in SubmitForReview()
   // Add check in Approve()
   // Enforce before IN_PROGRESS
   ```

6. **Prevent Double Refunds (Return + Warranty)**
   ```csharp
   // File: WarrantyClaimsController
   
   // Check if warranty linked to returned SalesOrderLine
   // Reject if SalesReturn exists for same SO line
   ```

### 7.2 Medium-Term (Next Sprint)

1. **Implement Credit Note Application Locking**
   - Use pessimistic locking for concurrent applications
   - Test with concurrent threads

2. **Add Refund Amount Factoring for Item Condition**
   - UNOPENED: 100%
   - OPENED: 75%
   - DAMAGED: 30%

3. **Create Stock Lot Tracking for Returns**
   - Add StockLotId to SalesReturnLine
   - Track which lots are returned

4. **Implement FIFO for Stock Lot Selection**
   - Modify SO dispatch to use oldest lot first
   - Add configuration for FIFO/LIFO/WEIGHTED_AVG

5. **Add Warranty Status Update on Claim Transitions**
   - Warranty → CLAIMED when claim IN_PROGRESS
   - Warranty → VOID when return approved for same item

### 7.3 Long-Term (Architecture)

1. **Event-Driven Architecture for Complex Workflows**
   - Publish events: ReturnApproved, PaymentCompleted, etc.
   - Coordinate with event handlers (stock update, credit note creation, warranty status)
   - Enables transaction isolation & eventual consistency

2. **Saga Pattern for Long-Running Transactions**
   ```
   ReturnApprovalSaga:
   1. Approve return
   2. Update stock
   3. Create credit note
   4. Update warranty status
   5. Compensate if any step fails
   ```

3. **Audit Entity for Complete Audit Trail**
   - Log all state changes
   - Track who changed what and when
   - Enables full traceability

4. **Separate Read Models for Reporting**
   - Maintain denormalized views for reporting
   - Avoid complex joins for analytics

---

## 8. TESTING STRATEGY

### 8.1 Unit Tests Required

```csharp
// Test scenarios

[TestCase]
public void RefundAmount_ShouldNotExceedOriginalPayment()
{
    // Arrange
    var originalPayment = CustomerPayment.Create(customerId, 100);
    
    // Act
    var refund = CustomerPayment.Create(customerId, 150, "REFUND");
    
    // Assert - Should throw
    Assert.Throws<InvalidOperationException>(() => 
        refund.Validate(originalPayment));
}

[TestCase]
public void CumulativeRefunds_ShouldNotExceedOriginalPayment()
{
    // Test multiple refunds from single payment
}

[TestCase]
public void ReturnRejection_ShouldReverseStockMovement()
{
    // When return REJECTED after APPROVED
    // Stock should be reversed
}

[TestCase]
public void DoubleRefund_Return_And_Warranty_ShouldFail()
{
    // Can't file warranty claim on returned item
}

[TestCase]
public void StorageWarrantyClaim_ShouldFailAfterExpiry()
{
    // Warranty expired - claim should fail
}

[TestCase]
public void RefundWithDamagedCondition_ShouldRefundAtReducedRate()
{
    // DAMAGED condition should refund 30%
}

[TestCase]
public void CreditNoteApplication_ShouldHandleConcurrency()
{
    // Parallel applications should lock correctly
}
```

### 8.2 Integration Tests Required

```
1. Full Return Workflow
   - Create → Approve → Receive → Process → Credit Note → Payment

2. Full Warranty Claim Workflow
   - Register → Claim → Approve → Replacement/Refund → Payment

3. Cross-Flow Tests
   - Return then Warranty (should fail)
   - Warranty then Return (should handle)
   - Concurrent returns and sales

4. Stock Reconciliation
   - Return → Stock updated → Payment fails → Reconcile
   - Multiple returns with stock lots → FIFO honored

5. Refund Abort Scenarios
   - Return approved → Credit note fails → Rollback
   - Payment failed → Stock should reverse
```

### 8.3 Scenario-Based Testing

**Scenario 1: Happy Path**
```
Sales Order → Invoice → Return (UNOPENED) → Credit Note → Refund Payment
Result: Stock +5, Customer refunded 100%, Audit trail complete
```

**Scenario 2: Damaged Return**
```
Sales Order → Invoice → Return (DAMAGED) → Credit Note (30%) → Refund Payment
Result: Stock +5, Customer refunded 30%, Difference to company revenue
```

**Scenario 3: Concurrent Operations**
```
Return APPROVED (stock +5) ↓
          ↓
      Stock Movement ← → Simultaneous Sale (stock -3)
          ↓
          ↓
Credit Note Creation ← → Payment Processing
Result: All operations atomic, no race conditions
```

**Scenario 4: Warranty After Return**
```
Return Approved → Credit Note Created → Payment Processing
        ↓
Warranty Claim Filed (SHOULD FAIL)
Result: Reject claim with message referencing existing return
```

---

## CONCLUSION

The AutoPartShop system has **8 CRITICAL issues** that require immediate attention, particularly around:

1. **Financial Controls** - Double refunds, over-refunds, refunds exceeding payments
2. **Inventory Accuracy** - Race conditions, missing stock reversals, lot tracking
3. **Warranty-Refund Integration** - Missing business rule enforcement
4. **Transaction Atomicity** - Partial failures leaving system in inconsistent state

**Recommended Action:** 
- Address all P0 (CRITICAL) items in this sprint
- Schedule P1 (HIGH) items for next sprint
- Plan P2 (MEDIUM) items for future phases

Implementation of transaction management, state machines, and comprehensive validation will significantly improve system reliability and financial integrity.

---

**Document prepared for:** AutoPartShop Development Team  
**Review required by:** Entire Backend & QA Team  
**Implementation timeline:** Start immediately (P0 items)
