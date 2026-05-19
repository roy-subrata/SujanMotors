import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PaymentProviderService, CreatePaymentProviderRequest } from '../services/payment-provider.service';
import { PROVIDER_TYPES, PaymentMethodOption } from '../../../shared/constants/payment-methods.constants';

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
    CardModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './payment-provider-form.component.html',
  styleUrls: ['./payment-provider-form.component.css']
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
  selectedProviderType: string = '';

  // Use shared provider types from centralized constants
  providerTypes: PaymentMethodOption[] = PROVIDER_TYPES;

  feeTypes = [
    { label: 'Fixed', value: 'FIXED' },
    { label: 'Percentage', value: 'PERCENTAGE' },
    { label: 'Tiered', value: 'TIERED' }
  ];

  filteredProviderTypes: any[] = [];
  filteredFeeTypes: any[] = [];

  // Field visibility based on provider type
  get showBankFields(): boolean {
    return this.selectedProviderType === 'BANK_TRANSFER';
  }

  get showMobileBankingFields(): boolean {
    return this.selectedProviderType === 'MOBILE_BANKING';
  }

  get showOnlineGatewayFields(): boolean {
    return this.selectedProviderType === 'ONLINE_GATEWAY' || this.selectedProviderType === 'CRYPTO';
  }

  get showFeeFields(): boolean {
    return !!this.selectedProviderType;
  }

  get showSettlementDays(): boolean {
    return ['BANK_TRANSFER', 'ONLINE_GATEWAY', 'CHECK', 'CRYPTO', 'MOBILE_BANKING'].includes(this.selectedProviderType);
  }

  get showCurrencies(): boolean {
    return ['ONLINE_GATEWAY', 'CRYPTO', 'BANK_TRANSFER'].includes(this.selectedProviderType);
  }

  constructor() {
    this.form = this.fb.group({
      providerName: ['', Validators.required],
      providerType: ['', Validators.required],
      // Bank Transfer fields
      bankName: [''],
      bankAccountNumber: [''],
      bankRoutingNumber: [''],
      beneficiaryName: [''],
      bankIBAN: [''],
      bankSWIFT: [''],
      // Online Gateway fields
      apiKey: [''],
      merchantId: [''],
      webhookUrl: [''],
      // Mobile Banking fields
      mobileNumber: [''],
      accountHolderName: [''],
      agentNumber: [''],
      // Fee Configuration
      transactionFeeType: ['FIXED'],
      transactionFeeAmount: [0],
      minimumAmount: [0],
      maximumAmount: [0],
      settlementDays: [1],
      supportedCurrencies: [''],
      notes: ['']
    });
  }

  ngOnInit(): void {
    // Subscribe to provider type changes
    this.form.get('providerType')?.valueChanges.subscribe(value => {
      this.selectedProviderType = value || '';
    });

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
        // Set provider type first for field visibility
        this.selectedProviderType = provider.providerType || '';

        this.form.patchValue({
          providerName: provider.providerName,
          providerType: provider.providerType,
          bankName: provider.bankName,
          bankAccountNumber: provider.bankAccountNumber,
          bankRoutingNumber: provider.bankRoutingNumber,
          beneficiaryName: provider.beneficiaryName,
          bankIBAN: provider.bankIBAN,
          bankSWIFT: provider.bankSWIFT,
          apiKey: provider.apiKey,
          merchantId: provider.merchantId,
          webhookUrl: provider.webhookUrl,
          mobileNumber: provider.mobileNumber,
          accountHolderName: provider.accountHolderName,
          agentNumber: provider.agentNumber,
          transactionFeeType: provider.transactionFeeType,
          transactionFeeAmount: provider.transactionFeeAmount,
          minimumAmount: provider.minimumAmount,
          maximumAmount: provider.maximumAmount,
          settlementDays: provider.settlementDays,
          supportedCurrencies: provider.supportedCurrencies,
          notes: provider.notes
        });
        this.loading = false;
      },
      error: (_error) => {
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
