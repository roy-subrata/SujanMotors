import { Component, OnInit, inject, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { TableModule, Table } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { RippleModule } from 'primeng/ripple';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { I18nService } from '../../../../shared/services/i18n.service';
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { CustomerTypeService, ItemResponse } from '@/shared/services/CountryService';
import { tap } from 'rxjs';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';

@Component({
    selector: 'app-customers-list',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, CardModule, PaginatorModule, SelectModule, ButtonModule, InputTextModule, ToastModule, ConfirmDialogModule, ContextMenuModule, TagModule, TooltipModule, RippleModule, DatePickerModule],
    providers: [MessageService, ConfirmationService],
    templateUrl: './customers-list.component.html',
    styleUrls: ['./customers-list.component.css']
})
export class CustomersListComponent implements OnInit {
    @ViewChild('dt') dt!: Table;
    @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

    private readonly customerService = inject(CustomerService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly customerTypeService = inject(CustomerTypeService);
    readonly i18n = inject(I18nService);

    customers = signal<CustomerResponse[]>([]);
    loading = signal(false);
    error = signal<string | null>(null);

    searchTerm = signal('');
    customerType = signal('');

    selectedCustomer: CustomerResponse | null = null;
    contextMenuItems: MenuItem[] = [];
    customerTypes: ItemResponse[] = [];
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 25;
    pageSizeOptions = [10, 25, 50, 100];

    ngOnInit(): void {
        this.loadCustomerTypes();
        this.initContextMenu();
    }

    private loadCustomerTypes() {
        this.customerTypeService
            .findAll({ query: '', page: 1, pageSize: 100 })
            .pipe(
                tap({
                    next: (value) => {
                        this.customerTypes = value.items;
                    },
                    error: (error) => {
                        console.error('Failed to call customer types list:', error);
                        this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Could not load customer types.' });
                    }
                })
            )
            .subscribe();
    }

    initContextMenu(): void {
        this.contextMenuItems = [
            {
                label: this.i18n.t('customers.editCustomer'),
                icon: 'pi pi-pencil',
                command: () => this.editCustomer(this.selectedCustomer!)
            },
            {
                label: this.i18n.t('common.actions.viewDetails'),
                icon: 'pi pi-eye',
                command: () => this.viewCustomer(this.selectedCustomer!)
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.activate'),
                icon: 'pi pi-check-circle',
                command: () => this.activateCustomer(this.selectedCustomer!),
                visible: this.selectedCustomer?.status !== 'ACTIVE'
            },
            {
                label: this.i18n.t('common.actions.deactivate'),
                icon: 'pi pi-times-circle',
                command: () => this.deactivateCustomer(this.selectedCustomer!),
                visible: this.selectedCustomer?.status === 'ACTIVE'
            },
            {
                label: this.i18n.t('common.actions.suspend'),
                icon: 'pi pi-ban',
                command: () => this.suspendCustomer(this.selectedCustomer!),
                visible: this.selectedCustomer?.status !== 'SUSPENDED'
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.delete'),
                icon: 'pi pi-trash',
                styleClass: 'p-menuitem-danger',
                command: () => this.confirmDelete(this.selectedCustomer!)
            }
        ];
    }

    loadCustomers(): void {
        this.loading.set(true);
        this.error.set(null);

        this.customerService
            .getCustomers({
                search: this.searchTerm(),
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                customerType: this.customerType()
            })
            .subscribe({
                next: (response) => {
                    this.customers.set(response.data);
                    this.totalRecords = response.pagination.totalCount;
                    this.loading.set(false);
                },
                error: (err) => {
                    this.error.set(this.i18n.t('customers.messages.loadFailed'));
                    this.loading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('customers.messages.loadFailed')
                    });
                    console.error('Error loading customers:', err);
                }
            });
    }

    onSearch(event: any): void {
        this.searchTerm.set(event.target.value);
        this.pageNumber = 1;
        this.loadCustomers();
    }

    onFilterChange(value: string) {
        this.customerType.set(value);
        this.loadCustomers();
    }

    onPageChange(event: PaginatorState): void {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.loadCustomers();
    }

    clearSearch(): void {
        this.searchTerm.set('');
        this.pageNumber = 1;
        this.customerType.set('');
        this.loadCustomers();
    }

    onLazyLoad(event: any): void {
        const page = Math.floor((event.first || 0) / (event.rows || 10)) + 1;
        this.pageNumber = page;
        this.pageSize = event.rows || 10;
        this.loadCustomers();
    }

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
                        this.loadCustomers();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to activate customer' });
                        console.error('Error activating customer:', err);
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
                        this.loadCustomers();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to deactivate customer' });
                        console.error('Error deactivating customer:', err);
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
                        this.loadCustomers();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to suspend customer' });
                        console.error('Error suspending customer:', err);
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
                        this.loadCustomers();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete customer' });
                        console.error('Error deleting customer:', err);
                    }
                });
            }
        });
    }

    /**
     * Show context menu programmatically
     */
    showContextMenu(event: MouseEvent, customer: CustomerResponse): void {
        event.preventDefault();
        event.stopPropagation();
        this.selectedCustomer = customer;
        this.updateContextMenuItems(customer);
        this.contextMenu?.show(event);
    }

    onContextMenuSelect(event: any): void {
        const customer = event.data as CustomerResponse;
        this.selectedCustomer = customer;
        this.updateContextMenuItems(customer);
    }

    /**
     * Update context menu items based on customer status
     */
    private updateContextMenuItems(customer: CustomerResponse): void {
        this.contextMenuItems = [
            {
                label: 'Edit Customer',
                icon: 'pi pi-pencil',
                command: () => this.editCustomer(customer)
            },
            {
                label: 'View Details',
                icon: 'pi pi-eye',
                command: () => this.viewCustomer(customer)
            },
            { separator: true },
            ...(customer.status !== 'ACTIVE'
                ? [
                      {
                          label: 'Activate',
                          icon: 'pi pi-check-circle',
                          command: () => this.activateCustomer(customer)
                      }
                  ]
                : []),
            ...(customer.status === 'ACTIVE'
                ? [
                      {
                          label: 'Deactivate',
                          icon: 'pi pi-times-circle',
                          command: () => this.deactivateCustomer(customer)
                      }
                  ]
                : []),
            ...(customer.status !== 'SUSPENDED'
                ? [
                      {
                          label: 'Suspend',
                          icon: 'pi pi-ban',
                          command: () => this.suspendCustomer(customer)
                      }
                  ]
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
                styleClass: 'p-menuitem-danger',
                command: () => this.confirmDelete(customer)
            }
        ];
    }

    getStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' {
        const severities: Record<string, 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast'> = {
            ACTIVE: 'success',
            INACTIVE: 'secondary',
            SUSPENDED: 'warn',
            BLACKLISTED: 'danger'
        };
        return severities[status] || 'secondary';
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
