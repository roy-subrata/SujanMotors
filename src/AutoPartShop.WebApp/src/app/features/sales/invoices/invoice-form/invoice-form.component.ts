import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { InvoiceService, InvoiceResponse, CreateInvoiceRequest, RecordPaymentRequest } from '../../services/invoice.service';
import { SalesOrderService, SalesOrderResponse } from '../../services/sales-order.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TextareaModule } from 'primeng/textarea';

@Component({
  selector: 'app-invoice-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    ToastModule,
    ConfirmDialogModule,
    DialogModule,
    TagModule,
    TableModule,
    InputTextModule,
    InputNumberModule,
    Select,
    DatePickerModule,
    TextareaModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './invoice-form.component.html',
  styleUrls: ['./invoice-form.component.css']
})
export class InvoiceFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly invoiceService = inject(InvoiceService);
  private readonly salesOrderService = inject(SalesOrderService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  invoiceForm!: FormGroup;
  paymentForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'view'>('create');
  invoiceId = signal<string | null>(null);
  invoice = signal<InvoiceResponse | null>(null);

  salesOrders = signal<SalesOrderResponse[]>([]);
  loadingSalesOrders = signal(false);
  showPaymentDialog = signal(false);

  // Computed property for p-select options
  salesOrderOptions = computed(() => {
    return this.salesOrders().map(order => ({
      label: `${order.soNumber} - ${order.customerName} - ${this.formatCurrency(order.grandTotal)}`,
      value: order.id
    }));
  });

  ngOnInit(): void {
    this.initializeForm();
    this.initializePaymentForm();
    this.loadSalesOrders();

    this.route.queryParams.subscribe(params => {
      const id = params['id'];
      const salesOrderId = params['salesOrderId'];

      if (id) {
        this.invoiceId.set(id);
        this.mode.set('view');
        this.loadInvoice(id);
      } else if (salesOrderId) {
        // Pre-select sales order if provided
        this.invoiceForm.patchValue({ salesOrderId });
        this.onSalesOrderChange(salesOrderId);
      }
    });

    if (this.mode() === 'view') {
      this.invoiceForm.disable();
    }
  }

  initializeForm(): void {
    const today = new Date();
    const twoWeeksLater = new Date();
    twoWeeksLater.setDate(twoWeeksLater.getDate() + 14);

    this.invoiceForm = this.fb.group({
      salesOrderId: ['', [Validators.required]],
      subTotal: [0, [Validators.required, Validators.min(0)]],
      taxAmount: [0, [Validators.min(0)]],
      dueDate: [twoWeeksLater, [Validators.required]],
      notes: ['']
    });
  }

  initializePaymentForm(): void {
    const today = new Date();

    this.paymentForm = this.fb.group({
      amount: [0, [Validators.required, Validators.min(0.01)]],
      paymentDate: [today, [Validators.required]]
    });
  }

  loadSalesOrders(): void {
    this.loadingSalesOrders.set(true);
    this.salesOrderService.getAllSalesOrders().subscribe({
      next: (orders) => {
        // Filter only confirmed orders that don't have invoices yet
        const availableOrders = orders.filter(o => o.status === 'CONFIRMED' || o.status === 'PENDING');
        this.salesOrders.set(availableOrders);
        this.loadingSalesOrders.set(false);
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load sales orders'
        });
        this.loadingSalesOrders.set(false);
      }
    });
  }

  onSalesOrderChange(salesOrderId: string): void {
    const order = this.salesOrders().find(o => o.id === salesOrderId);
    if (order) {
      this.invoiceForm.patchValue({
        subTotal: order.subTotal,
        taxAmount: order.taxAmount
      });
    }
  }

  loadInvoice(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.invoiceService.getInvoiceById(id).subscribe({
      next: (invoice) => {
        this.invoice.set(invoice);
        this.invoiceForm.patchValue({
          salesOrderId: invoice.salesOrderId,
          subTotal: invoice.subTotal,
          taxAmount: invoice.taxAmount,
          dueDate: new Date(invoice.dueDate),
          notes: invoice.notes
        });
        this.loading.set(false);
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load invoice'
        });
        this.loading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.invoiceForm.invalid) {
      Object.keys(this.invoiceForm.controls).forEach(key => {
        const control = this.invoiceForm.get(key);
        if (control?.invalid) {
          control.markAsTouched();
        }
      });
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const formValue = this.invoiceForm.value;
    const request: CreateInvoiceRequest = {
      salesOrderId: formValue.salesOrderId,
      subTotal: formValue.subTotal,
      taxAmount: formValue.taxAmount,
      dueDate: this.formatDateForApi(formValue.dueDate),
      notes: formValue.notes
    };

    this.invoiceService.createInvoice(request).subscribe({
      next: (invoice) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Invoice created successfully'
        });
        this.router.navigate(['/sales/invoices/view'], {
          queryParams: { id: invoice.id }
        });
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to create invoice'
        });
        this.saving.set(false);
      }
    });
  }

  issueInvoice(): void {
    if (!this.invoiceId()) return;

    this.confirmationService.confirm({
      message: 'Are you sure you want to issue this invoice? This action cannot be undone.',
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.invoiceService.issueInvoice(this.invoiceId()!).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Invoice issued successfully'
            });
            this.loadInvoice(this.invoiceId()!);
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to issue invoice'
            });
          }
        });
      }
    });
  }

  openPaymentDialog(): void {
    const invoice = this.invoice();
    if (invoice) {
      this.paymentForm.patchValue({
        amount: invoice.outstandingAmount,
        paymentDate: new Date()
      });
      this.showPaymentDialog.set(true);
    }
  }

  closePaymentDialog(): void {
    this.showPaymentDialog.set(false);
    this.paymentForm.reset();
    this.initializePaymentForm();
  }

  recordPayment(): void {
    if (this.paymentForm.invalid || !this.invoiceId()) {
      return;
    }

    const formValue = this.paymentForm.value;
    const request: RecordPaymentRequest = {
      amount: formValue.amount,
      paymentDate: this.formatDateForApi(formValue.paymentDate)
    };

    this.invoiceService.recordPayment(this.invoiceId()!, request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Payment Recorded',
          detail: `Payment of ${this.formatCurrency(formValue.amount)} recorded successfully`
        });
        this.closePaymentDialog();
        this.loadInvoice(this.invoiceId()!);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to record payment'
        });
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/sales/invoices']);
  }

  getStatusSeverity(status: string): string {
    const severityMap: Record<string, string> = {
      DRAFT: 'secondary',
      ISSUED: 'info',
      PARTIALLY_PAID: 'warning',
      PAID: 'success',
      OVERDUE: 'danger',
      CANCELLED: 'secondary'
    };
    return severityMap[status] || 'secondary';
  }

  formatCurrency(amount: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(amount, currency);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }

  private formatDateForApi(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
