import { Component, EventEmitter, Input, Output, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { ConfirmationService, MessageService } from 'primeng/api';
import { MenuItem } from 'primeng/api';
import { SupplierService, SupplierResponse } from '../../services/supplier.service';

@Component({
  selector: 'app-suppliers-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, ConfirmDialogModule, TooltipModule, TagModule, ContextMenuModule, RippleModule],
  providers: [ConfirmationService, MessageService],
  templateUrl: './suppliers-list.component.html',
  styleUrls: ['./suppliers-list.component.css']
})
export class SuppliersListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() suppliers: SupplierResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;

  @Output() editClick = new EventEmitter<SupplierResponse>();
  @Output() deleteClick = new EventEmitter<SupplierResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() supplierDeleted = new EventEmitter<void>();

  contextMenuItems: MenuItem[] = [];
  selectedSupplier: SupplierResponse | null = null;

  private readonly supplierService = inject(SupplierService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  constructor() {}

  ngOnInit(): void {
    this.initializeContextMenu();
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
            this.onEditClick(this.selectedSupplier);
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
   * Edit supplier
   */
  onEditClick(supplier: SupplierResponse): void {
    this.editClick.emit(supplier);
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
        this.supplierDeleted.emit();
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
   * Handle pagination change
   */
  onPageChange(event: any): void {
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
      return;
    }
    const pageNumber = Math.floor(event.first / event.rows) + 1;
    this.pageChange.emit({
      page: pageNumber,
      rows: event.rows
    });
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
