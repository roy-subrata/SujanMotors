# Customer Payment Feature - Quick Reference Guide

## Access URLs

### Development
- List: `http://localhost:4200/sales/customer-payments`
- Create: `http://localhost:4200/sales/customer-payments/new`
- Edit: `http://localhost:4200/sales/customer-payments/edit?id={paymentId}`
- View: `http://localhost:4200/sales/customer-payments/view?id={paymentId}`
- Summary: `http://localhost:4200/sales/customer-payments/summary/{customerId}`

## Component Selectors

```html
<app-customer-payment-list></app-customer-payment-list>
<app-customer-payment-form></app-customer-payment-form>
<app-customer-payment-summary></app-customer-payment-summary>
```

## Service Usage

```typescript
import { CustomerPaymentService } from '../services/customer-payment.service';

// Inject service
private paymentService = inject(CustomerPaymentService);

// Get paginated payments
this.paymentService.getCustomerPayments(1, 10, 'search').subscribe();

// Get payment by ID
this.paymentService.getCustomerPaymentById(id).subscribe();

// Get customer summary
this.paymentService.getCustomerPaymentSummary(customerId).subscribe();

// Create payment
this.paymentService.createCustomerPayment(request).subscribe();

// Confirm payment
this.paymentService.confirmPayment(id).subscribe();

// Refund payment
this.paymentService.refundPayment(id).subscribe();
```

## Form Structure

### Create Payment Request
```typescript
const request: CreateCustomerPaymentRequest = {
  customerId: string,              // Required
  invoiceId?: string,              // Optional
  paymentProviderId: string,       // Required
  amount: number,                  // Required (min: 0.01)
  paymentMethod: string,           // Required (CASH|CARD|UPI|NET_BANKING|CHEQUE)
  transactionNumber: string,       // Required
  referenceNumber: string,         // Optional
  paymentDate?: string,            // Optional (ISO date)
  notes: string                    // Optional
};
```

## Payment Statuses

| Status | Description | Severity | Actions Available |
|--------|-------------|----------|-------------------|
| PENDING | Initial state | secondary | Confirm, Cancel, Edit, Delete |
| PROCESSING | Being processed | info | Cancel |
| COMPLETED | Successfully completed | success | Refund, View |
| FAILED | Failed payment | danger | View |
| CANCELLED | Cancelled by user | danger | View |
| REFUNDED | Refunded to customer | warning | View |

## Context Menu Actions

### Available Actions by Status

```
PENDING:
  ✓ View Details
  ✓ View Summary
  ✓ Edit
  ✓ Confirm
  ✓ Cancel
  ✓ Delete

PROCESSING:
  ✓ View Details
  ✓ View Summary
  ✓ Cancel

COMPLETED:
  ✓ View Details
  ✓ View Summary
  ✓ Refund (if not reconciled)

FAILED/CANCELLED/REFUNDED:
  ✓ View Details
  ✓ View Summary
```

## Table Columns

1. Customer Name
2. Amount (formatted as INR)
3. Payment Date (dd-MMM-yyyy)
4. Payment Method
5. Status (badge)
6. Provider
7. Invoice # (or '-')
8. Reconciled (YES/NO tag)
9. Actions (context menu button)

## API Response Examples

### Payment Response
```json
{
  "id": "guid",
  "customerId": "guid",
  "customerName": "John Doe",
  "invoiceId": "guid",
  "invoiceNumber": "INV-001",
  "paymentProviderId": "guid",
  "providerName": "Stripe",
  "transactionNumber": "TXN-12345",
  "amount": 1000.00,
  "paymentFee": 20.00,
  "netAmount": 980.00,
  "currency": "INR",
  "paymentDate": "2025-12-11T00:00:00Z",
  "paymentMethod": "CARD",
  "status": "COMPLETED",
  "referenceNumber": "REF-001",
  "authorizationCode": "AUTH-123",
  "notes": "Payment for invoice INV-001",
  "settledDate": "2025-12-11T10:30:00Z",
  "settledBy": "admin",
  "isReconciled": false,
  "reconciledDate": null,
  "createdAt": "2025-12-11T09:00:00Z"
}
```

### Summary Response
```json
{
  "customerId": "guid",
  "customerName": "John Doe",
  "totalPaid": 5000.00,
  "totalFees": 100.00,
  "completedPayments": 5,
  "pendingPayments": 1,
  "failedPayments": 0,
  "lastPaymentDate": "2025-12-11T00:00:00Z",
  "lastPaymentAmount": 1000.00,
  "paymentHistory": [...]
}
```

## PrimeNG Configuration

### Required Modules
```typescript
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule } from 'primeng/contextmenu';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { DropdownModule } from 'primeng/dropdown';
import { CardModule } from 'primeng/card';
import { SkeletonModule } from 'primeng/skeleton';
```

### Required Services
```typescript
import { MessageService, ConfirmationService } from 'primeng/api';

providers: [MessageService, ConfirmationService]
```

## Styling Classes

### Container Classes
- `.container` - Main container
- `.header` - Header section
- `.table-container` - Table wrapper
- `.form-card` - Form card
- `.metrics-grid` - Metrics grid

### Header Classes
- `.header-title` - Main title
- `.header-subtitle` - Subtitle text
- `.toolbar` - Toolbar container
- `.search-container` - Search input wrapper
- `.btn-create` - Create button

### Utility Classes
- `.text-right` - Right align text
- `.text-center` - Center align text
- `.loading-state` - Loading skeleton
- `.error-state` - Error message

## Currency Formatting

```typescript
formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR'
  }).format(value);
}
```

## Date Formatting

```typescript
formatDate(date: string): string {
  return new Date(date).toLocaleDateString('en-IN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  });
}
```

## Validation Messages

```typescript
// Required field
'Please fill in all required fields'

// Success messages
'Payment created successfully'
'Payment updated successfully'
'Payment confirmed successfully'
'Payment cancelled successfully'
'Payment refunded successfully'
'Payment deleted successfully'

// Error messages
'Failed to load customer payments'
'Failed to create customer payment'
'Failed to update customer payment'
'Failed to confirm payment'
'Failed to cancel payment'
'Failed to refund payment'
'Failed to delete payment'
```

## Common Tasks

### Navigate to Create Form
```typescript
this.router.navigate(['/sales/customer-payments/new']);
```

### Navigate to Edit Form
```typescript
this.router.navigate(['/sales/customer-payments/edit'], {
  queryParams: { id: payment.id }
});
```

### Navigate to Summary
```typescript
this.router.navigate(['/sales/customer-payments/summary', customerId]);
```

### Show Success Toast
```typescript
this.messageService.add({
  severity: 'success',
  summary: 'Success',
  detail: 'Operation completed successfully'
});
```

### Show Error Toast
```typescript
this.messageService.add({
  severity: 'error',
  summary: 'Error',
  detail: 'Operation failed'
});
```

### Confirm Dialog
```typescript
this.confirmationService.confirm({
  message: 'Are you sure?',
  header: 'Confirm',
  icon: 'pi pi-exclamation-triangle',
  accept: () => {
    // Perform action
  }
});
```

## Testing

### Unit Test Example
```typescript
describe('CustomerPaymentListComponent', () => {
  it('should load payments on init', () => {
    // Test implementation
  });

  it('should filter payments by search term', () => {
    // Test implementation
  });

  it('should confirm payment', () => {
    // Test implementation
  });
});
```

### E2E Test Example
```typescript
describe('Customer Payments', () => {
  it('should create new payment', () => {
    cy.visit('/sales/customer-payments');
    cy.get('.btn-create').click();
    // Fill form and submit
  });
});
```

## Troubleshooting

### Issue: Payments not loading
**Solution**: Check API endpoint is running and returns correct data format

### Issue: Form validation not working
**Solution**: Ensure FormControl names match form field names

### Issue: Context menu not showing
**Solution**: Check ViewChild reference and template reference variable

### Issue: Currency not formatting
**Solution**: Verify locale settings and Intl support in browser

### Issue: Routing not working
**Solution**: Ensure routes are properly imported in sales.routes.ts

## Best Practices

1. Always handle errors in subscribe
2. Use takeUntil for subscription cleanup
3. Show loading states during API calls
4. Validate forms before submission
5. Use toast for user feedback
6. Use confirmation dialogs for destructive actions
7. Format currency and dates consistently
8. Keep components focused and single-purpose
9. Share common logic in services
10. Follow reactive programming patterns

## Resources

- PrimeNG Documentation: https://primeng.org
- Angular Documentation: https://angular.io
- RxJS Documentation: https://rxjs.dev
- Supplier Payment Reference: `src/AutoPartShop.WebApp/src/app/features/procurement/supplier-payment/`
