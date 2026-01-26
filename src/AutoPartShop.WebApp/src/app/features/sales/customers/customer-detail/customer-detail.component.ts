import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { SkeletonModule } from 'primeng/skeleton';
import { AvatarModule } from 'primeng/avatar';
import { MessageService } from 'primeng/api';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { CustomerPaymentService, CustomerPaymentHistorySummary, PaymentHistoryItem, CustomerPaymentQuery } from '../../services/customer-payment.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';

@Component({
    selector: 'app-customer-detail',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FormsModule, PaginatorModule, ButtonModule, CardModule, TagModule, ToastModule, TabsModule, TableModule, SelectModule, DatePickerModule, TooltipModule, SkeletonModule, AvatarModule],
    providers: [MessageService],
    templateUrl: './customer-detail.component.html',
    styleUrls: ['./customer-detail.component.css']
})
export class CustomerDetailComponent implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly customerService = inject(CustomerService);
    private readonly paymentService = inject(CustomerPaymentService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly fb = inject(FormBuilder);

    customerId = signal<string>('');
    customer = signal<CustomerResponse | null>(null);
    paymentSummary = signal<CustomerPaymentHistorySummary | null>(null);
    loading = signal(true);
    activeTabIndex = signal(0);

    paymentHistory = signal<PaymentHistoryItem[]>([]);

    pageSize: number = 25;
    pageSizeOptions = [10, 25, 50, 100];
    totalCount: number = 0;
    first: number = 0;

    filterForm: FormGroup;
    constructor() {
        this.filterForm = this.fb.group({
            customerId: [''],
            search: [''],
            dates: [new Date(), new Date()],
            status: ['']
        });
    }

    get pageNumber(): number {
        return Math.floor(this.first / this.pageSize) + 1;
    }

    statusOptions = [
        { label: 'All Statuses', value: '' },
        { label: 'Completed', value: 'COMPLETED' },
        { label: 'Pending', value: 'PENDING' },
        { label: 'Failed', value: 'FAILED' },
        { label: 'Cancelled', value: 'CANCELLED' },
        { label: 'Refunded', value: 'REFUNDED' }
    ];

    ngOnInit(): void {
        this.route.queryParams.subscribe((params) => {
            const id = params['id'];
            if (id) {
                this.customerId.set(id);
                this.loadCustomerData();
                this.filterForm.patchValue({ customerId: id });
                this.loadPaymentHistory();
            } else {
                this.router.navigate(['/sales/customers']);
            }
        });

        this.filterForm.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => this.loadPaymentHistory());
    }

    loadPaymentHistory(): void {
        const form = this.filterForm.value;
        if (!form?.customerId) return;

        // Skip if only one date is selected (wait for both dates in range)
        const dates = form.dates;
        if (dates && dates[0] && !dates[1]) return;

        this.loading.set(true);
        const query: CustomerPaymentQuery = {
            customerId: form.customerId,
            status: form.status,
            search: form.search,
            toDate: dates?.[1],
            fromDate: dates?.[0],
            pageNumber: this.pageNumber,
            pageSize: this.pageSize
        };

        this.paymentService.getCustomerPayments(query).subscribe({
            next: (response) => {
                const values = response.data.map((x) => ({
                    id: x.id,
                    amount: x.amount,
                    paymentDate: x.paymentDate,
                    status: x.status,
                    paymentMethod: x.paymentMethod,
                    invoiceNumber: x.invoiceNumber ?? '',
                    paymentType: x.paymentType,
                    transactionNumber: x.transactionNumber,
                    providerName: x.providerName,
                    sourceAdvancePaymentId: x.sourceAdvancePaymentId,
                    sourceAdvanceTransactionNumber: x.transactionNumber,
                    isReconciled:x.isReconciled
                }));
                this.paymentHistory.set(values);
                this.totalCount = response.pagination.totalCount;
                this.loading.set(false);
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load payment history'
                });
                this.loading.set(false);
                console.error('Error loading payment history:', error);
            }
        });
    }

    clearSearch() {
        this.filterForm.reset({
            customerId: this.customerId(),
            search: '',
            dates: null,
            status: ''
        });
    }

    loadCustomerData(): void {
        this.loading.set(true);

        // Load customer details
        this.customerService.getCustomerById(this.customerId()).subscribe({
            next: (customer) => {
                this.customer.set(customer);
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load customer details'
                });
                console.error('Error loading customer:', error);
            }
        });

        // Load payment summary
        this.paymentService.getCustomerPaymentSummary(this.customerId()).subscribe({
            next: (summary) => {
                this.paymentSummary.set(summary);
                this.loading.set(false);
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load payment summary'
                });
                console.error('Error loading payment summary:', error);
                this.loading.set(false);
            }
        });
    }

    getInitials(customer: CustomerResponse): string {
        const firstName = customer.firstName || '';
        const lastName = customer.lastName || '';
        return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
    }

    getStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        switch (status?.toUpperCase()) {
            case 'ACTIVE':
                return 'success';
            case 'INACTIVE':
                return 'secondary';
            case 'SUSPENDED':
                return 'danger';
            default:
                return 'info';
        }
    }

    getPaymentStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        switch (status?.toUpperCase()) {
            case 'COMPLETED':
                return 'success';
            case 'PENDING':
                return 'warn';
            case 'FAILED':
            case 'CANCELLED':
                return 'danger';
            case 'REFUNDED':
                return 'secondary';
            default:
                return 'info';
        }
    }

    editCustomer(): void {
        this.router.navigate(['/sales/customers/edit'], {
            queryParams: { id: this.customerId() }
        });
    }

    recordPayment(): void {
        this.router.navigate(['/sales/customer-payments/new'], {
            queryParams: { customerId: this.customerId() }
        });
    }

    goBack(): void {
        this.router.navigate(['/sales/customers']);
    }

    formatCurrency(value: number): string {
        const currency = this.currencyService.selectedCurrency() || 'BDT';
        return this.currencyService.formatCurrency(value, currency);
    }

    onPageChange(event: PaginatorState): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? this.pageSize;
        this.filterForm.updateValueAndValidity();
    }
}
