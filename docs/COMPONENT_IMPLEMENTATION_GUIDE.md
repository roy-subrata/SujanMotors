# Component Implementation Guide

## Quick Reference for Creating Remaining Components

### Template Structure for Form Dialog

```typescript
// Component File: suppliers-form-dialog.component.ts
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-suppliers-form-dialog',
  templateUrl: './suppliers-form-dialog.component.html',
  styleUrls: ['./suppliers-form-dialog.component.css']
})
export class SuppliersFormDialogComponent {
  @Input() visible = false;
  @Input() supplier: SupplierResponse | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() submitted = new EventEmitter<SupplierResponse>();

  form: FormGroup;
  isEditing = false;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      name: ['', [Validators.required]],
      code: ['', [Validators.required]],
      contactPerson: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      address: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      country: ['', Validators.required],
      postalCode: ['', Validators.required],
      paymentTerms: [''],
      creditLimit: [0]
    });
  }

  onShow(): void {
    if (this.supplier) {
      this.isEditing = true;
      this.form.patchValue(this.supplier);
    } else {
      this.isEditing = false;
      this.form.reset();
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const request = this.isEditing
      ? { id: this.supplier!.id, ...this.form.value }
      : this.form.value;

    // Call service to save
    // Emit submitted event
    this.submitted.emit(response);
    this.visibleChange.emit(false);
  }
}
```

---

## Supplier Form Dialog Template

```html
<p-dialog
  [(visible)]="visible"
  (visibleChange)="visibleChange.emit($event)"
  [header]="isEditing ? 'Edit Supplier' : 'New Supplier'"
  [modal]="true"
  [style]="{ width: '90vw', maxWidth: '600px' }"
  (onShow)="onShow()">

  <form [formGroup]="form" (ngSubmit)="onSubmit()">
    <!-- Basic Information -->
    <div class="form-group">
      <label for="name">Name <span class="required">*</span></label>
      <input
        pInputText
        id="name"
        type="text"
        formControlName="name"
        placeholder="Enter supplier name"
        class="w-full">
      <small class="text-danger" *ngIf="form.get('name')?.hasError('required')">
        Name is required
      </small>
    </div>

    <div class="form-group">
      <label for="code">Code <span class="required">*</span></label>
      <input
        pInputText
        id="code"
        type="text"
        formControlName="code"
        placeholder="Unique supplier code"
        class="w-full">
      <small class="text-danger" *ngIf="form.get('code')?.hasError('required')">
        Code is required
      </small>
    </div>

    <!-- Contact Information -->
    <div class="form-row">
      <div class="form-group col">
        <label for="contactPerson">Contact Person <span class="required">*</span></label>
        <input
          pInputText
          id="contactPerson"
          type="text"
          formControlName="contactPerson"
          placeholder="Contact person name"
          class="w-full">
      </div>

      <div class="form-group col">
        <label for="email">Email <span class="required">*</span></label>
        <input
          pInputText
          id="email"
          type="email"
          formControlName="email"
          placeholder="supplier@example.com"
          class="w-full">
      </div>
    </div>

    <div class="form-group">
      <label for="phone">Phone <span class="required">*</span></label>
      <input
        pInputText
        id="phone"
        type="tel"
        formControlName="phone"
        placeholder="+91 XXXXX XXXXX"
        class="w-full">
    </div>

    <!-- Address Information -->
    <div class="form-group">
      <label for="address">Address <span class="required">*</span></label>
      <textarea
        pInputTextarea
        id="address"
        formControlName="address"
        rows="3"
        placeholder="Full address"
        class="w-full">
      </textarea>
    </div>

    <div class="form-row">
      <div class="form-group col">
        <label for="city">City <span class="required">*</span></label>
        <input
          pInputText
          id="city"
          type="text"
          formControlName="city"
          placeholder="City"
          class="w-full">
      </div>

      <div class="form-group col">
        <label for="state">State <span class="required">*</span></label>
        <input
          pInputText
          id="state"
          type="text"
          formControlName="state"
          placeholder="State"
          class="w-full">
      </div>
    </div>

    <div class="form-row">
      <div class="form-group col">
        <label for="country">Country <span class="required">*</span></label>
        <input
          pInputText
          id="country"
          type="text"
          formControlName="country"
          placeholder="Country"
          class="w-full">
      </div>

      <div class="form-group col">
        <label for="postalCode">Postal Code <span class="required">*</span></label>
        <input
          pInputText
          id="postalCode"
          type="text"
          formControlName="postalCode"
          placeholder="Postal code"
          class="w-full">
      </div>
    </div>

    <!-- Payment Terms -->
    <div class="form-row">
      <div class="form-group col">
        <label for="paymentTerms">Payment Terms</label>
        <p-dropdown
          id="paymentTerms"
          formControlName="paymentTerms"
          [options]="paymentTermsOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Select payment terms"
          class="w-full">
        </p-dropdown>
      </div>

      <div class="form-group col">
        <label for="creditLimit">Credit Limit</label>
        <p-inputNumber
          id="creditLimit"
          formControlName="creditLimit"
          placeholder="0.00"
          [currency]="'INR'"
          [currencyDisplay]="'symbol'"
          class="w-full">
        </p-inputNumber>
      </div>
    </div>
  </form>

  <ng-template pTemplate="footer">
    <button
      pButton
      type="button"
      label="Cancel"
      icon="pi pi-times"
      class="p-button-text"
      (click)="visibleChange.emit(false)">
    </button>
    <button
      pButton
      type="button"
      [label]="isEditing ? 'Update' : 'Create'"
      icon="pi pi-check"
      class="p-button-success"
      (click)="onSubmit()"
      [disabled]="form.invalid">
    </button>
  </ng-template>
</p-dialog>
```

---

## Key Form Features to Implement

### 1. **Email Validation**
```typescript
Validators.email // Built-in Angular validator
```

### 2. **Phone Formatting**
```typescript
phone: new FormControl('', [
  Validators.required,
  Validators.pattern(/^[\d\s\-\+\(\)]{10,}$/)
])
```

### 3. **Credit Limit as Currency**
```html
<p-inputNumber
  formControlName="creditLimit"
  [currency]="'INR'"
  [currencyDisplay]="'symbol'">
</p-inputNumber>
```

### 4. **Dynamic Dropdown Options**
```typescript
paymentTermsOptions = [
  { label: 'Net 15', value: 'NET15' },
  { label: 'Net 30', value: 'NET30' },
  { label: 'Net 45', value: 'NET45' },
  { label: 'COD', value: 'COD' }
];
```

### 5. **Form Submission with API Call**
```typescript
onSubmit(): void {
  if (this.form.invalid) return;

  const request = this.isEditing
    ? { id: this.supplier!.id, ...this.form.value }
    : this.form.value;

  const observable = this.isEditing
    ? this.supplierService.updateSupplier(this.supplier!.id, request)
    : this.supplierService.createSupplier(request);

  observable.subscribe({
    next: (supplier) => {
      this.submitted.emit(supplier);
      this.visibleChange.emit(false);
    },
    error: (error) => {
      this.messageService.add({
        severity: 'error',
        detail: error?.error?.message || 'Operation failed'
      });
    }
  });
}
```

---

## Purchase Order Form - Special Considerations

### Line Items Management
```typescript
lineItems: FormArray = this.fb.array([]);

addLineItem(): void {
  const lineItem = this.fb.group({
    partId: ['', Validators.required],
    quantity: [0, [Validators.required, Validators.min(1)]],
    unitPrice: [0, [Validators.required, Validators.min(0)]]
  });
  this.lineItems.push(lineItem);
}

removeLineItem(index: number): void {
  this.lineItems.removeAt(index);
}

getLineTotal(index: number): number {
  const item = this.lineItems.at(index).value;
  return item.quantity * item.unitPrice;
}

getGrandTotal(): number {
  return this.lineItems.controls.reduce((sum, item) => {
    const line = item.value;
    return sum + (line.quantity * line.unitPrice);
  }, 0);
}
```

### Part Selection Dropdown with Search
```html
<p-dropdown
  [options]="parts"
  optionLabel="name"
  optionValue="id"
  [showClear]="true"
  [filter]="true"
  filterBy="name,sku"
  placeholder="Search and select part"
  [virtualScroll]="true"
  [rows]="10">
  <ng-template let-value pTemplate="selectedItem">
    <div *ngIf="value">
      {{ value.name }} ({{ value.sku }})
    </div>
  </ng-template>
  <ng-template let-part pTemplate="item">
    <div class="flex justify-between">
      <span>{{ part.name }}</span>
      <small class="text-gray-500">{{ part.sku }}</small>
    </div>
  </ng-template>
</p-dropdown>
```

---

## CSS Classes for Forms

```scss
.form-group {
  display: flex;
  flex-direction: column;
  margin-bottom: 1rem;
  gap: 0.5rem;

  label {
    font-weight: 600;
    color: #374151;
    font-size: 0.875rem;

    .required {
      color: #ef4444;
    }
  }
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;

  &.full {
    grid-template-columns: 1fr;
  }

  .col {
    // Inherits from form-group
  }
}

.w-full {
  width: 100%;
}

.text-danger {
  color: #ef4444;
  font-size: 0.75rem;
}

@media (max-width: 768px) {
  .form-row {
    grid-template-columns: 1fr;
  }
}
```

---

## Integration Checklist for Form Dialog

- [ ] Create component TypeScript file with form setup
- [ ] Create HTML template with form fields
- [ ] Create CSS file with form styling
- [ ] Add form validation
- [ ] Implement API integration
- [ ] Add error handling
- [ ] Add success/failure notifications
- [ ] Handle create vs edit modes
- [ ] Add to parent component imports
- [ ] Test form submission
- [ ] Test validation messages
- [ ] Test responsive design

---

## Common Import Statements

```typescript
import { FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { DropdownModule } from 'primeng/dropdown';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
```

---

## Next Steps

1. **Create Supplier Form Dialog** (High Priority)
   - Estimated time: 1-2 hours
   - Includes: Component, template, styling, validation, API integration

2. **Create Purchase Order Components** (Medium Priority)
   - Estimated time: 3-4 hours
   - More complex due to line items management

3. **Testing & Refinement** (Ongoing)
   - Unit tests
   - Integration tests
   - E2E tests

---

**Tips for Success**:
- Keep form fields aligned and consistent
- Always validate before submission
- Provide clear error messages
- Use loading states during API calls
- Show success/failure notifications
- Test responsiveness on mobile
- Keep reusable form groups for similar inputs
