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
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { CustomerPaymentService, CreateCustomerPaymentRequest } from '../services/customer-payment.service';
import { CustomerService, CustomerResponse } from '../services/customer.service';
import { InvoiceService, InvoiceResponse } from '../services/invoice.service';
import { PaymentProviderService, PaymentProviderResponse } from '../../procurement/services/payment-provider.service';

@Component({
  selector: 'app-customer-payment-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    AutoCompleteModule,
    CardModule,
    ToastModule,
    DatePickerModule,
    SelectModule,
    TagModule
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

  form: FormGroup;
  loading = false;
  isEditing = false;
  paymentId: string | null = null;

  customers: CustomerResponse[] = [];
  filteredCustomers: CustomerResponse[] = [];
  invoices: InvoiceResponse[] = [];
  filteredInvoices: InvoiceResponse[] = [];
  paymentProviders: PaymentProviderResponse[] = [];
  filteredPaymentProviders: PaymentProviderResponse[] = [];

  paymentMethods = [
    { label: 'Cash', value: 'CASH', icon: 'pi-money-bill' },
    { label: 'UPI', value: 'UPI', icon: 'pi-mobile' },
    { label: 'Credit Card', value: 'CREDIT_CARD', icon: 'pi-credit-card' },
    { label: 'Debit Card', value: 'DEBIT_CARD', icon: 'pi-credit-card' },
    { label: 'Card', value: 'CARD', icon: 'pi-credit-card' },
    { label: 'Cheque', value: 'CHEQUE', icon: 'pi-file-edit' },
    { label: 'Bank Transfer', value: 'BANK_TRANSFER', icon: 'pi-building' },
    { label: 'NEFT', value: 'NEFT', icon: 'pi-building' },
    { label: 'RTGS', value: 'RTGS', icon: 'pi-building' },
    { label: 'Demand Draft', value: 'DEMAND_DRAFT', icon: 'pi-file' }
  ];


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
    // Load customers and payment providers
    this.loadCustomers();
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
   * Load customers for autocomplete
   */
  private loadCustomers(): void {
    this.customerService.getCustomers(1, 1000).subscribe({
      next: (response) => {
        this.customers = response.data || [];
        this.filteredCustomers = this.customers;
      },
      error: (error) => {
        console.error('Error loading customers:', error);
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
   * Load invoices for selected customer
   */
  private loadCustomerInvoices(customerId: string): void {
    this.invoiceService.getInvoicesByCustomer(customerId).subscribe({
      next: (invoices) => {
        this.invoices = invoices || [];
        this.filteredInvoices = this.invoices;
      },
      error: (error) => {
        console.error('Error loading invoices:', error);
        this.invoices = [];
        this.filteredInvoices = [];
      }
    });
  }

  /**
   * Filter customers
   */
  filterCustomers(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredCustomers = this.customers.filter(customer => {
      const fullName = customer.firstName + ' ' + customer.lastName;
      return fullName.toLowerCase().includes(query) ||
             customer.customerCode.toLowerCase().includes(query) ||
             (customer.email && customer.email.toLowerCase().includes(query));
    });
  }

  /**
   * Filter invoices
   */
  filterInvoices(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredInvoices = this.invoices.filter(invoice =>
      invoice.invoiceNumber.toLowerCase().includes(query)
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

    this.loading = true;

    if (this.isEditing && this.paymentId) {
      // For update, only send mutable fields
      const updateRequest = {
        status: '',
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
          this.router.navigate(['/sales/customer-payments']);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update customer payment'
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
        paymentDate: paymentDate ? (paymentDate instanceof Date ? paymentDate.toISOString() : paymentDate) : undefined,
        notes: this.form.get('notes')?.value || ''
      };

      this.service.createCustomerPayment(createRequest).subscribe({
        next: (createdPayment) => {
          const paymentMethod = this.form.get('paymentMethod')?.value?.toUpperCase() || '';

          // Auto-confirm instant payment methods (CASH, UPI, CARD)
          // Keep CHEQUE and BANK_TRANSFER as PENDING for manual verification
          const instantPaymentMethods = ['CASH', 'UPI', 'CARD', 'CREDIT_CARD', 'DEBIT_CARD'];

          if (instantPaymentMethods.includes(paymentMethod)) {
            // Automatically confirm instant payments
            this.service.confirmPayment(createdPayment.id).subscribe({
              next: () => {
                this.messageService.add({
                  severity: 'success',
                  summary: 'Success',
                  detail: 'Payment created and confirmed successfully'
                });
                this.router.navigate(['/sales/customer-payments']);
              },
              error: (confirmError) => {
                this.messageService.add({
                  severity: 'warn',
                  summary: 'Warning',
                  detail: 'Payment created but auto-confirmation failed. Please confirm manually.'
                });
                this.router.navigate(['/sales/customer-payments']);
              }
            });
          } else {
            // For CHEQUE, BANK_TRANSFER, etc., keep as PENDING for manual verification
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Payment created successfully. ${paymentMethod} payments require manual confirmation.`,
              life: 5000
            });
            this.router.navigate(['/sales/customer-payments']);
          }
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to create customer payment'
          });
          this.loading = false;
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/sales/customer-payments']);
  }

  /**
   * Check if payment method is instant (auto-confirm)
   */
  isInstantPaymentMethod(method: string): boolean {
    const instantMethods = ['CASH', 'UPI', 'CARD', 'CREDIT_CARD', 'DEBIT_CARD'];
    return instantMethods.includes(method);
  }

  /**
   * Get icon for payment method
   */
  getPaymentMethodIcon(method: string): string {
    const methodObj = this.paymentMethods.find(m => m.value === method);
    return methodObj ? `pi ${methodObj.icon}` : 'pi pi-money-bill';
  }
}
