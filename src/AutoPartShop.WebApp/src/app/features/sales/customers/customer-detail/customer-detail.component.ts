import { Component, OnInit, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { AvatarModule } from 'primeng/avatar';
import { MessageService } from 'primeng/api';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
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
    private readonly messageService = inject(MessageService);
    private readonly destroyRef = inject(DestroyRef);

    customerId = signal<string>('');
    customer = signal<CustomerResponse | null>(null);
    loading = signal(true);

    ngOnInit(): void {
        this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
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
        this.customerService.getCustomerById(this.customerId())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (customer) => {
                    this.customer.set(customer);
                    this.loading.set(false);
                },
                error: (error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load customer details'
                    });
                    console.error('Error loading customer:', error);
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
            case 'ACTIVE':   return 'success';
            case 'INACTIVE': return 'secondary';
            case 'SUSPENDED': return 'danger';
            default:         return 'info';
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

    goBack(): void {
        this.router.navigate(['/sales/customers']);
    }
}
