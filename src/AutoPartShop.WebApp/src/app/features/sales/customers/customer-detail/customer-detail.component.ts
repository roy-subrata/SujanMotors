import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
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
import { CustomerPaymentService, CustomerPaymentHistorySummary, PaymentHistoryItem } from '../../services/customer-payment.service';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    TagModule,
    ToastModule,
    TabsModule,
    TableModule,
    SelectModule,
    DatePickerModule,
    TooltipModule,
    SkeletonModule,
    AvatarModule
  ],
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
  activeTabIndex = signal(0);

  // Filters for payment history
  statusFilter = signal<string>('');
  dateRange = signal<Date[]>([]);

  statusOptions = [
    { label: 'All Statuses', value: '' },
    { label: 'Completed', value: 'COMPLETED' },
    { label: 'Pending', value: 'PENDING' },
    { label: 'Failed', value: 'FAILED' },
    { label: 'Cancelled', value: 'CANCELLED' },
    { label: 'Refunded', value: 'REFUNDED' }
  ];

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
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

  get filteredPayments(): PaymentHistoryItem[] {
    const payments = this.paymentSummary()?.paymentHistory || [];
    let filtered = [...payments];

    // Filter by status
    if (this.statusFilter()) {
      filtered = filtered.filter(p => p.status === this.statusFilter());
    }

    // Filter by date range
    const dateRange = this.dateRange();
    if (dateRange && dateRange.length === 2 && dateRange[0] && dateRange[1]) {
      const startDate = new Date(dateRange[0]).setHours(0, 0, 0, 0);
      const endDate = new Date(dateRange[1]).setHours(23, 59, 59, 999);

      filtered = filtered.filter(p => {
        const paymentDate = new Date(p.paymentDate).getTime();
        return paymentDate >= startDate && paymentDate <= endDate;
      });
    }

    return filtered;
  }

  clearFilters(): void {
    this.statusFilter.set('');
    this.dateRange.set([]);
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

  getCreditStatusSeverity(): 'success' | 'warn' | 'danger' {
    const customer = this.customer();
    if (!customer) return 'success';

    const availableCredit = customer.creditLimit - customer.currentBalance;
    const percentageUsed = (customer.currentBalance / customer.creditLimit) * 100;

    if (percentageUsed >= 90) return 'danger';
    if (percentageUsed >= 70) return 'warn';
    return 'success';
  }

  getCreditStatusText(): string {
    const customer = this.customer();
    if (!customer) return 'Good';

    const percentageUsed = (customer.currentBalance / customer.creditLimit) * 100;

    if (percentageUsed >= 90) return 'Critical';
    if (percentageUsed >= 70) return 'Warning';
    return 'Good';
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
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR'
    }).format(value);
  }
}
