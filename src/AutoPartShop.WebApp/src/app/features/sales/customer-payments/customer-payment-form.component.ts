import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { CustomerPaymentService, CreateCustomerPaymentRequest, CustomerPaymentResponse } from '../services/customer-payment.service';
import { CustomerService, CustomerResponse } from '../services/customer.service';
import { InvoiceService, InvoiceResponse } from '../services/invoice.service';
import { PaymentProviderService, PaymentProviderResponse } from '../../procurement/services/payment-provider.service';
import { CUSTOMER_PAYMENT_METHODS, PaymentMethodOption, getPaymentMethodIcon as getMethodIcon } from '../../../shared/constants/payment-methods.constants';
import { CurrencyService } from '../../../shared/services/currency.service';
import { AppCurrencyPipe } from '../../../shared/pipes/app-currency.pipe';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';
import { map } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'app-customer-payment-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    CardModule,
    ToastModule,
    DatePickerModule,
    SelectModule,
    TagModule,
    AppCurrencyPipe,
    LazyAutocompleteComponent
  ],
  providers: [MessageService],
  templateUrl: './customer-payment-form.component.html',
  styleUrls: ['./customer-payment-form.component.css']
})
export class CustomerPaymentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly service = inject(CustomerPaymentService);
  private readonly customerService = inject(CustomerService);
  private readonly invoiceService = inject(InvoiceService);
  private readonly paymentProviderService = inject(PaymentProviderService);
  private readonly currencyService = inject(CurrencyService);

  form: FormGroup;
  loading = false;
  isEditing = false;
  paymentId: string | null = null;

  // Set after successful creation — shows the success/receipt panel
  createdPayment = signal<CustomerPaymentResponse | null>(null);
  receiptLoading = signal(false);

  invoices: InvoiceResponse[] = [];
  paymentProviders: PaymentProviderResponse[] = [];

  // Use shared payment methods from centralized constants
  paymentMethods: PaymentMethodOption[] = CUSTOMER_PAYMENT_METHODS;

  // Lazy fetch function
  fetchCustomersLazy = (req: LazyRequest) =>
    this.customerService.getCustomers({
      search: req.search,
      pageNumber: req.pageNumber,
      pageSize: req.pageSize
    }).pipe(
      map(res => ({
        items: res.data,
        totalCount: res.pagination.totalCount
      } as LazyResponse<CustomerResponse>))
    );

  fetchPaymentProvidersLazy = (req: LazyRequest) => of({
    items: this.paymentProviders.filter(p => 
      !req.search || p.providerName.toLowerCase().includes(req.search.toLowerCase())
    ),
    totalCount: this.paymentProviders.length
  } as LazyResponse<PaymentProviderResponse>);

  get currencyCode(): string {
    return this.currencyService.selectedCurrency();
  }

  get currencyLocale(): string {
    return this.currencyService.getSelectedCurrencyLocale();
  }


  constructor() {
    this.form = this.fb.group({
      customerId: ['', Validators.required],
      invoiceId: [''],
      paymentProviderId: ['', Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      paymentFee: [0],
      paymentMethod: ['', Validators.required],
      transactionNumber: [''],
      referenceNumber: [''],
      authorizationCode: [''],
      paymentDate: [new Date()],
      notes: ['']
    });
  }

  ngOnInit(): void {
    // Load payment providers
    this.loadPaymentProviders();

    this.route.queryParams.subscribe(params => {
      if (params['id']) {
        this.paymentId = params['id'];
        this.isEditing = true;
        this.loadPayment();
        // Disable fields that shouldn't be edited after creation
        this.form.get('customerId')?.disable();
        this.form.get('paymentProviderId')?.disable();
        this.form.get('amount')?.disable();
        this.form.get('paymentMethod')?.disable();
        this.form.get('transactionNumber')?.disable();
        this.form.get('referenceNumber')?.disable();
        this.form.get('invoiceId')?.disable();
        this.form.get('paymentDate')?.disable();
      } else if (params['customerId']) {
        // Pre-select customer when coming from customer list "Record Payment" action
        this.preSelectCustomer(params['customerId']);
      }
    });

    // Watch for customer changes to load their invoices
    this.form.get('customerId')?.valueChanges.subscribe(customerId => {
      if (customerId && typeof customerId === 'string') {
        this.loadCustomerInvoices(customerId);
      } else if (customerId && typeof customerId === 'object' && customerId.id) {
        this.loadCustomerInvoices(customerId.id);
      }
    });
  }

  /**
   * Pre-select a customer by ID (used when navigating from customer list)
   */
  private preSelectCustomer(customerId: string): void {
    this.customerService.getCustomerById(customerId).subscribe({
      next: (customer) => {
        this.form.patchValue({ customerId: customer });
        this.loadCustomerInvoices(customerId);
      },
      error: (error) => {
        console.error('Error loading customer:', error);
        this.messageService.add({ 
          severity: 'error', 
          summary: 'Error', 
          detail: 'Failed to load customer details' 
        });
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
      },
      error: (error) => {
        console.error('Error loading payment providers:', error);
      }
    });
  }

  /**
   * Load invoices for selected customer
   */
  private loadCustomerInvoices(customerId: string): void {
    this.invoiceService.getInvoicesByCustomer(customerId).subscribe({
      next: (invoices) => {
        this.invoices = invoices || [];
      },
      error: (error) => {
        console.error('Error loading invoices:', error);
        this.invoices = [];
      }
    });
  }

  /**
   * Get customer display name
   */
  getCustomerDisplay(customer: CustomerResponse): string {
    return customer.firstName + ' ' + customer.lastName + ' (' + customer.customerCode + ')';
  }

  /**
   * Get invoice display
   */
  getInvoiceDisplay(invoice: InvoiceResponse): string {
    return invoice.invoiceNumber + ' - ' + invoice.grandTotal.toFixed(2);
  }

  loadPayment(): void {
    if (!this.paymentId) return;
    this.loading = true;
    this.service.getCustomerPaymentById(this.paymentId).subscribe({
      next: (payment) => {
        this.form.patchValue({
          customerId: payment.customerId,
          invoiceId: payment.invoiceId,
          paymentProviderId: payment.paymentProviderId,
          amount: payment.amount,
          paymentFee: payment.paymentFee,
          paymentMethod: payment.paymentMethod,
          transactionNumber: payment.transactionNumber,
          referenceNumber: payment.referenceNumber,
          authorizationCode: payment.authorizationCode,
          notes: payment.notes,
          paymentDate: new Date(payment.paymentDate)
        });
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load customer payment'
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

    if (this.loading) return;

    this.loading = true;

    if (this.isEditing && this.paymentId) {
      // For update, only send mutable fields
      const updateRequest = {
        referenceNumber: this.form.get('referenceNumber')?.value || '',
        authorizationCode: this.form.get('authorizationCode')?.value || '',
        notes: this.form.get('notes')?.value || ''
      };
      this.service.updateCustomerPayment(this.paymentId, updateRequest).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Customer payment updated successfully'
          });
          this.loading = false;
          this.router.navigate(['/sales/customer-payments']);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to update customer payment')
          });
          this.loading = false;
        }
      });
    } else {
      // For create, send all required fields
      const customerId = this.form.get('customerId')?.value;
      const invoiceId = this.form.get('invoiceId')?.value;
      const paymentProviderId = this.form.get('paymentProviderId')?.value;
      const paymentDate = this.form.get('paymentDate')?.value;

      const createRequest: CreateCustomerPaymentRequest = {
        customerId: typeof customerId === 'string' ? customerId : customerId?.id || '',
        invoiceId: invoiceId ? (typeof invoiceId === 'string' ? invoiceId : invoiceId?.id) : undefined,
        paymentProviderId: typeof paymentProviderId === 'string' ? paymentProviderId : paymentProviderId?.id || '',
        amount: this.form.get('amount')?.value || 0,
        paymentMethod: this.form.get('paymentMethod')?.value || '',
        transactionNumber: this.form.get('transactionNumber')?.value || '',
        referenceNumber: this.form.get('referenceNumber')?.value || '',
        paymentDate: paymentDate ? (paymentDate instanceof Date ? this.toLocalDateString(paymentDate) : paymentDate) : undefined,
        notes: this.form.get('notes')?.value || ''
      };

      this.service.createCustomerPayment(createRequest).subscribe({
        next: (payment) => {
          this.loading = false;
          this.createdPayment.set(payment);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to create customer payment')
          });
          this.loading = false;
        }
      });
    }
  }

  printReceipt(): void {
    const payment = this.createdPayment();
    if (!payment) return;

    this.receiptLoading.set(true);
    this.service.downloadReceipt(payment.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => window.URL.revokeObjectURL(url), 60000);
        this.receiptLoading.set(false);
      },
      error: () => {
        this.receiptLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to generate receipt. Please try again.',
          life: 5000
        });
      }
    });
  }

  recordAnother(): void {
    this.createdPayment.set(null);
    this.form.reset({
      paymentDate: new Date(),
      amount: 0,
      paymentFee: 0
    });
  }

  onCancel(): void {
    this.router.navigate(['/sales/customer-payments']);
  }

  /**
   * Get icon for payment method (using shared helper)
   */
  getPaymentMethodIcon(method: string): string {
    return getMethodIcon(method);
  }

  private toLocalDateString(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
