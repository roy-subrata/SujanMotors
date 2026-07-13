# FLOW DIAGRAMS WITH EDGE CASE ANNOTATIONS

**Document:** AutoPartShop Business Flows Analysis  
**Date:** April 23, 2026

---

## 1. SALES RETURN → STOCK → REFUND FLOW

### Happy Path (Current Implementation)
```
┌─────────────────────────────────────────────────────────────────┐
│ CREATE SALES RETURN                                             │
│ ✓ Check return qty ≤ ordered qty                               │
│ ✓ Check cumulative returns                                      │
│ Status: PENDING                                                 │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ APPROVE RETURN                                                  │
│ ✓ Transition PENDING → APPROVED                                 │
│ Status: APPROVED                                                │
│ ApprovedDate: now                                               │
│ ApprovedBy: current user                                        │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ CREATE STOCK MOVEMENT (IN)         ⚠️ EDGE CASE 3.2.1           │
│ ✓ Add $stock += quantity                                        │
│ ✓ Create movement record                                        │
│ Status: RECEIVED                                                │
│ ⚠️ ISSUE: Done before payment confirmed!                       │
│ ⚠️ If payment fails later, stock stays increased               │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ MARK AS RECEIVED                                                │
│ Status: RECEIVED                                                │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ PROCESS RETURN                                                  │
│ Status: PROCESSED                                               │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ CREATE CREDIT NOTE                                              │
│ ✓ Issue credit note for full return amount                      │
│ ✓ CreditNote.Status = AVAILABLE                                 │
│ Amount: Sum(return line items)                                  │
│ ⚠️ ISSUE: Amount not validated vs line items total              │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ CREATE REFUND PAYMENT                                           │
│ ✓ Create payment for refund amount                              │
│ Status: PENDING                                                 │
│ Method: CASH_REFUND or STORE_CREDIT                             │
│ ⚠️ CRITICAL EDGE CASES:                                         │
│ ✗ Amount not validated                                          │
│ ✗ No check for cumulative refunds                               │
│ ✗ May exceed original payment                                   │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ PAYMENT PROCESSING                                              │
│ Status: PROCESSING → COMPLETED                                  │
│ ✓ Payment settled                                               │
│ ⚠️ ISSUE: Can still refund after reconciliation                 │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ ✅ FINAL STATE                                                  │
│ Return: PROCESSED                                               │
│ Stock: Increased by removed qty                                 │
│ Payment: COMPLETED/RECONCILED                                   │
│ Audit Trail: Complete                                           │
└─────────────────────────────────────────────────────────────────┘
```

### Failure Path - Return Rejection
```
┌──────────────────────────────┐
│ RETURN APPROVED              │ ─→ Stock += 5 ✓
│ Status: APPROVED             │
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────┐
│ RETURN REJECTED              │
│ Status: REJECTED             │
│ ⚠️ CRITICAL ISSUE:           │
│ Stock NOT REVERSED!          │ ─→ Stock STILL += 5 ✗✗✗
│ Inventory overstated         │
└──────────────────────────────┘

FIX REQUIRED:
┌──────────────────────────────┐
│ ON REJECTION:               │
│ 1. Create reverse stock OUT │
│ 2. Decrease stock level     │
│ 3. Stock back to original   │
└──────────────────────────────┘
```

---

## 2. WARRANTY CLAIM → REPLACEMENT/REFUND FLOW

### Warranty Registration (Sale Time)
```
┌────────────────────────────────┐
│ SALES ORDER CREATED            │
│ Item: Widget A, Qty: 1         │
│ Price: $100, Warranty: 12 mo   │
└──────────────┬─────────────────┘
               │
               ▼
┌────────────────────────────────┐
│ CREATE WARRANTY REGISTRATION   │
│ ✓ WarrantyNumber: WR-2026-XXXXX│
│ ✓ ExpiryDate: SaleDate + 12mo  │
│ Status: ACTIVE                 │
│ CertificateNumber: Generated   │
└────────────────────────────────┘
```

### Warranty Claim - Problem Path
```
┌──────────────────────────────────────────────────┐
│ CUSTOMER FILES WARRANTY CLAIM                    │
│ ClaimNumber: WC-2026-XXXXX                       │
│ IssueDescription: "Widget doesn't work"         │
│ ServiceType: REPAIR / REPLACEMENT / REFUND       │
│ Status: PENDING                                  │
│ ⚠️ EDGE CASES:                                  │
│ 🔴 After expiry? → Should FAIL (NOT CHECKED)    │
│ 🔴 Multiple active claims? → May exist (NO CHK) │
│ 🔴 Item already returned? → May conflict        │
└────────────────┬─────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────┐
│ SUBMIT FOR REVIEW                                │
│ Status: UNDER_REVIEW                             │
│ 🔴 CRITICAL: Warranty expiry NOT checked here    │
└────────────────┬─────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────┐
│ REVIEW & APPROVE CLAIM                           │
│ Status: APPROVED                                 │
│ ApprovedBy: Manager                              │
│ ⚠️ Warranty status NOT updated to CLAIMED        │
└────────────────┬─────────────────────────────────┘
                 │
         ┌───────┴────────┬────────────┐
         │                │            │
         ▼                ▼            ▼
      REPAIR         REPLACEMENT      REFUND
```

### Service Type: REFUND Path
```
┌────────────────────────────────────────┐
│ REFUND CLAIM APPROVED                  │
│ ServiceType: REFUND                    │
│ Status: APPROVED                       │
│ ServiceCost: $0 (no repair labor)      │
└──────────────┬───────────────────────────┘
               │
               ▼
┌────────────────────────────────────────┐
│ ASSIGN TECHNICIAN (or skip)            │
│ Status: IN_PROGRESS                    │
│ ⚠️ Issue: Warranty status NOT updated   │
│         Should be: CLAIMED              │
└──────────────┬───────────────────────────┘
               │
               ▼
┌────────────────────────────────────────┐
│ ⚠️ CRITICAL: WHERE IS DEFECTIVE ITEM?  │
│ No stock movement created for:         │
│ - Defective item return (OUT)          │
│ - Replacement item shipment (OUT)      │
│ Lost inventory control!                │
└──────────────┬───────────────────────────┘
               │
               ▼
┌────────────────────────────────────────┐
│ CREATE REFUND PAYMENT                  │
│ Status: PENDING                        │
│ Amount: Original sale price            │
│ 🔴 ISSUE: ServiceCost not deducted     │
│ 🔴 Issue: Not linked to warranty claim │
└──────────────┬───────────────────────────┘
               │
               ▼
┌────────────────────────────────────────┐
│ PROCESS REFUND                         │
│ Status: COMPLETED                      │
│ Customer refunded full amount          │
└────────────────────────────────────────┘
```

---

## 3. CRITICAL: SALES RETURN + WARRANTY CONFLICT

### Edge Case: Double Refund Possible
```
TIMELINE:
═════════════════════════════════════════════

Day 1: Customer returns item
  ┌──────────────────────┐
  │ Sales Return Created │
  │ Qty: 1 (Widget A)   │
  │ Status: PENDING     │
  └──────────┬───────────┘
             │ (APPROVED)
             ▼
  ┌──────────────────────┐
  │ Stock += 1           │
  │ Status: RECEIVED     │
  └──────────┬───────────┘
             │ (PROCESSED)
             ▼
  ┌──────────────────────┐
  │ Credit Note Issued   │
  │ Amount: $100        │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Refund Payment $100  │
  │ Status: COMPLETED   │
  └──────────────────────┘

Day 2: Customer ALSO files warranty claim
  ┌──────────────────────┐
  │ Warranty Claim Filed │
  │ ⚠️ NO CHECK for:    │
  │    existing return   │
  │ Status: PENDING     │
  └──────────┬───────────┘
             │ (APPROVED)
             ▼
  ┌──────────────────────┐
  │ Warranty Refund $100 │
  │ Status: COMPLETED   │
  └──────────────────────┘

RESULT: Customer refunded $200 for ONE item! ✗✗✗
        Company loss: $100

FIX:
In WarrantyClaimsController.Create():
  var existingReturn = await _salesReturnRepository
    .GetBySalesOrderLineAsync(warranty.SalesOrderLineId);
  
  if (existingReturn?.Status == "PROCESSED")
    throw new InvalidOperationException(
      "Item already returned via SO-XXXXX");
```

---

## 4. PAYMENT REFUND - VALIDATION FLOW

### Current: No Validation (BROKEN)
```
Original Payment: $100
  │
  ├─→ Refund 1: $70 ✓ (Should work)
  │
  ├─→ Refund 2: $50 ✗ (70 + 50 = 120 > 100)
  │             But system allows it!
  │
  ├─→ Refund 3: $100 ✗ (70 + 50 + 100 = 220 > 100)
  │              But system allows it!
  │
  └─→ Total refunded: $220 for $100 payment ✗✗✗
      Financial loss: $120
```

### Proposed: With Validation (FIXED)
```
Original Payment: $100

Refund 1: Check 70 ≤ 100 ✓ → ALLOWED
  Cumulative: $70, Remaining: $30

Refund 2: Check 50 ≤ 30 ✗ → REJECTED
  Message: "Only $30 available balance"

Refund 2b: Check 30 ≤ 30 ✓ → ALLOWED
  Cumulative: $100, Remaining: $0

Refund 3: Check 10 ≤ 0 ✗ → REJECTED
  Message: "No balance remaining"
```

---

## 5. CREDIT NOTE APPLICATION - RACE CONDITION

### Concurrent Application Problem
```
Credit Note: $100 (AVAILABLE)

Thread 1                          Thread 2
═════════════════════════════════════════════════

Check Balance
  $100 available ✓
                                 Check Balance
                                   $100 available ✓
Apply $100
  UsedAmount += 100 ✓
  Save ✓
                                 Apply $100
                                   UsedAmount += 100 ✓
                                   Save ✓

Final State:
  Used: $200
  Available: -$100

RESULT: Over-credited $100! ✗✗✗

FIX: Use SELECT ... FOR UPDATE
  WITH (SERIALIZABLE isolation level)
  to prevent concurrent applications
```

---

## 6. STOCK LEVEL - BASE UNIT COMPLEXITY

### Multi-Unit Return Scenario
```
Part: Widget A
  Base Unit: Piece (1 pc)
  Sales Unit: Dozen (12 pcs)
  
Sales Order:
  - Line 1: 5 Dozen (= 60 Pieces) @ $1/piece

Customer Return:
  - Return 2 Dozen (= 24 Pieces) @ $1/piece

Stock Movement:
  ✓ Quantity: 24 (pieces - display)
  ✓ QuantityInBaseUnit: 24 (pieces - calculation)
  ✓ Created correctly ✓

Edge Case: What if conversion wrong?
  ✗ Quantity: 2 (displayed as dozen)
  ✗ QuantityInBaseUnit: 24 (say wrongly 19)
  
  Result: Stock movement inconsistent
  Inventory audit fails
```

---

## 7. STOCK MOVEMENT TYPE AMBIGUITY

### Current Problem
```
StockMovement.MovementType = "RETURN"

What does RETURN mean?
  │
  ├─→ Sales Return (customer returned item)
  │    Effect: Stock += qty
  │    Reason: Defective, wrong item, etc.
  │
  ├─→ Purchase Return (return to supplier)
  │    Effect: Stock -= qty
  │    Reason: Defective, excess stock, etc.
  │
  ├─→ Warranty Return (defective via warranty)
  │    Effect: Stock += qty (received back)
  │    Reason: Covered under warranty
  │
  └─→ Damage/Loss
       (Can't tell from Movement Type)
       
RESULT: Audit trail unclear ✗
```

### Proposed Solution
```
Use Specific Types:
  - "SALES_RETURN"     → Customer returns
  - "PURCHASE_RETURN"  → Return to supplier
  - "WARRANTY_RETURN"  → Warranty defective received
  - "DAMAGE_WRITE_OFF" → Damaged/unsellable
  - "LOSS_ADJUSTMENT"  → Inventory loss
  - "TRANSFER_OUT"     → To another warehouse
  - "TRANSFER_IN"      → From another warehouse
  
Now audit trail is clear ✓
```

---

## 8. STOCK LOT SELECTION - FIFO ISSUE

### Current: Random Selection (WRONG)
```
Available Stock Lots:
  Lot 1 (2025-01-01): 50 pcs @ $10/pc = $500
  Lot 2 (2025-02-01): 30 pcs @ $12/pc = $360
  (Oldest)            (Newest)

Dispatch Order: 60 pcs

Random Selection:
  Scenario A:
    Pick Lot 2 all (30) + Lot 1 (30) → COGS = 360 + 300 = $660 ✗ (Wrong)
  
  Scenario B:
    Pick Lot 1 all (50) + Lot 2 (10) → COGS = 500 + 120 = $620 ✗ (Wrong)

RESULT: COGS calculation inconsistent ✗
        Financial reporting unreliable
```

### Proposed: FIFO (CORRECT)
```
FIFO Selection (Always):
  Pick Lot 1 all (50) @ $10/pc = $500
  Pick Lot 2 (10) @ $12/pc = $120
  
  Total COGS: $620
  (Lowest cost inventory flow)
  
Configuration:
  DEFAULT: FIFO
  OPTIONAL: LIFO, WEIGHTED_AVG (if needed)
```

---

## 9. PAYMENT STATUS TRANSITIONS

### Current: No State Machine (BROKEN)
```
Any transition allowed:
  PENDING ←→ PROCESSING
  PENDING ←→ COMPLETED
  COMPLETED → PENDING (WRONG!)
  FAILED ←→ COMPLETED (WRONG!)
  SETTLED → CANCELLED (WRONG!)
  
Result: Inconsistent state ✗
```

### Proposed: State Machine (FIXED)
```
Valid Transitions:
  PENDING 
    → PROCESSING ✓
    → CANCELLED ✓
    
  PROCESSING 
    → COMPLETED ✓
    → FAILED ✓
    → CANCELLED ✓
    
  COMPLETED 
    → REFUNDED ✓ (refund initiated)
    → SETTLED ✓ (confirmation received)
    
  FAILED 
    → PENDING ✓ (retry)
    → CANCELLED ✓
    
  REFUNDED 
    → SETTLED ✓
    → CANCELLED ✓
    
  SETTLED
    (Final state, no transitions)
    
  CANCELLED
    (Final state, no transitions)

Prevent invalid transitions ✓
```

---

## 10. IDEAL RECOMMENDED FLOW (WITH ALL FIXES)

```
        SALES ORDER
            │
            ├─ Link to: SO#{number}, Customer, Invoice
            │
            ▼
        SALES ORDER FULFILLED
            │
            └─ If needed: Item returned within 30 days?
            
            ▼ YES
            
        CREATE SALES RETURN
        ✓ Validate qty ≤ ordered
        ✓ Validate cumulative returns
        ✓ Validate no warranty claim exists
        ✓ Status: PENDING
        
            │
            ▼
        
        APPROVE RETURN
        ✓ Set ApprovedBy, ApprovedDate
        ✓ Condition check (UNOPENED/OPENED/DAMAGED)
        ✓ Status: APPROVED
        
            │
            ▼
        
        RECEIVE RETURN
        ✓ Create StockLot for returns (traceability)
        ✓ Status: RECEIVED
        │ Note: DO NOT update stock yet
        │
            ▼
        
        PROCESS RETURN
        ✓ Check condition → apply refund factor
        ✓ Calculate refund: qty × price × conditionFactor
        ✓ Create Credit Note
        ✓ Status: PROCESSED
        
            │
            ▼
        
        CREATE REFUND PAYMENT
        ✓ Amount = credit_note_amount
        ✓ Link to SalesReturn
        ✓ Status: PENDING
        ✓ Validate: amount ≤ original invoice amount
        
            │
            ▼
        
        PROCESS PAYMENT
        ✓ Status: PROCESSING
        
            │
            ▼
        
        COMPLETE PAYMENT
        ✓ Status: COMPLETED
        ✓ Mark: IsReconciled = true (if settled)
        
            │
            ▼
        
        UPDATE STOCK (NOW!)
        ✓ Create stock movement: Type=SALES_RETURN
        ✓ Stock += qty
        ✓ Link to StockLot created earlier
        
            │
            ▼
        
        ✅ FINAL STATE
        Return: PROCESSED
        Payment: COMPLETED
        Stock: Updated
        Audit Trail: Complete
        
        Refund CANNOT be issued again for same SO line
        
```

---

**Visual flows show critical gaps in current implementation.**  
**All identified edge cases must be addressed for financial & inventory integrity.**
