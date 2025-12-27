import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { PaginatorModule } from 'primeng/paginator';
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';

@Component({
  selector: 'app-purchase-orders-list',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ButtonModule,
    ConfirmDialogModule,
    ContextMenuModule,
    RippleModule,
    TagModule,
    TooltipModule,
    PaginatorModule
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './purchase-orders-list.component.html',
  styleUrls: ['./purchase-orders-list.component.css']
})
export class PurchaseOrdersListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() purchaseOrders: PurchaseOrderResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() searchTerm = '';

  @Output() editClick = new EventEmitter<PurchaseOrderResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() poDeleted = new EventEmitter<void>();

  contextMenuItems: MenuItem[] = [];
  selectedPurchaseOrder: PurchaseOrderResponse | null = null;
  pageSizeOptions = [10, 25, 50, 100];

  private readonly poService = inject(PurchaseOrderService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

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
          if (this.selectedPurchaseOrder) {
            this.onEditClick(this.selectedPurchaseOrder);
          }
        }
      },
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.viewDetails(this.selectedPurchaseOrder);
          }
        }
      },
      { separator: true },
      {
        label: 'Submit',
        icon: 'pi pi-send',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.submitPurchaseOrder(this.selectedPurchaseOrder);
          }
        },
        visible: this.selectedPurchaseOrder ? this.selectedPurchaseOrder.status === 'DRAFT' : false
      },
      {
        label: 'Confirm',
        icon: 'pi pi-check',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.confirmPurchaseOrder(this.selectedPurchaseOrder);
          }
        },
        visible: this.selectedPurchaseOrder ? this.selectedPurchaseOrder.status === 'SUBMITTED' : false
      },
      { separator: true },
      {
        label: 'Cancel',
        icon: 'pi pi-ban',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.cancelPurchaseOrder(this.selectedPurchaseOrder);
          }
        },
        visible: this.selectedPurchaseOrder ? ['DRAFT', 'SUBMITTED'].includes(this.selectedPurchaseOrder.status) : false
      },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedPurchaseOrder) {
            this.onDeleteClick(this.selectedPurchaseOrder);
          }
        },
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  /**
   * Show context menu
   */
  showContextMenu(event: MouseEvent, po: PurchaseOrderResponse): void {
    this.selectedPurchaseOrder = po;
    this.initializeContextMenu();
    if (this.contextMenu) {
      this.contextMenu.show(event);
    }
  }

  /**
   * View purchase order details
   */
  viewPurchaseOrder(po: PurchaseOrderResponse): void {
    this.router.navigate(['/procurement/purchase-orders/view'], { queryParams: { id: po.id } });
  }

  /**
   * Edit purchase order
   */
  editPurchaseOrder(po: PurchaseOrderResponse): void {
    this.editClick.emit(po);
    this.router.navigate(['/procurement/purchase-orders/edit'], { queryParams: { id: po.id } });
  }

  /**
   * Handle edit click
   */
  onEditClick(po: PurchaseOrderResponse): void {
    this.editClick.emit(po);
    this.router.navigate(['/procurement/purchase-orders/edit'], { queryParams: { id: po.id } });
  }

  /**
   * View details
   */
  viewDetails(po: PurchaseOrderResponse): void {
    this.messageService.add({
      severity: 'info',
      summary: 'Info',
      detail: `PO #${po.poNumber} - Status: ${po.status}`
    });
  }

  /**
   * Submit purchase order
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
   * Delete purchase order (legacy)
   */
  onDeleteClick(po: PurchaseOrderResponse): void {
    this.deletePurchaseOrder(po);
  }

  /**
   * Delete purchase order via API
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
   * Handle pagination change
   */
  onPageChange(event: any): void {
    // Validate event data
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
      return;
    }
    const pageNumber = Math.floor(event.first / event.rows) + 1;
    this.pageChange.emit({
      page: pageNumber,  // Convert first index to 1-based page number
      rows: event.rows
    });
  }

  /**
   * Get status badge severity
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

  /**
   * Format currency
   */
  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR'
    }).format(value);
  }

  /**
   * Format date
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }
}
