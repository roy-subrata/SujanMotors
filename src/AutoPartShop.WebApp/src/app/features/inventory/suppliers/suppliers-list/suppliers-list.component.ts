import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { MenuModule, Menu } from 'primeng/menu';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { Select } from 'primeng/select';
import { ConfirmationService, MessageService } from 'primeng/api';
import { MenuItem } from 'primeng/api';
import { SupplierService, SupplierResponse } from '../../services/supplier.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-suppliers-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, ConfirmDialogModule, TooltipModule, TagModule, MenuModule, RippleModule, ToastModule, Select],
  providers: [ConfirmationService, MessageService],
  templateUrl: './suppliers-list.component.html',
  styleUrls: ['./suppliers-list.component.css']
})
export class SuppliersListComponent implements OnInit {
  @ViewChild('actionMenu') actionMenu!: Menu;

  suppliers: SupplierResponse[] = [];
  loading = false;
  totalRecords = 0;
  pageSize = 10;
  pageNumber = 1;
  first = 0;
  searchTerm = '';
  pageSizeOptions = [10, 20, 50];

  actionMenuItems: MenuItem[] = [];
  selectedSupplier: SupplierResponse | null = null;

  Math = Math;

  private readonly supplierService = inject(SupplierService);
  private readonly currencyService = inject(CurrencyService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.supplierService.getSuppliers({
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      search: this.searchTerm
    }).subscribe({
      next: (response) => {
        this.suppliers = response.data;
        this.totalRecords = response.pagination.totalCount;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load suppliers'
        });
        console.error('Error loading suppliers:', error);
        this.loading = false;
      }
    });
  }

  // Filter methods
  hasActiveFilters(): boolean {
    return !!this.searchTerm;
  }

  onSearch(): void {
    this.resetPagination();
    this.loadData();
  }

  onSearchClear(): void {
    this.searchTerm = '';
    this.resetPagination();
    this.loadData();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.resetPagination();
    this.loadData();
  }

  private resetPagination(): void {
    this.pageNumber = 1;
    this.first = 0;
  }

  refreshData(): void {
    this.loadData();
  }

  // Pagination
  onPageChange(event: any): void {
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
      return;
    }
    this.first = event.first;
    this.pageSize = event.rows;
    this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
    this.loadData();
  }

  // Action menu
  showActionMenu(event: Event, supplier: SupplierResponse): void {
    this.selectedSupplier = supplier;

    this.actionMenuItems = [
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => this.viewSupplier(supplier)
      },
      {
        label: 'Edit Supplier',
        icon: 'pi pi-pencil',
        command: () => this.onEditClick(supplier)
      },
      { separator: true },
      ...(supplier.isActive
        ? [{
            label: 'Deactivate',
            icon: 'pi pi-times-circle',
            command: () => this.deactivateSupplier(supplier.id)
          }]
        : [{
            label: 'Activate',
            icon: 'pi pi-check-circle',
            command: () => this.activateSupplier(supplier.id)
          }]),
      { separator: true },
      {
        label: 'Payment Accounts',
        icon: 'pi pi-credit-card',
        command: () => this.viewPaymentAccounts(supplier)
      },
      {
        label: 'Record Payment',
        icon: 'pi pi-wallet',
        command: () => this.recordPayment(supplier)
      },
      {
        label: 'Account Summary',
        icon: 'pi pi-chart-line',
        command: () => this.viewAccountSummary(supplier)
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => this.onDeleteClick(supplier),
        styleClass: 'text-red-600'
      }
    ];

    this.actionMenu.toggle(event);
  }

  // Navigation
  createSupplier(): void {
    this.router.navigate(['/inventory/suppliers/create']);
  }

  viewSupplier(supplier: SupplierResponse): void {
    this.router.navigate(['/inventory/suppliers/detail'], { queryParams: { id: supplier.id } });
  }

  onEditClick(supplier: SupplierResponse): void {
    this.router.navigate(['/inventory/suppliers/edit'], { queryParams: { id: supplier.id, mode: 'edit' } });
  }

  recordPayment(supplier: SupplierResponse): void {
    this.router.navigate(['/procurement/supplier-payments/new'], { queryParams: { supplierId: supplier.id } });
  }

  viewAccountSummary(supplier: SupplierResponse): void {
    this.router.navigate(['/procurement/supplier-account-summary'], { queryParams: { supplierId: supplier.id } });
  }

  viewPaymentAccounts(supplier: SupplierResponse): void {
    this.router.navigate(['/inventory/suppliers/payment-accounts'], { queryParams: { supplierId: supplier.id } });
  }

  // CRUD
  onDeleteClick(supplier: SupplierResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete supplier '${supplier.name}'? This action cannot be undone.`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.supplierService.deleteSupplier(supplier.id).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Supplier deleted successfully' });
            this.loadData();
          },
          error: (error) => {
            this.messageService.add({ severity: 'error', summary: 'Error', detail: error?.error?.message || 'Failed to delete supplier' });
          }
        });
      }
    });
  }

  private activateSupplier(supplierId: string): void {
    this.supplierService.activateSupplier(supplierId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Supplier activated successfully' });
        this.loadData();
      },
      error: (error) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: error?.error?.message || 'Failed to activate supplier' });
      }
    });
  }

  private deactivateSupplier(supplierId: string): void {
    this.supplierService.deactivateSupplier(supplierId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Supplier deactivated successfully' });
        this.loadData();
      },
      error: (error) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: error?.error?.message || 'Failed to deactivate supplier' });
      }
    });
  }

  // Utility methods
  formatStatus(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }

  formatCurrency(amount: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(amount, currency);
  }

  getRatingClass(rating: number): string {
    if (rating >= 4) return 'rating-excellent';
    if (rating >= 3) return 'rating-good';
    if (rating >= 2) return 'rating-fair';
    return 'rating-poor';
  }
}
