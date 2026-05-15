# Payment Architecture Fix - ONE Source of Truth

## 🔴 Problem Identified

**Current system has TWO payment tracking mechanisms:**

1. **InvoicePayment** - Created by `Invoice.RecordPayment()`
2. **CustomerPayment** - Created separately, optionally linked to invoice

**This causes:**
- ❌ Data inconsistency (payments recorded twice or not at all)
- ❌ Wrong balance calculations
- ❌ Confusion about which is the "real" payment
- ❌ Customer pays ₹32,000 but system shows ₹22,000

## ✅ Solution: CustomerPayment as Single Source of Truth

### Architecture Change

```
BEFORE (BROKEN):
Invoice
  ├── AmountPaid (stored field) ❌
  ├── RecordPayment() method ❌
  └── InvoicePayment collection ❌

CustomerPayment (separate, not used for calculations) ❌

AFTER (FIXED):
CustomerPayment (MASTER)
  ├── CustomerId
  ├── InvoiceId (optional - can pay to invoice or customer account)
  ├── Amount
  ├── Status (PENDING, COMPLETED, FAILED)
  └── PaymentDate

Invoice
  ├── CustomerPayments collection (navigation) ✔️
  ├── AmountPaid (computed from CustomerPayments) ✔️
  ├── OutstandingAmount (computed) ✔️
  └── NO InvoicePayment! ✔️
```

### Implementation Steps

#### Step 1: Update Invoice Entity

**Remove:**
- `AmountPaid` as stored property
- `RecordPayment()` method
- `InvoicePayment` collection

**Add:**
- Computed `AmountPaid` property
- Computed `TotalAmount` property (for clarity)
- Invoice status update method based on payments

```csharp
// Invoice.cs - FIXED VERSION
public class Invoice : AuditableEntity
{
    // ... existing fields ...

    public decimal TotalAmount => GrandTotal; // Alias for clarity

    // COMPUTED from CustomerPayments
    public decimal AmountPaid =>
        CustomerPayments?
            .Where(p => p.Status == "COMPLETED")
            .Sum(p => p.Amount) ?? 0;

    // COMPUTED based on AmountPaid
    public decimal OutstandingAmount => TotalAmount - AmountPaid;

    // Navigation property
    public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();

    // Update status based on payments
    public void UpdatePaymentStatus()
    {
        if (AmountPaid <= 0)
        {
            Status = IsOverdue ? "OVERDUE" : "ISSUED";
        }
        else if (AmountPaid >= TotalAmount)
        {
            Status = "PAID";
        }
        else
        {
            Status = IsOverdue ? "OVERDUE" : "PARTIALLY_PAID";
        }
    }
}
```

#### Step 2: Delete or Deprecate InvoicePayment

**Option A: Complete Deletion**
1. Remove `InvoicePayment.cs` entity
2. Remove from `DbContext`
3. Create migration to drop table
4. Update any code referencing it

**Option B: Migration Path (Safer)**
1. Mark `InvoicePayment` as `[Obsolete]`
2. Create data migration to convert to `CustomerPayment`
3. Keep table for historical data

#### Step 3: Update Payment Flow

**OLD (Wrong) Flow:**
```csharp
// ❌ WRONG - Creates orphaned payment
invoice.RecordPayment(amount);
```

**NEW (Correct) Flow:**
```csharp
// ✔️ CORRECT - Single source of truth
var payment = CustomerPayment.Create(
    customerId: invoice.SalesOrder.CustomerId,
    paymentProviderId: providerId,
    amount: paymentAmount,
    paymentMethod: "BANK_TRANSFER",
    transactionNumber: "TXN-12345"
);

// Link to invoice
payment.LinkToInvoice(invoice.Id);

// Mark as completed
payment.MarkAsCompleted();

// Save payment
await customerPaymentRepository.AddAsync(payment);

// Update invoice status based on payments
invoice.UpdatePaymentStatus();
await invoiceRepository.UpdateAsync(invoice);
```

#### Step 4: Handle Overpayment

```csharp
public class Invoice
{
    public decimal CreditBalance => AmountPaid > TotalAmount
        ? AmountPaid - TotalAmount
        : 0;

    public bool HasCredit => CreditBalance > 0;
}

public class Customer
{
    // Track customer account balance
    public decimal AccountBalance =>
        CustomerPayments
            .Where(p => p.Status == "COMPLETED" && p.InvoiceId == null)
            .Sum(p => p.Amount)
        + Invoices.Sum(i => i.CreditBalance);
}
```

#### Step 5: Update Balance Calculations

**Customer Outstanding Balance:**
```csharp
public class Customer
{
    public decimal TotalInvoiced => Invoices.Sum(i => i.TotalAmount);

    public decimal TotalPaid =>
        CustomerPayments
            .Where(p => p.Status == "COMPLETED")
            .Sum(p => p.Amount);

    public decimal OutstandingBalance => TotalInvoiced - TotalPaid;

    public decimal OverdueAmount =>
        Invoices
            .Where(i => i.IsOverdue && i.OutstandingAmount > 0)
            .Sum(i => i.OutstandingAmount);
}
```

#### Step 6: Update API Endpoints

**Invoice Payment Endpoint:**
```csharp
[HttpPost("invoices/{invoiceId:guid}/pay")]
public async Task<IActionResult> PayInvoice(
    Guid invoiceId,
    PayInvoiceRequest request)
{
    var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
    if (invoice is null) return NotFound();

    // Create CustomerPayment (MASTER record)
    var payment = CustomerPayment.Create(
        customerId: invoice.SalesOrder.CustomerId,
        paymentProviderId: request.PaymentProviderId,
        amount: request.Amount,
        paymentMethod: request.PaymentMethod,
        transactionNumber: request.TransactionNumber
    );

    // Link to invoice
    payment.LinkToInvoice(invoiceId);

    // Complete payment
    payment.MarkAsCompleted();

    // Save payment
    await _customerPaymentRepository.AddAsync(payment);

    // Reload invoice with payments and update status
    invoice = await _invoiceRepository.GetByIdWithPaymentsAsync(invoiceId);
    invoice.UpdatePaymentStatus();
    await _invoiceRepository.UpdateAsync(invoice);

    return Ok(new {
        PaymentId = payment.Id,
        InvoiceStatus = invoice.Status,
        AmountPaid = invoice.AmountPaid,
        Outstanding = invoice.OutstandingAmount
    });
}
```

### Data Migration

**Convert existing InvoicePayments to CustomerPayments:**

```sql
-- Migration: Convert InvoicePayment to CustomerPayment
INSERT INTO CustomerPayments (
    Id, CustomerId, InvoiceId, Amount, PaymentDate,
    PaymentMethod, Status, TransactionNumber, ReferenceNumber,
    CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
)
SELECT
    NEWID(),
    so.CustomerId,
    ip.InvoiceId,
    ip.Amount,
    ip.PaymentDate,
    ip.PaymentMethod,
    'COMPLETED',
    ip.ReferenceNumber,
    ip.ReferenceNumber,
    ip.CreatedBy,
    ip.CreatedDate,
    ip.ModifiedBy,
    ip.ModifiedDate
FROM InvoicePayments ip
INNER JOIN Invoices i ON ip.InvoiceId = i.Id
INNER JOIN SalesOrders so ON i.SalesOrderId = so.Id
WHERE NOT EXISTS (
    SELECT 1 FROM CustomerPayments cp
    WHERE cp.InvoiceId = ip.InvoiceId
    AND cp.Amount = ip.Amount
    AND cp.PaymentDate = ip.PaymentDate
);

-- After migration completes and verification:
-- DROP TABLE InvoicePayments;
```

### Verification Checklist

✅ **Sanity Check:** "If I delete CustomerPayments table, can I still calculate everything?"
- Answer should be: **NO** - CustomerPayments is the ONLY source

✅ **Payment Flow Check:**
1. Create CustomerPayment → ✔️ ONE record created
2. Link to Invoice → ✔️ InvoiceId set
3. Invoice AmountPaid updates → ✔️ Computed from CustomerPayments
4. Customer balance updates → ✔️ Computed from CustomerPayments

✅ **Balance Accuracy:**
- Invoice Outstanding = Invoice Total - SUM(CustomerPayments WHERE Status=COMPLETED)
- Customer Balance = SUM(Invoice Totals) - SUM(CustomerPayments WHERE Status=COMPLETED)
- Both calculations use SAME data source

✅ **Multiple Payments:**
- Invoice ₹23,000
- Payment 1: ₹22,000 (CustomerPayment record)
- Payment 2: ₹10,000 (CustomerPayment record)
- Total Paid: ₹32,000 ✔️
- Outstanding: -₹9,000 (credit) ✔️
- Customer credit balance: ₹9,000 ✔️

## Summary

**Key Changes:**
1. ❌ **DELETE** `InvoicePayment` entity (or mark obsolete)
2. ✔️ **USE** `CustomerPayment` as single source of truth
3. ✔️ **COMPUTE** `Invoice.AmountPaid` from linked `CustomerPayments`
4. ✔️ **COMPUTE** all balances from `CustomerPayments`
5. ✔️ **HANDLE** overpayments as credit balance

**Benefits:**
- ✅ Single source of truth
- ✅ No data duplication
- ✅ Accurate balance calculations
- ✅ Support for partial payments, overpayments, advances
- ✅ Clear audit trail
- ✅ Simpler code
