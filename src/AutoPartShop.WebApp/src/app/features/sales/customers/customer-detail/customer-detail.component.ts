import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { AvatarModule } from 'primeng/avatar';
import { MessageService } from 'primeng/api';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { CustomerPaymentService, CustomerPaymentHistorySummary } from '../../services/customer-payment.service';
import { AppCurrencyPipe } from '../../../../shared/pipes/app-currency.pipe';

@Component({
    selector: 'app-customer-detail',
    standalone: true,
    imports: [CommonModule, ButtonModule, TagModule, ToastModule, TooltipModule, AvatarModule, AppCurrencyPipe],
    providers: [MessageService],
    templateUrl: './customer-detail.component.html',
    styleUrls: ['./customer-detail.component.css']
})
export class CustomerDetailComponent implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly customerService = inject(CustomerService);
    private readonly paymentService = inject(CustomerPaymentService);
    private readonly messageService = inject(MessageService);

    customerId = signal<string>('');
    customer = signal<CustomerResponse | null>(null);
    paymentSummary = signal<CustomerPaymentHistorySummary | null>(null);
    loading = signal(true);

    ngOnInit(): void {
        this.route.queryParams.subscribe((params) => {
            const id = params['id'];
            if (id) {
                this.customerId.set(id);
                this.loadCustomerData();
            } else {
                this.router.navigate(['/sales/customers']);
            }
        });
    }

    loadCustomerData(): void {
        this.loading.set(true);

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

    viewPayments(): void {
        this.router.navigate(['/sales/customer-payments'], {
            queryParams: { customerId: this.customerId() }
        });
    }

    viewAccountSummary(): void {
        this.router.navigate(['/sales/customer-account-summary']);
    }

    viewPaymentSummary(): void {
        this.router.navigate(['/sales/customer-payments/summary', this.customerId()]);
    }

    goBack(): void {
        this.router.navigate(['/sales/customers']);
    }
}
