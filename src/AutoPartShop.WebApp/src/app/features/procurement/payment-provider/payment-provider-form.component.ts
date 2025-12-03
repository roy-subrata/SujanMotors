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
import { PaymentProviderService, CreatePaymentProviderRequest } from '../services/payment-provider.service';

@Component({
  selector: 'app-payment-provider-form',
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
        <h2>{{ isEditing ? 'Edit Payment Provider' : 'Create Payment Provider' }}</h2>
      </div>

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="form-container">
        <div class="form-row">
          <div class="form-group">
            <label>Provider Name *</label>
            <input pInputText formControlName="providerName" placeholder="e.g., Stripe, PayPal" />
          </div>
          <div class="form-group">
            <label>Provider Type *</label>
            <p-autoComplete [suggestions]="filteredProviderTypes" formControlName="providerType"
              [forceSelection]="true" field="label" placeholder="Select Type"
              (completeMethod)="filterProviderTypes($event)"></p-autoComplete>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Bank Name</label>
            <input pInputText formControlName="bankName" placeholder="Bank name" />
          </div>
          <div class="form-group">
            <label>Account Number</label>
            <input pInputText formControlName="bankAccountNumber" placeholder="Account number" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Routing Number</label>
            <input pInputText formControlName="bankRoutingNumber" placeholder="Routing number" />
          </div>
          <div class="form-group">
            <label>Beneficiary Name</label>
            <input pInputText formControlName="beneficiaryName" placeholder="Beneficiary name" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>IBAN</label>
            <input pInputText formControlName="bankIBAN" placeholder="IBAN" />
          </div>
          <div class="form-group">
            <label>SWIFT Code</label>
            <input pInputText formControlName="bankSWIFT" placeholder="SWIFT code" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>API Key</label>
            <input pInputText formControlName="apiKey" placeholder="API key" type="password" />
          </div>
          <div class="form-group">
            <label>Merchant ID</label>
            <input pInputText formControlName="merchantId" placeholder="Merchant ID" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Fee Type</label>
            <p-autoComplete [suggestions]="filteredFeeTypes" formControlName="transactionFeeType"
              [forceSelection]="true" field="label" placeholder="Select Fee Type"
              (completeMethod)="filterFeeTypes($event)"></p-autoComplete>
          </div>
          <div class="form-group">
            <label>Fee Amount</label>
            <p-inputNumber formControlName="transactionFeeAmount" placeholder="0.00"></p-inputNumber>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Minimum Amount</label>
            <p-inputNumber formControlName="minimumAmount" placeholder="0.00"></p-inputNumber>
          </div>
          <div class="form-group">
            <label>Maximum Amount</label>
            <p-inputNumber formControlName="maximumAmount" placeholder="0.00"></p-inputNumber>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Settlement Days</label>
            <p-inputNumber formControlName="settlementDays" placeholder="1"></p-inputNumber>
          </div>
          <div class="form-group">
            <label>Supported Currencies</label>
            <input pInputText formControlName="supportedCurrencies" placeholder="USD,EUR,GBP" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group full">
            <label>Webhook URL</label>
            <input pInputText formControlName="webhookUrl" placeholder="https://example.com/webhook" />
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
export class PaymentProviderFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly service = inject(PaymentProviderService);

  form: FormGroup;
  loading = false;
  isEditing = false;
  providerId: string | null = null;

  providerTypes = [
    { label: 'Online Gateway', value: 'ONLINE_GATEWAY' },
    { label: 'Bank Transfer', value: 'BANK_TRANSFER' },
    { label: 'Cash', value: 'CASH' },
    { label: 'Check', value: 'CHECK' },
    { label: 'Crypto', value: 'CRYPTO' },
    { label: 'Other', value: 'OTHER' }
  ];

  feeTypes = [
    { label: 'Fixed', value: 'FIXED' },
    { label: 'Percentage', value: 'PERCENTAGE' },
    { label: 'Tiered', value: 'TIERED' }
  ];

  filteredProviderTypes: any[] = [];
  filteredFeeTypes: any[] = [];

  constructor() {
    this.form = this.fb.group({
      providerName: ['', Validators.required],
      providerType: ['', Validators.required],
      bankName: [''],
      bankAccountNumber: [''],
      bankRoutingNumber: [''],
      beneficiaryName: [''],
      bankIBAN: [''],
      bankSWIFT: [''],
      apiKey: [''],
      merchantId: [''],
      transactionFeeType: ['FIXED'],
      transactionFeeAmount: [0],
      minimumAmount: [0],
      maximumAmount: [0],
      settlementDays: [1],
      supportedCurrencies: [''],
      webhookUrl: [''],
      notes: ['']
    });
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['id']) {
        this.providerId = params['id'];
        this.isEditing = true;
        this.loadProvider();
      }
    });
  }

  loadProvider(): void {
    if (!this.providerId) return;
    this.loading = true;
    this.service.getPaymentProviderById(this.providerId).subscribe({
      next: (provider) => {
        this.form.patchValue({
          providerName: provider.providerName,
          providerType: provider.providerType,
          bankName: provider.bankName,
          bankAccountNumber: provider.bankAccountNumber,
          bankRoutingNumber: provider.bankRoutingNumber,
          beneficiaryName: provider.beneficiaryName,
          bankIBAN: provider.bankIBAN,
          bankSWIFT: provider.bankSWIFT,
          transactionFeeType: provider.transactionFeeType,
          transactionFeeAmount: provider.transactionFeeAmount,
          minimumAmount: provider.minimumAmount,
          maximumAmount: provider.maximumAmount,
          settlementDays: provider.settlementDays,
          supportedCurrencies: provider.supportedCurrencies,
          webhookUrl: provider.webhookUrl,
          notes: provider.notes
        });
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load payment provider'
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
    const request = this.form.value as CreatePaymentProviderRequest;

    if (this.isEditing && this.providerId) {
      this.service.updatePaymentProvider(this.providerId, request).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Payment provider updated successfully'
          });
          this.router.navigate(['/procurement/payment-providers']);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update payment provider'
          });
          this.loading = false;
        }
      });
    } else {
      this.service.createPaymentProvider(request).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Payment provider created successfully'
          });
          this.router.navigate(['/procurement/payment-providers']);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to create payment provider'
          });
          this.loading = false;
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/procurement/payment-providers']);
  }

  /**
   * Filter provider types based on user input
   */
  filterProviderTypes(event: any): void {
    const filtered: any[] = [];
    const query = event.query.toLowerCase();

    this.providerTypes.forEach(type => {
      if (type.label.toLowerCase().includes(query)) {
        filtered.push(type);
      }
    });

    this.filteredProviderTypes = filtered;
  }

  /**
   * Filter fee types based on user input
   */
  filterFeeTypes(event: any): void {
    const filtered: any[] = [];
    const query = event.query.toLowerCase();

    this.feeTypes.forEach(type => {
      if (type.label.toLowerCase().includes(query)) {
        filtered.push(type);
      }
    });

    this.filteredFeeTypes = filtered;
  }
}
