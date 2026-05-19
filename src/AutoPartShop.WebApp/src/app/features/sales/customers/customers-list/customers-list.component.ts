import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';

import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';

import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { CustomerTypeService, ItemResponse } from '@/shared/services/CountryService';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { tap } from 'rxjs';

@Component({
    selector: 'app-customers-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        Select,
        TagModule,
        MenuModule,
        TooltipModule,
        ToastModule,
        ConfirmDialogModule,
        PaginatorModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './customers-list.component.html',
    styleUrls: ['./customers-list.component.css']
})
export class CustomersListComponent implements OnInit {
    private readonly customerService = inject(CustomerService);
    private readonly currencyService = inject(CurrencyService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly customerTypeService = inject(CustomerTypeService);

    @ViewChild('actionMenu') actionMenu!: Menu;

    // Data state
    customers: CustomerResponse[] = [];
    selectedCustomer: CustomerResponse | null = null;
    loading = false;

    // Pagination state
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 10;
    first = 0;
    pageSizeOptions = [10, 20, 50];

    // Filter state
    searchTerm = '';
    filterType = '';

    // Dropdown options
    customerTypes: ItemResponse[] = [];

    // Action menu items
    actionMenuItems: MenuItem[] = [];

    // Expose Math for template
    Math = Math;

    ngOnInit(): void {
        this.loadCustomerTypes();
        this.loadData();
    }

    private loadCustomerTypes(): void {
        this.customerTypeService
            .findAll({ query: '', page: 1, pageSize: 100 })
            .pipe(
                tap({
                    next: (value) => {
                        this.customerTypes = value.items;
                    },
                    error: (error) => {
                        console.error('Failed to load customer types:', error);
                    }
                })
            )
            .subscribe();
    }

    loadData(): void {
        this.loading = true;

        this.customerService
            .getCustomers({
                search: this.searchTerm,
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                customerType: this.filterType
            })
            .subscribe({
                next: (response) => {
                    this.customers = response.data;
                    this.totalRecords = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Error loading customers:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load customers'
                    });
                    this.loading = false;
                }
            });
    }

    onLazyLoad(event: TableLazyLoadEvent): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    // Filter methods
    hasActiveFilters(): boolean {
        return !!(this.searchTerm || this.filterType);
    }

    onSearch(): void {
        this.resetPagination();
        this.loadData();
    }

    onFilterChange(): void {
        this.resetPagination();
        this.loadData();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterType = '';
        this.resetPagination();
        this.loadData();
    }

    private resetPagination(): void {
        this.pageNumber = 1;
        this.first = 0;
    }

    // Pagination
    onPageChange(event: PaginatorState): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    // Action menu
    showActionMenu(event: Event, customer: CustomerResponse): void {
        this.selectedCustomer = customer;

        this.actionMenuItems = [
            {
                label: 'View Details',
                icon: 'pi pi-eye',
                command: () => this.viewCustomer(customer)
            },
            {
                label: 'Edit Customer',
                icon: 'pi pi-pencil',
                command: () => this.editCustomer(customer)
            },
            { separator: true },
            ...(customer.status !== 'ACTIVE'
                ? [{
                    label: 'Activate',
                    icon: 'pi pi-check-circle',
                    command: () => this.activateCustomer(customer)
                }]
                : []),
            ...(customer.status === 'ACTIVE'
                ? [{
                    label: 'Deactivate',
                    icon: 'pi pi-times-circle',
                    command: () => this.deactivateCustomer(customer)
                }]
                : []),
            ...(customer.status !== 'SUSPENDED'
                ? [{
                    label: 'Suspend',
                    icon: 'pi pi-ban',
                    command: () => this.suspendCustomer(customer)
                }]
                : []),
            { separator: true },
            {
                label: 'Record Payment',
                icon: 'pi pi-wallet',
                command: () => this.recordPayment(customer)
            },
            { separator: true },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                command: () => this.confirmDelete(customer),
                styleClass: 'text-red-600'
            }
        ];

        this.actionMenu.toggle(event);
    }

    // CRUD operations
    createCustomer(): void {
        this.router.navigate(['/sales/customers/create']);
    }

    viewCustomer(customer: CustomerResponse): void {
        this.router.navigate(['/sales/customers/detail'], { queryParams: { id: customer.id } });
    }

    editCustomer(customer: CustomerResponse): void {
        this.router.navigate(['/sales/customers/edit'], { queryParams: { id: customer.id, mode: 'edit' } });
    }

    recordPayment(customer: CustomerResponse): void {
        this.router.navigate(['/sales/customer-payments/new'], { queryParams: { customerId: customer.id } });
    }

    activateCustomer(customer: CustomerResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to activate customer "${customer.fullName}"?`,
            header: 'Activate Customer',
            icon: 'pi pi-check-circle',
            accept: () => {
                this.customerService.activateCustomer(customer.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Customer activated successfully' });
                        this.loadData();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to activate customer' });
                    }
                });
            }
        });
    }

    deactivateCustomer(customer: CustomerResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to deactivate customer "${customer.fullName}"?`,
            header: 'Deactivate Customer',
            icon: 'pi pi-times-circle',
            accept: () => {
                this.customerService.deactivateCustomer(customer.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Customer deactivated successfully' });
                        this.loadData();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to deactivate customer' });
                    }
                });
            }
        });
    }

    suspendCustomer(customer: CustomerResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to suspend customer "${customer.fullName}"?`,
            header: 'Suspend Customer',
            icon: 'pi pi-ban',
            accept: () => {
                this.customerService.suspendCustomer(customer.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Customer suspended successfully' });
                        this.loadData();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to suspend customer' });
                    }
                });
            }
        });
    }

    confirmDelete(customer: CustomerResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete customer "${customer.fullName}"? This action cannot be undone.`,
            header: 'Delete Customer',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.customerService.deleteCustomer(customer.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Customer deleted successfully' });
                        this.loadData();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete customer' });
                    }
                });
            }
        });
    }

    refreshData(): void {
        this.loadData();
    }

    // Utility methods
    formatCurrency(amount: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currency);
    }

    getStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const severityMap: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            ACTIVE: 'success',
            INACTIVE: 'secondary',
            SUSPENDED: 'warn',
            BLACKLISTED: 'danger'
        };
        return severityMap[status] || 'secondary';
    }

    formatStatus(status: string): string {
        if (!status) return '-';
        return status
            .split('_')
            .map((word) => word.charAt(0) + word.slice(1).toLowerCase())
            .join(' ');
    }

    getBalanceStatus(customer: CustomerResponse): { severity: 'success' | 'warn' | 'danger'; text: string } {
        if (customer.dueAmount > 0) {
            return { severity: 'danger', text: 'Has Due' };
        } else if (customer.advanceAmount > 0) {
            return { severity: 'success', text: 'Has Advance' };
        }
        return { severity: 'success', text: 'Clear' };
    }
}
