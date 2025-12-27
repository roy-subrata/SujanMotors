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
import { SupplierPaymentService, CreateSupplierPaymentRequest } from '../services/supplier-payment.service';
import { SupplierService, SupplierResponse } from '../../inventory/services/supplier.service';
import { PaymentProviderService, PaymentProviderResponse } from '../services/payment-provider.service';

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
    CardModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './supplier-payment-form.component.html',
  styleUrls: ['./supplier-payment-form.component.css']
})
export class SupplierPaymentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly service = inject(SupplierPaymentService);
  private readonly supplierService = inject(SupplierService);
  private readonly paymentProviderService = inject(PaymentProviderService);

  form: FormGroup;
  loading = false;
  isEditing = false;
  paymentId: string | null = null;

  suppliers: SupplierResponse[] = [];
  filteredSuppliers: SupplierResponse[] = [];
  paymentProviders: PaymentProviderResponse[] = [];
  filteredPaymentProviders: PaymentProviderResponse[] = [];

  paymentMethods = [
    { label: 'Cash', value: 'Cash' },
    { label: 'Check', value: 'Check' },
    { label: 'Bank Transfer', value: 'BankTransfer' },
    { label: 'Credit Card', value: 'CreditCard' },
    { label: 'Debit Card', value: 'DebitCard' },
    { label: 'Online Payment', value: 'OnlinePayment' },
    { label: 'UPI', value: 'UPI' },
    { label: 'NEFT/RTGS', value: 'NEFT_RTGS' },
    { label: 'IMPS', value: 'IMPS' }
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
    // Load suppliers and payment providers
    this.loadSuppliers();
    this.loadPaymentProviders();
    this.filteredPaymentMethods = this.paymentMethods;

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

  /**
   * Load suppliers for autocomplete
   */
  private loadSuppliers(): void {
    this.supplierService.getAllSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = Array.isArray(suppliers) ? suppliers : [];
        this.filteredSuppliers = this.suppliers;
      },
      error: (error) => {
        console.error('Error loading suppliers:', error);
      }
    });
  }

  /**
   * Load payment providers for autocomplete
   */
  private loadPaymentProviders(): void {
    this.paymentProviderService.getAllPaymentProviders().subscribe({
      next: (providers) => {
        this.paymentProviders = Array.isArray(providers) ? providers : [];
        this.filteredPaymentProviders = this.paymentProviders;
      },
      error: (error) => {
        console.error('Error loading payment providers:', error);
      }
    });
  }

  /**
   * Filter suppliers
   */
  filterSuppliers(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredSuppliers = this.suppliers.filter(supplier =>
      supplier.name.toLowerCase().includes(query) ||
      supplier.code.toLowerCase().includes(query)
    );
  }

  /**
   * Filter payment providers
   */
  filterPaymentProviders(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredPaymentProviders = this.paymentProviders.filter(provider =>
      provider.providerName.toLowerCase().includes(query)
    );
  }

  /**
   * Filter payment methods
   */
  filterPaymentMethods(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredPaymentMethods = this.paymentMethods.filter(method =>
      method.label.toLowerCase().includes(query)
    );
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
      const supplierId = this.form.get('supplierId')?.value;
      const paymentProviderId = this.form.get('paymentProviderId')?.value;
      const paymentMethod = this.form.get('paymentMethod')?.value;

      const createRequest: CreateSupplierPaymentRequest = {
        supplierId: typeof supplierId === 'string' ? supplierId : supplierId?.id || '',
        paymentProviderId: typeof paymentProviderId === 'string' ? paymentProviderId : paymentProviderId?.id || '',
        amount: this.form.get('amount')?.value || 0,
        paymentMethod: paymentMethod || '',
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

}
