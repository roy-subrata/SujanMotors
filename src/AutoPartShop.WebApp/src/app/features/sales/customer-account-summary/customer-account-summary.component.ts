import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { Observable, Subject, takeUntil } from 'rxjs';
import { map } from 'rxjs/operators';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete/lazy-autocomplete.component';
import { CustomerService, CustomerResponse } from '../services/customer.service';
import { CustomerVehicleService, CustomerVehicleResponse } from '../services/customer-vehicle.service';
import {
    CustomerAccountSummaryService,
    CustomerAccountSummary,
    CustomerAccountSummaryQuery
} from '../services/customer-account-summary.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';

@Component({
    selector: 'app-customer-account-summary',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        ToastModule,
        DatePickerModule,
        SelectModule,
        TooltipModule,
        PaginatorModule,
        SkeletonModule,
        LazyAutocompleteComponent,
        PageHeaderComponent,
        PageContainerComponent
    ],
    providers: [MessageService],
    templateUrl: './customer-account-summary.component.html',
    styleUrls: ['./customer-account-summary.component.css']
})
export class CustomerAccountSummaryComponent implements OnInit, OnDestroy {
    private readonly customerService = inject(CustomerService);
    private readonly vehicleService = inject(CustomerVehicleService);
    private readonly summaryService = inject(CustomerAccountSummaryService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly currencyService = inject(CurrencyService);
    private readonly destroy$ = new Subject<void>();

    // Filter state
    selectedCustomer: CustomerResponse | null = null;
    fromDate: Date | null = null;
    toDate: Date | null = null;

    // Vehicle filter — the customer's vehicles, and the one this statement is scoped to (optional)
    vehicles = signal<CustomerVehicleResponse[]>([]);
    selectedVehicleId: string | null = null;

    // Report state
    summary = signal<CustomerAccountSummary | null>(null);
    loading = signal(false);
    error = signal<string | null>(null);

    // PDF / Print loading state
    pdfLoading = signal(false);

    // Pagination
    pageNumber = 1;
    pageSize = 20;
    first = 0;

    // Label of the vehicle the statement is currently scoped to (empty = all vehicles)
    get selectedVehicleLabel(): string {
        if (!this.selectedVehicleId) return '';
        return this.vehicles().find(v => v.id === this.selectedVehicleId)?.label ?? '';
    }

    customerFetchFn = (req: LazyRequest): Observable<LazyResponse<CustomerResponse>> =>
        this.customerService.getCustomers({
            search: req.search,
            pageNumber: req.pageNumber,
            pageSize: req.pageSize
        }).pipe(map(res => ({ items: res.data, totalCount: res.pagination.totalCount })));

    ngOnInit(): void {}

    onCustomerSelected(customer: CustomerResponse): void {
        this.selectedCustomer = customer;
        // Reset any prior vehicle filter and load this customer's active vehicles
        this.selectedVehicleId = null;
        this.vehicles.set([]);
        this.vehicleService.getByCustomer(customer.id, true)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (vehicles) => this.vehicles.set(vehicles),
                error: () => this.vehicles.set([])
            });
    }

    onCustomerCleared(): void {
        this.selectedCustomer = null;
        this.selectedVehicleId = null;
        this.vehicles.set([]);
        this.summary.set(null);
    }

    onVehicleChange(): void {
        // Re-run the report scoped to the selected vehicle (or all vehicles when cleared)
        if (this.selectedCustomer && this.summary()) {
            this.generateReport();
        }
    }

    generateReport(): void {
        if (!this.selectedCustomer) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Warning',
                detail: 'Please select a customer',
                life: 3000
            });
            return;
        }

        this.pageNumber = 1;
        this.first = 0;
        this.loadReport();
    }

    private loadReport(): void {
        if (!this.selectedCustomer) return;

        this.loading.set(true);
        this.error.set(null);

        const query: CustomerAccountSummaryQuery = {
            customerId: this.selectedCustomer.id,
            fromDate: this.fromDate ? this.toLocalDateString(this.fromDate) : undefined,
            toDate: this.toDate ? this.toLocalDateString(this.toDate) : undefined,
            customerVehicleId: this.selectedVehicleId ?? undefined,
            pageNumber: this.pageNumber,
            pageSize: this.pageSize
        };

        this.summaryService
            .getAccountSummary(this.selectedCustomer.id, query)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (data) => {
                    this.summary.set(data);
                    this.loading.set(false);
                },
                error: (err) => {
                    console.error('Error loading account summary:', err);
                    this.error.set('Failed to load account summary. Please try again.');
                    this.loading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load customer account summary',
                        life: 5000
                    });
                }
            });
    }

    onPageChange(event: PaginatorState): void {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? 20;
        this.first = event.first ?? 0;
        this.loadReport();
    }

    onDownloadPdf(): void {
        if (!this.selectedCustomer || !this.summary()) return;

        this.pdfLoading.set(true);

        const query: CustomerAccountSummaryQuery = {
            customerId: this.selectedCustomer.id,
            fromDate: this.fromDate ? this.toLocalDateString(this.fromDate) : undefined,
            toDate: this.toDate ? this.toLocalDateString(this.toDate) : undefined,
            customerVehicleId: this.selectedVehicleId ?? undefined,
            pageNumber: 1,
            pageSize: 2147483647
        };

        this.summaryService
            .downloadStatementPdf(this.selectedCustomer.id, query)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (blob) => {
                    const url = window.URL.createObjectURL(blob);
                    const anchor = document.createElement('a');
                    const s = this.summary()!;
                    const dateStr = new Date().toISOString().split('T')[0];
                    anchor.href = url;
                    anchor.download = `account-statement-${s.customerCode || 'customer'}-${dateStr}.pdf`;
                    anchor.click();
                    window.URL.revokeObjectURL(url);
                    this.pdfLoading.set(false);
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: 'PDF downloaded successfully',
                        life: 3000
                    });
                },
                error: () => {
                    this.pdfLoading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to generate PDF. Please try again.',
                        life: 5000
                    });
                }
            });
    }

    onPrint(): void {
        if (!this.selectedCustomer || !this.summary()) return;

        this.pdfLoading.set(true);

        const query: CustomerAccountSummaryQuery = {
            customerId: this.selectedCustomer.id,
            fromDate: this.fromDate ? this.toLocalDateString(this.fromDate) : undefined,
            toDate: this.toDate ? this.toLocalDateString(this.toDate) : undefined,
            customerVehicleId: this.selectedVehicleId ?? undefined,
            pageNumber: 1,
            pageSize: 2147483647
        };

        this.summaryService
            .downloadStatementPdf(this.selectedCustomer.id, query)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (blob) => {
                    const url = window.URL.createObjectURL(blob);
                    const printWindow = window.open(url, '_blank');
                    setTimeout(() => window.URL.revokeObjectURL(url), 60000);
                    this.pdfLoading.set(false);
                    if (!printWindow) {
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Pop-up blocked',
                            detail: 'Please allow pop-ups for this site and try again.',
                            life: 6000
                        });
                    }
                },
                error: () => {
                    this.pdfLoading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to generate PDF for printing. Please try again.',
                        life: 5000
                    });
                }
            });
    }

    goBack(): void {
        this.router.navigate(['/sales/customers']);
    }

    formatCurrency(value: number | undefined | null): string {
        const numValue = value ?? 0;
        if (isNaN(numValue)) {
            const currency = this.currencyService.selectedCurrency();
            return this.currencyService.formatCurrency(0, currency);
        }
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(numValue, currency);
    }

    formatDate(date: string | undefined): string {
        if (!date) return '-';
        return new Date(date).toLocaleDateString('en-IN', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    getStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        switch (status) {
            case 'PAID':
                return 'success';
            case 'ISSUED':
                return 'info';
            case 'PARTIALLY_PAID':
                return 'warn';
            case 'OVERDUE':
                return 'danger';
            case 'CANCELLED':
                return 'secondary';
            default:
                return 'info';
        }
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    // toISOString() converts to UTC which shifts dates in non-UTC timezones.
    // This helper returns "YYYY-MM-DD" in local time so the backend receives
    // the date the user actually selected.
    private toLocalDateString(date: Date): string {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }
}
