import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { SupplierPaymentService, CreateSupplierPaymentRequest } from '../services/supplier-payment.service';

@Component({
  selector: 'app-supplier-payment-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    AutoCompleteModule,
    ToastModule
  ],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>
    <div class="container">
      <div class="header">
        <h2>{{ isEditing ? 'Edit Supplier Payment' : 'Create Supplier Payment' }}</h2>
      </div>

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="form-container">
        <div class="form-row">
          <div class="form-group">
            <label>Supplier ID *</label>
            <input pInputText formControlName="supplierId" placeholder="Supplier ID (e.g., UUID)" [readonly]="isEditing" />
          </div>
          <div class="form-group">
            <label>Payment Provider ID *</label>
            <input pInputText formControlName="paymentProviderId" placeholder="Payment Provider ID (e.g., UUID)" [readonly]="isEditing" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Amount *</label>
            <p-inputNumber formControlName="amount" placeholder="0.00" [readonly]="isEditing"></p-inputNumber>
          </div>
          <div class="form-group">
            <label>Payment Method *</label>
            <p-autoComplete [suggestions]="filteredPaymentMethods" formControlName="paymentMethod"
              [forceSelection]="true" field="label" placeholder="Select Method"
              (completeMethod)="filterPaymentMethods($event)" [disabled]="isEditing"></p-autoComplete>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Transaction Number</label>
            <input pInputText formControlName="transactionNumber" placeholder="Transaction number" [readonly]="isEditing" />
          </div>
          <div class="form-group">
            <label>Reference Number</label>
            <input pInputText formControlName="referenceNumber" placeholder="Reference number" [readonly]="isEditing" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group" *ngIf="isEditing">
            <label>Authorization Code</label>
            <input pInputText formControlName="authorizationCode" placeholder="Authorization code" />
          </div>
          <div class="form-group" [class.full]="!isEditing">
            <label>Invoice Number</label>
            <input pInputText formControlName="invoiceNumber" placeholder="Invoice number" [readonly]="isEditing" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Payment Date</label>
            <input pInputText type="date" formControlName="paymentDate" [readonly]="isEditing" />
          </div>
          <div class="form-group" *ngIf="!isEditing">
            <label>Purchase Order ID</label>
            <input pInputText formControlName="purchaseOrderId" placeholder="Purchase Order ID (optional)" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group full">
            <label>Notes</label>
            <textarea pInputText formControlName="notes" placeholder="Additional notes" rows="3"></textarea>
          </div>
        </div>

        <div class="button-group">
          <button pButton type="submit" label="Save" icon="pi pi-check" [loading]="loading"></button>
          <button pButton type="button" label="Cancel" icon="pi pi-times" class="p-button-secondary" (click)="onCancel()"></button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .container {
      padding: 2rem;
    }
    .header {
      margin-bottom: 2rem;
    }
    .form-container {
      max-width: 1000px;
    }
    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      margin-bottom: 1rem;
    }
    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    .form-group.full {
      grid-column: 1 / -1;
    }
    .form-group label {
      font-weight: 500;
      font-size: 0.875rem;
    }
    .button-group {
      display: flex;
      gap: 1rem;
      margin-top: 2rem;
    }
    textarea {
      padding: 0.5rem;
      border: 1px solid #d0d0d0;
      border-radius: 4px;
      font-family: inherit;
    }
  `]
})
export class SupplierPaymentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly service = inject(SupplierPaymentService);

  form: FormGroup;
  loading = false;
  isEditing = false;
  paymentId: string | null = null;

  paymentMethods = [
    { label: 'Bank Transfer', value: 'BANK_TRANSFER' },
    { label: 'Check', value: 'CHECK' },
    { label: 'Cash', value: 'CASH' },
    { label: 'Crypto', value: 'CRYPTO' },
    { label: 'Other', value: 'OTHER' }
  ];

  filteredPaymentMethods: any[] = [];

  constructor() {
    this.form = this.fb.group({
      supplierId: ['', Validators.required],
      paymentProviderId: ['', Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      paymentMethod: ['', Validators.required],
      transactionNumber: [''],
      referenceNumber: [''],
      authorizationCode: [''],
      invoiceNumber: [''],
      purchaseOrderId: [''],
      paymentDate: [new Date().toISOString().split('T')[0]],
      notes: ['']
    });
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['id']) {
        this.paymentId = params['id'];
        this.isEditing = true;
        this.loadPayment();
        // Disable fields that shouldn't be edited after creation
        this.form.get('supplierId')?.disable();
        this.form.get('paymentProviderId')?.disable();
        this.form.get('amount')?.disable();
        this.form.get('paymentMethod')?.disable();
        this.form.get('transactionNumber')?.disable();
        this.form.get('referenceNumber')?.disable();
        this.form.get('invoiceNumber')?.disable();
        this.form.get('paymentDate')?.disable();
        this.form.get('purchaseOrderId')?.disable();
      }
    });
  }

  loadPayment(): void {
    if (!this.paymentId) return;
    this.loading = true;
    this.service.getSupplierPaymentById(this.paymentId).subscribe({
      next: (payment) => {
        this.form.patchValue({
          supplierId: payment.supplierId,
          paymentProviderId: payment.paymentProviderId,
          amount: payment.amount,
          paymentMethod: payment.paymentMethod,
          transactionNumber: payment.transactionNumber,
          referenceNumber: payment.referenceNumber,
          authorizationCode: payment.authorizationCode,
          invoiceNumber: payment.invoiceNumber,
          notes: payment.notes,
          paymentDate: new Date(payment.paymentDate).toISOString().split('T')[0]
        });
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load supplier payment'
        });
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.messageService.add({
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields'
      });
      return;
    }

    this.loading = true;

    if (this.isEditing && this.paymentId) {
      // For update, only send mutable fields
      const updateRequest = {
        status: '',
        referenceNumber: this.form.get('referenceNumber')?.value || '',
        authorizationCode: this.form.get('authorizationCode')?.value || '',
        notes: this.form.get('notes')?.value || ''
      };
      this.service.updateSupplierPayment(this.paymentId, updateRequest).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Supplier payment updated successfully'
          });
          this.router.navigate(['/procurement/supplier-payments']);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update supplier payment'
          });
          this.loading = false;
        }
      });
    } else {
      // For create, send all required fields
      const createRequest: CreateSupplierPaymentRequest = {
        supplierId: this.form.get('supplierId')?.value || '',
        paymentProviderId: this.form.get('paymentProviderId')?.value || '',
        amount: this.form.get('amount')?.value || 0,
        paymentMethod: this.form.get('paymentMethod')?.value || '',
        transactionNumber: this.form.get('transactionNumber')?.value || '',
        referenceNumber: this.form.get('referenceNumber')?.value || '',
        invoiceNumber: this.form.get('invoiceNumber')?.value || '',
        paymentDate: this.form.get('paymentDate')?.value,
        notes: this.form.get('notes')?.value || ''
      };
      this.service.createSupplierPayment(createRequest).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Supplier payment created successfully'
          });
          this.router.navigate(['/procurement/supplier-payments']);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to create supplier payment'
          });
          this.loading = false;
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/procurement/supplier-payments']);
  }

  /**
   * Filter payment methods based on user input
   */
  filterPaymentMethods(event: any): void {
    const filtered: any[] = [];
    const query = event.query.toLowerCase();

    this.paymentMethods.forEach(method => {
      if (method.label.toLowerCase().includes(query)) {
        filtered.push(method);
      }
    });

    this.filteredPaymentMethods = filtered;
  }
}
