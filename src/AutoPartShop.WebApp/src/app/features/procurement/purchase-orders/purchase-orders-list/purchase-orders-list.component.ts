/* ============================================================
   PURCHASE ORDERS LIST COMPONENT
   ============================================================
   This component displays a list of purchase orders in a clean,
   modern table format.

   FEATURES:
   - Paginated data table
   - Status badges with color coding
   - Context menu for row actions
   - Responsive design

   TO REUSE THIS COMPONENT:
   1. Copy the entire component folder
   2. Update the data model (PurchaseOrderResponse)
   3. Update the service injection
   4. Modify context menu items as needed
   5. Adjust column definitions in HTML
   ============================================================ */

import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { ApplyAdvanceCreditDialogComponent } from '../apply-advance-credit/apply-advance-credit-dialog.component';

@Component({
  selector: 'app-purchase-orders-list',
  standalone: true,
  imports: [
    CommonModule,
    ContextMenuModule,
    ConfirmDialogModule,
    TooltipModule,
    RouterModule
  ],
  providers: [ConfirmationService, MessageService, DialogService],
  templateUrl: './purchase-orders-list.component.html',
  styleUrls: ['./purchase-orders-list.component.css']
})
export class PurchaseOrdersListComponent implements OnInit {
  /* ===================== VIEW CHILDREN ===================== */
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  /* ===================== INPUT PROPERTIES =====================
     These properties are passed from the parent component
     ============================================================ */
  @Input() purchaseOrders: PurchaseOrderResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() searchTerm = '';

  /* ===================== OUTPUT EVENTS =====================
     Events emitted to the parent component
     ========================================================== */
  @Output() editClick = new EventEmitter<PurchaseOrderResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() poDeleted = new EventEmitter<void>();

  /* ===================== COMPONENT STATE ===================== */
  contextMenuItems: MenuItem[] = [];
  selectedPurchaseOrder: PurchaseOrderResponse | null = null;
  pageSizeOptions = [10, 25, 50, 100];

  // Expose Math to the template for calculations
  Math = Math;

  /* ===================== DEPENDENCY INJECTION ===================== */
  private readonly poService = inject(PurchaseOrderService);
  private readonly currencyService = inject(CurrencyService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly dialogService = inject(DialogService);
  private readonly router = inject(Router);

  private dialogRef: DynamicDialogRef | null | undefined;

  /* ===================== COMPUTED PROPERTIES ===================== */

  /**
   * Calculate total number of pages based on total records and rows per page
   * Used in pagination display and navigation
   */
  get totalPages(): number {
    return Math.ceil(this.totalRecords / this.rows) || 1;
  }

  /* ===================== LIFECYCLE HOOKS ===================== */

  ngOnInit(): void {
    this.initializeContextMenu();
  }

  /* ===================== CONTEXT MENU ===================== */

  /**
   * Initialize context menu items
   * Add or modify menu items based on your requirements
   */
  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.viewDetails(this.selectedPurchaseOrder);
          }
        }
      },
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.onEditClick(this.selectedPurchaseOrder);
          }
        },
        visible: this.selectedPurchaseOrder ? this.selectedPurchaseOrder.status === 'DRAFT' : false
      },
      { separator: true },
      {
        label: 'Apply Advance Credit',
        icon: 'pi pi-credit-card',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.openApplyCreditDialog(this.selectedPurchaseOrder);
          }
        },
        visible: this.selectedPurchaseOrder ?
          (['CONFIRMED', 'PARTIAL', 'DELIVERED'].includes(this.selectedPurchaseOrder.status) &&
           this.selectedPurchaseOrder.outstandingAmount > 0) : false
      },
      {
        label: 'Record Payment',
        icon: 'pi pi-wallet',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.recordPayment(this.selectedPurchaseOrder);
          }
        },
        visible: this.selectedPurchaseOrder ?
          (['CONFIRMED', 'PARTIAL', 'DELIVERED'].includes(this.selectedPurchaseOrder.status) &&
           this.selectedPurchaseOrder.outstandingAmount > 0) : false
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.onDeleteClick(this.selectedPurchaseOrder);
          }
        },
        visible: this.selectedPurchaseOrder ? this.selectedPurchaseOrder.status === 'DRAFT' : false,
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  /**
   * Show context menu on row action button click or right-click
   */
  showContextMenu(event: MouseEvent, po: PurchaseOrderResponse): void {
    event.preventDefault();
    event.stopPropagation();
    this.selectedPurchaseOrder = po;
    this.initializeContextMenu();
    this.contextMenu?.show(event);
  }

  /* ===================== STATUS STYLING ===================== */

  /**
   * Get CSS class for status badge based on status value
   * Returns a class name that maps to CSS styling
   *
   * STATUS MAPPINGS:
   * - DRAFT, PENDING, PARTIAL → status-pending (amber)
   * - SUBMITTED, PROCESSING → status-submitted (blue)
   * - CONFIRMED, DELIVERED, PAID → status-confirmed (green)
   * - CANCELLED, CLOSED → status-cancelled (red)
   * - Others → status-default (gray)
   *
   * TO CUSTOMIZE: Add new status values and their corresponding classes
   */
  getStatusClass(status: string): string {
    const statusLower = status?.toLowerCase() || '';

    switch (statusLower) {
      case 'draft':
        return 'status-draft';
      case 'pending':
        return 'status-pending';
      case 'partial':
        return 'status-partial';
      case 'submitted':
        return 'status-submitted';
      case 'processing':
        return 'status-processing';
      case 'confirmed':
        return 'status-confirmed';
      case 'delivered':
        return 'status-delivered';
      case 'paid':
        return 'status-paid';
      case 'cancelled':
        return 'status-cancelled';
      case 'closed':
        return 'status-closed';
      case 'active':
        return 'status-active';
      case 'offline':
        return 'status-offline';
      default:
        return 'status-default';
    }
  }

  /**
   * Get status badge severity (legacy method for PrimeNG Tag)
   * Kept for backward compatibility
   */
  getStatusSeverity(status: string): string {
    switch (status?.toUpperCase()) {
      case 'DRAFT':
        return 'warning';
      case 'SUBMITTED':
        return 'info';
      case 'CONFIRMED':
        return 'success';
      case 'PARTIAL':
        return 'warning';
      case 'DELIVERED':
        return 'success';
      case 'CANCELLED':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  /* ===================== PAGINATION ===================== */

  /**
   * Navigate to a specific page
   * Emits pageChange event with new page number
   */
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.pageChange.emit({
      page: page,
      rows: this.rows
    });
  }

  /**
   * Handle page size change from dropdown
   * Resets to page 1 when page size changes
   */
  onPageSizeChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const newRows = parseInt(select.value, 10);
    this.pageChange.emit({
      page: 1, // Reset to first page when changing page size
      rows: newRows
    });
  }

  /**
   * Handle pagination change (legacy method for PrimeNG paginator)
   * Kept for backward compatibility
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

  /* ===================== FORMATTING ===================== */

  /**
   * Format currency value using the currency service
   * Uses the currently selected currency from settings
   */
  formatCurrency(value: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(value, currency);
  }

  /**
   * Format date for display
   * Uses Indian locale format (DD/MM/YYYY)
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }

  /* ===================== NAVIGATION ===================== */

  /**
   * Navigate to purchase order detail view
   */
  viewPurchaseOrder(po: PurchaseOrderResponse): void {
    this.router.navigate(['/procurement/purchase-orders/view'], { queryParams: { id: po.id } });
  }

  /**
   * Navigate to purchase order edit form
   */
  editPurchaseOrder(po: PurchaseOrderResponse): void {
    this.editClick.emit(po);
    this.router.navigate(['/procurement/purchase-orders/edit'], { queryParams: { id: po.id } });
  }

  /**
   * Handle edit click (alias for editPurchaseOrder)
   */
  onEditClick(po: PurchaseOrderResponse): void {
    this.editClick.emit(po);
    this.router.navigate(['/procurement/purchase-orders/edit'], { queryParams: { id: po.id } });
  }

  /**
   * View details (alias for viewPurchaseOrder)
   */
  viewDetails(po: PurchaseOrderResponse): void {
    this.router.navigate(['/procurement/purchase-orders/view'], { queryParams: { id: po.id } });
  }

  /* ===================== ACTIONS ===================== */

  /**
   * Submit purchase order for approval
   */
  submitPurchaseOrder(po: PurchaseOrderResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to submit PO #${po.poNumber}?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.poService.submitPurchaseOrder(po.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Purchase Order #${po.poNumber} submitted successfully`
            });
            this.poDeleted.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to submit purchase order'
            });
            console.error('Error submitting purchase order:', error);
          }
        });
      }
    });
  }

  /**
   * Confirm purchase order
   */
  confirmPurchaseOrder(po: PurchaseOrderResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to confirm PO #${po.poNumber}?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.poService.confirmPurchaseOrder(po.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Purchase Order #${po.poNumber} confirmed successfully`
            });
            this.poDeleted.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to confirm purchase order'
            });
            console.error('Error confirming purchase order:', error);
          }
        });
      }
    });
  }

  /**
   * Cancel purchase order
   */
  cancelPurchaseOrder(po: PurchaseOrderResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to cancel PO #${po.poNumber}?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.poService.cancelPurchaseOrder(po.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Purchase Order #${po.poNumber} cancelled successfully`
            });
            this.poDeleted.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to cancel purchase order'
            });
            console.error('Error cancelling purchase order:', error);
          }
        });
      }
    });
  }

  /**
   * Delete purchase order
   */
  deletePurchaseOrder(po: PurchaseOrderResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete PO #${po.poNumber}?`,
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deletePurchaseOrderById(po.id);
      }
    });
  }

  /**
   * Handle delete click (alias for deletePurchaseOrder)
   */
  onDeleteClick(po: PurchaseOrderResponse): void {
    this.deletePurchaseOrder(po);
  }

  /**
   * Delete purchase order by ID via API
   */
  private deletePurchaseOrderById(id: string): void {
    this.poService.deletePurchaseOrder(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Purchase Order deleted successfully'
        });
        this.poDeleted.emit();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to delete purchase order'
        });
        console.error('Error deleting purchase order:', error);
      }
    });
  }

  /**
   * Open dialog to apply advance credit to purchase order
   */
  openApplyCreditDialog(po: PurchaseOrderResponse): void {
    this.dialogRef = this.dialogService.open(ApplyAdvanceCreditDialogComponent, {
      header: 'Apply Advance Credit',
      width: '850px',
      data: {
        supplierId: po.supplierId,
        purchaseOrderId: po.id,
        purchaseOrderAmount: po.outstandingAmount
      }
    });

    if (this.dialogRef) {
      this.dialogRef.onClose.subscribe((result) => {
        if (result) {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Advance credit applied successfully. Purchase order updated.'
          });
          this.poDeleted.emit();
        }
      });
    }
  }

  /**
   * Navigate to record payment for purchase order
   */
  recordPayment(po: PurchaseOrderResponse): void {
    this.router.navigate(['/procurement/supplier-payments/new'], {
      queryParams: {
        supplierId: po.supplierId,
        purchaseOrderId: po.id
      }
    });
  }
}
