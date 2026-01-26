import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { ConfirmationService, MessageService } from 'primeng/api';
import { MenuItem } from 'primeng/api';
import { SupplierService, SupplierResponse } from '../../services/supplier.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-suppliers-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, ConfirmDialogModule, TooltipModule, TagModule, ContextMenuModule, RippleModule, ToastModule],
  providers: [ConfirmationService, MessageService],
  templateUrl: './suppliers-list.component.html',
  styleUrls: ['./suppliers-list.component.css']
})
export class SuppliersListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  suppliers: SupplierResponse[] = [];
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;
  searchTerm = '';

  contextMenuItems: MenuItem[] = [];
  selectedSupplier: SupplierResponse | null = null;

  private readonly supplierService = inject(SupplierService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  constructor() {}

  ngOnInit(): void {
    this.loadSuppliers();
    this.initializeContextMenu();
  }

  /**
   * Load suppliers from API
   */
  loadSuppliers(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): void {
    if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
      pageNumber = 1;
    }
    if (!pageSize || isNaN(pageSize) || pageSize < 1) {
      pageSize = 10;
    }

    this.loading = true;
    this.supplierService.getSuppliers({
      pageNumber: pageNumber,
      pageSize: pageSize,
      search: searchTerm
    }).subscribe({
      next: (response) => {
        this.suppliers = response.data;
        this.totalRecords = response.pagination.totalCount;
        this.rows = response.pagination.pageSize;
        this.currentPage = response.pagination.pageNumber;
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

  /**
   * Navigate to create supplier page
   */
  onCreateClick(): void {
    this.router.navigate(['/inventory/suppliers/create']);
  }

  /**
   * Handle search
   */
  onSearch(query: string): void {
    this.searchTerm = query;
    this.loadSuppliers(1, this.rows, query);
  }

  /**
   * Handle search clear
   */
  onSearchClear(): void {
    this.searchTerm = '';
    this.loadSuppliers(1, this.rows);
  }

  /**
   * Initialize context menu items
   */
  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedSupplier) {
            this.onEditClick(this.selectedSupplier);
          }
        }
      },
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedSupplier) {
            this.viewSupplier(this.selectedSupplier);
          }
        }
      },
      { separator: true },
      {
        label: 'Activate',
        icon: 'pi pi-check',
        command: () => {
          if (this.selectedSupplier && !this.selectedSupplier.isActive) {
            this.activateSupplier(this.selectedSupplier.id);
          }
        },
        visible: this.selectedSupplier ? !this.selectedSupplier.isActive : false
      },
      {
        label: 'Deactivate',
        icon: 'pi pi-times',
        command: () => {
          if (this.selectedSupplier && this.selectedSupplier.isActive) {
            this.deactivateSupplier(this.selectedSupplier.id);
          }
        },
        visible: this.selectedSupplier ? this.selectedSupplier.isActive : false
      },
      { separator: true },
      {
        label: 'Payment Accounts',
        icon: 'pi pi-credit-card',
        command: () => {
          if (this.selectedSupplier) {
            this.viewPaymentAccounts(this.selectedSupplier);
          }
        }
      },
      {
        label: 'View Payment Summary',
        icon: 'pi pi-chart-bar',
        command: () => {
          if (this.selectedSupplier) {
            this.viewPaymentSummary(this.selectedSupplier);
          }
        }
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedSupplier) {
            this.onDeleteClick(this.selectedSupplier);
          }
        },
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  /**
   * Show context menu for supplier
   */
  showContextMenu(event: MouseEvent, supplier: SupplierResponse): void {
    this.selectedSupplier = supplier;
    this.initializeContextMenu();
    if (this.contextMenu) {
      this.contextMenu.show(event);
    }
  }

  /**
   * Activate supplier
   */
  private activateSupplier(supplierId: string): void {
    this.supplierService.activateSupplier(supplierId).subscribe({
      next: (updatedSupplier) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Supplier activated successfully'
        });
        const index = this.suppliers.findIndex(s => s.id === supplierId);
        if (index !== -1) {
          this.suppliers[index] = updatedSupplier;
          this.suppliers = [...this.suppliers];
        }
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to activate supplier'
        });
      }
    });
  }

  /**
   * Deactivate supplier
   */
  private deactivateSupplier(supplierId: string): void {
    this.supplierService.deactivateSupplier(supplierId).subscribe({
      next: (updatedSupplier) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Supplier deactivated successfully'
        });
        const index = this.suppliers.findIndex(s => s.id === supplierId);
        if (index !== -1) {
          this.suppliers[index] = updatedSupplier;
          this.suppliers = [...this.suppliers];
        }
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to deactivate supplier'
        });
      }
    });
  }

  /**
   * Navigate to edit supplier page
   */
  onEditClick(supplier: SupplierResponse): void {
    this.router.navigate(['/inventory/suppliers/edit'], { queryParams: { id: supplier.id, mode: 'edit' } });
  }

  /**
   * Navigate to view supplier details page
   */
  viewSupplier(supplier: SupplierResponse): void {
    this.router.navigate(['/inventory/suppliers/view'], { queryParams: { id: supplier.id, mode: 'view' } });
  }

  /**
   * Confirm and delete supplier
   */
  onDeleteClick(supplier: SupplierResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete supplier '${supplier.name}'? This action cannot be undone.`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteSupplier(supplier.id);
      }
    });
  }

  /**
   * Delete supplier from API
   */
  private deleteSupplier(supplierId: string): void {
    this.supplierService.deleteSupplier(supplierId).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Supplier deleted successfully'
        });
        this.loadSuppliers(this.currentPage, this.rows, this.searchTerm);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to delete supplier'
        });
        console.error('Error deleting supplier:', error);
      }
    });
  }

  /**
   * Navigate to record payment for supplier
   */
  recordPayment(supplier: SupplierResponse): void {
    this.router.navigate(['/procurement/supplier-payments/new'], { queryParams: { supplierId: supplier.id } });
  }

  /**
   * Navigate to view payment summary for supplier
   */
  viewPaymentSummary(supplier: SupplierResponse): void {
    this.router.navigate(['/procurement/supplier-payments/summary', supplier.id]);
  }

  /**
   * Navigate to view payment accounts for supplier
   */
  viewPaymentAccounts(supplier: SupplierResponse): void {
    this.router.navigate(['/inventory/suppliers/payment-accounts'], { queryParams: { supplierId: supplier.id } });
  }

  /**
   * Handle pagination change
   */
  onPageChange(event: any): void {
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
      return;
    }
    const pageNumber = Math.floor(event.first / event.rows) + 1;
    this.loadSuppliers(pageNumber, event.rows, this.searchTerm);
  }

  /**
   * Get status badge severity
   */
  getStatusSeverity(isActive: boolean): string {
    return isActive ? 'success' : 'danger';
  }

  /**
   * Get rating badge
   */
  getRatingClass(rating: number): string {
    if (rating >= 4) return 'rating-excellent';
    if (rating >= 3) return 'rating-good';
    if (rating >= 2) return 'rating-fair';
    return 'rating-poor';
  }

  /**
   * Format credit limit for display
   */
  formatCreditLimit(creditLimit: number): string {
    return `₹${creditLimit.toFixed(2)}`;
  }
}
