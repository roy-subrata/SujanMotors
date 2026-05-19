# Credit Note Integration Guide

## Overview

Credit notes are created when you issue a credit for a purchase return. They can be applied as credit against future purchase orders from the same supplier.

## Complete Flow

### 1. Create Purchase Return
1. Go to **Procurement > Purchase Returns**
2. Create a new return for a PO
3. Approve the return
4. Mark as Returned (goods sent back)
5. Mark as Received (supplier confirmed receipt)

### 2. Issue Credit Note
1. On the purchase return, click **Issue Credit Note**
2. Enter the credit amount (can be partial or full refund amount)
3. System creates:
   - `CreditNote` entity (tracked separately)
   - `SupplierPayment` record (ADVANCE type for audit trail)
   - Links credit note to the purchase return

### 3. Apply Credit to Purchase Order

#### Option A: Using the Apply Credit Component

Add this to your PO form template:

```html
<!-- Add after supplier selection and PO totals section -->
<app-apply-credit-notes
    [supplierId]="form.get('supplierId')?.value"
    [purchaseOrderId]="poId"
    [poTotalAmount]="form.get('totalAmount')?.value || 0"
    [poPaidAmount]="form.get('paidAmount')?.value || 0"
    (creditApplied)="onCreditApplied($event)">
</app-apply-credit-notes>
```

Add to component imports:
```typescript
import { ApplyCreditNotesComponent } from '../purchase-credits/apply-credit-notes.component';

@Component({
    imports: [
        // ... existing imports
        ApplyCreditNotesComponent
    ]
})
```

Add handler in component:
```typescript
onCreditApplied(amount: number): void {
    // Update PO outstanding amount
    const currentTotal = this.form.get('totalAmount')?.value || 0;
    const currentPaid = this.form.get('paidAmount')?.value || 0;
    const outstanding = currentTotal - currentPaid;
    
    // Refresh PO data
    if (this.poId) {
        this.loadPurchaseOrder(this.poId);
    }
}
```

#### Option B: Manual Credit Application

Use the credit note service directly:

```typescript
import { CreditNoteService } from '../services/credit-note.service';

// Get available credits for supplier
this.creditNoteService.getAvailableCredits(supplierId).subscribe(credits => {
    // credits is array of available credit notes
});

// Apply credit to PO
this.creditNoteService.applyCredit({
    creditNoteId: selectedCreditId,
    purchaseOrderId: poId,
    amountToApply: amount
}).subscribe(response => {
    // Credit applied successfully
});
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/creditnote/supplier/{id}` | Get all credit notes for supplier |
| GET | `/api/creditnote/supplier/{id}/available` | Get available (usable) credits |
| GET | `/api/creditnote/supplier/{id}/total-available` | Get total available credit amount |
| GET | `/api/creditnote/list?supplierId=&status=&pageNumber=&pageSize=` | Paginated list |
| GET | `/api/creditnote/{id}` | Get credit note by ID |
| POST | `/api/creditnote/apply` | Apply credit to PO |
| PATCH | `/api/creditnote/{id}/cancel?reason=` | Cancel unused credit |

## Data Model

### CreditNote Entity
```csharp
{
    Id: Guid,
    CreditNoteNumber: string (e.g., "CN-20260412123456"),
    SupplierId: Guid,
    PurchaseReturnId: Guid?,
    PurchaseOrderId: Guid?,  // PO this credit was applied to
    TotalAmount: decimal,
    UsedAmount: decimal,
    AvailableAmount: decimal,  // Computed: Total - Used
    Status: string,  // AVAILABLE, PARTIALLY_USED, FULLY_USED, EXPIRED, CANCELLED
    IssueDate: DateTime,
    ExpiryDate: DateTime?,
    Notes: string
}
```

### Supplier Ledger Impact
```
Supplier Balance = Total Purchases - Total Payments - Total Refunds
Available Credit = Advance Payments Remaining + Credit Notes Available
```

Credit notes appear in the supplier ledger as available credit, reducing the net payable amount.

## Frontend Files

| File | Purpose |
|------|---------|
| `credit-note.service.ts` | API service for credit note operations |
| `apply-credit-notes.component.ts` | Component for applying credits to POs |
| `apply-credit-notes.component.html` | Template for credit application UI |
| `apply-credit-notes.component.css` | Styles for credit application UI |

## Example: Full PO Form Integration

```html
<!-- Purchase Order Form -->
<div class="po-form">
    <!-- Existing PO fields -->
    <app-po-form-fields></app-po-form-fields>
    
    <!-- Credit Application Section -->
    <div class="credit-section" *ngIf="form.get('supplierId')?.value && poId">
        <app-apply-credit-notes
            [supplierId]="form.get('supplierId')?.value"
            [purchaseOrderId]="poId"
            [poTotalAmount]="form.get('totalAmount')?.value || 0"
            [poPaidAmount]="form.get('paidAmount')?.value || 0"
            (creditApplied)="onCreditApplied($event)">
        </app-apply-credit-notes>
    </div>
</div>
```
