import { Component, OnInit, inject, signal, ViewChild, HostListener } from '@angular/core';
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

@Component({
  selector: 'app-customers-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    ContextMenuModule,
    TagModule,
    TooltipModule,
    RippleModule
  ],
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

  customers = signal<CustomerResponse[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  currentPage = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = signal(0);
  searchTerm = signal('');

  selectedCustomer: CustomerResponse | null = null;
  contextMenuItems: MenuItem[] = [];

  ngOnInit(): void {
    this.loadCustomers();
    this.initContextMenu();
  }

  initContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => this.viewCustomer(this.selectedCustomer!)
      },
      {
        label: 'Edit Customer',
        icon: 'pi pi-pencil',
        command: () => this.editCustomer(this.selectedCustomer!)
      },
      { separator: true },
      {
        label: 'Activate',
        icon: 'pi pi-check-circle',
        command: () => this.activateCustomer(this.selectedCustomer!),
        visible: this.selectedCustomer?.status !== 'ACTIVE'
      },
      {
        label: 'Deactivate',
        icon: 'pi pi-times-circle',
        command: () => this.deactivateCustomer(this.selectedCustomer!),
        visible: this.selectedCustomer?.status === 'ACTIVE'
      },
      {
        label: 'Suspend',
        icon: 'pi pi-ban',
        command: () => this.suspendCustomer(this.selectedCustomer!),
        visible: this.selectedCustomer?.status !== 'SUSPENDED'
      },
      { separator: true },
      {
        label: 'Record Payment',
        icon: 'pi pi-wallet',
        command: () => this.recordPayment(this.selectedCustomer!)
      },
      {
        label: 'View Invoices',
        icon: 'pi pi-file',
        command: () => this.viewInvoices(this.selectedCustomer!)
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        styleClass: 'p-menuitem-danger',
        command: () => this.confirmDelete(this.selectedCustomer!)
      }
    ];
  }

  loadCustomers(): void {
    this.loading.set(true);
    this.error.set(null);

    this.customerService.getCustomers(this.currentPage(), this.pageSize(), this.searchTerm() || undefined).subscribe({
      next: (response) => {
        this.customers.set(response.data);
        this.totalCount.set(response.pagination.totalCount);
        this.totalPages.set(response.pagination.totalPages);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load customers');
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load customers' });
        console.error('Error loading customers:', err);
      }
    });
  }

  onSearch(event: any): void {
    this.searchTerm.set(event.target.value);
    this.currentPage.set(1);
    this.loadCustomers();
  }

  clearSearch(): void {
    this.searchTerm.set('');
    this.currentPage.set(1);
    this.loadCustomers();
  }

  onLazyLoad(event: any): void {
    const page = Math.floor((event.first || 0) / (event.rows || 10)) + 1;
    this.currentPage.set(page);
    this.pageSize.set(event.rows || 10);
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

  viewInvoices(customer: CustomerResponse): void {
    this.router.navigate(['/sales/invoices'], { queryParams: { customerId: customer.id } });
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
      ...(customer.status !== 'ACTIVE' ? [{
        label: 'Activate',
        icon: 'pi pi-check-circle',
        command: () => this.activateCustomer(customer)
      }] : []),
      ...(customer.status === 'ACTIVE' ? [{
        label: 'Deactivate',
        icon: 'pi pi-times-circle',
        command: () => this.deactivateCustomer(customer)
      }] : []),
      ...(customer.status !== 'SUSPENDED' ? [{
        label: 'Suspend',
        icon: 'pi pi-ban',
        command: () => this.suspendCustomer(customer)
      }] : []),
      { separator: true },
      {
        label: 'Record Payment',
        icon: 'pi pi-wallet',
        command: () => this.recordPayment(customer)
      },
      {
        label: 'View Invoices',
        icon: 'pi pi-file',
        command: () => this.viewInvoices(customer)
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
      'ACTIVE': 'success',
      'INACTIVE': 'secondary',
      'SUSPENDED': 'warn',
      'BLACKLISTED': 'danger'
    };
    return severities[status] || 'secondary';
  }

  getCreditStatus(customer: CustomerResponse): { severity: 'success' | 'warn' | 'danger'; text: string } {
    const usedCredit = customer.creditLimit - customer.availableCredit;
    const usagePercent = customer.creditLimit > 0 ? (usedCredit / customer.creditLimit) * 100 : 0;
    
    if (usagePercent >= 100) {
      return { severity: 'danger', text: 'Over Limit' };
    } else if (usagePercent >= 80) {
      return { severity: 'warn', text: 'Near Limit' };
    }
    return { severity: 'success', text: 'Good' };
  }

  @HostListener('document:keydown', ['$event'])
  handleKeyboardShortcuts(event: KeyboardEvent): void {
    // Ctrl+N: New Customer
    if (event.ctrlKey && event.key === 'n') {
      event.preventDefault();
      this.createCustomer();
    }
    // F5: Refresh
    if (event.key === 'F5' && !event.ctrlKey) {
      event.preventDefault();
      this.loadCustomers();
    }
  }
}
