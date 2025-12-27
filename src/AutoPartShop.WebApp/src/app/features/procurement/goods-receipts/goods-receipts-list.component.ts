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
import { GoodsReceiptService, GoodsReceiptResponse } from '../services/goods-receipt.service';

@Component({
  selector: 'app-goods-receipts-list',
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
  templateUrl: './goods-receipts-list.component.html',
  styleUrls: ['./goods-receipts-list.component.css']
})
export class GoodsReceiptsListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() goodsReceipts: GoodsReceiptResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() searchTerm = '';

  @Output() viewClick = new EventEmitter<GoodsReceiptResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() grnDeleted = new EventEmitter<void>();
  @Output() onGrnUpdated = new EventEmitter<void>();

  contextMenuItems: MenuItem[] = [];
  selectedGrn: GoodsReceiptResponse | null = null;
  pageSizeOptions = [10, 25, 50, 100];

  private readonly grnService = inject(GoodsReceiptService);
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
        label: 'View',
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedGrn) {
            this.onViewClick(this.selectedGrn);
          }
        }
      },
      { separator: true },
      {
        label: 'Verify',
        icon: 'pi pi-check',
        command: () => {
          if (this.selectedGrn) {
            this.verifyGrn(this.selectedGrn);
          }
        },
        visible: this.selectedGrn ? this.selectedGrn.status === 'PENDING' : false
      },
      {
        label: 'Accept',
        icon: 'pi pi-check-circle',
        command: () => {
          if (this.selectedGrn) {
            this.acceptGrn(this.selectedGrn);
          }
        },
        visible: this.selectedGrn ? this.selectedGrn.status === 'VERIFIED' : false
      },
      {
        label: 'Reject',
        icon: 'pi pi-times-circle',
        command: () => {
          if (this.selectedGrn) {
            this.rejectGrn(this.selectedGrn);
          }
        },
        visible: this.selectedGrn ? ['PENDING', 'VERIFIED'].includes(this.selectedGrn.status) : false
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedGrn) {
            this.onDeleteClick(this.selectedGrn);
          }
        },
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  /**
   * Show context menu
   */
  showContextMenu(event: MouseEvent, grn: GoodsReceiptResponse): void {
    this.selectedGrn = grn;
    this.initializeContextMenu();
    if (this.contextMenu) {
      this.contextMenu.show(event);
    }
  }

  /**
   * View goods receipt
   */
  viewGoodsReceipt(grn: GoodsReceiptResponse): void {
    this.router.navigate(['/procurement/goods-receipts/view'], { queryParams: { id: grn.id } });
  }

  /**
   * Handle view click (legacy for context menu)
   */
  onViewClick(grn: GoodsReceiptResponse): void {
    this.viewGoodsReceipt(grn);
  }

  /**
   * Verify goods receipt
   */
  verifyGrn(grn: GoodsReceiptResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to verify GRN #${grn.grnNumber}?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.grnService.verifyGoodsReceipt(grn.id, 'Warehouse Manager').subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Goods Receipt #${grn.grnNumber} verified successfully`
            });
            this.onGrnUpdated.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to verify goods receipt'
            });
            console.error('Error verifying GRN:', error);
          }
        });
      }
    });
  }

  /**
   * Accept goods receipt
   */
  acceptGrn(grn: GoodsReceiptResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to accept GRN #${grn.grnNumber}?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.grnService.acceptGoodsReceipt(grn.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Goods Receipt #${grn.grnNumber} accepted successfully`
            });
            this.onGrnUpdated.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to accept goods receipt'
            });
            console.error('Error accepting GRN:', error);
          }
        });
      }
    });
  }

  /**
   * Reject goods receipt
   */
  rejectGrn(grn: GoodsReceiptResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to reject GRN #${grn.grnNumber}?`,
      header: 'Confirm Rejection',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.grnService.rejectGoodsReceipt(grn.id, 'Rejected by warehouse').subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Goods Receipt #${grn.grnNumber} rejected`
            });
            this.onGrnUpdated.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to reject goods receipt'
            });
            console.error('Error rejecting GRN:', error);
          }
        });
      }
    });
  }

  /**
   * Delete goods receipt
   */
  deleteGoodsReceipt(grn: GoodsReceiptResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete GRN #${grn.grnNumber}?`,
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteGrnById(grn.id);
      }
    });
  }

  /**
   * Delete goods receipt (legacy for context menu)
   */
  onDeleteClick(grn: GoodsReceiptResponse): void {
    this.deleteGoodsReceipt(grn);
  }

  /**
   * Delete goods receipt via API
   */
  private deleteGrnById(id: string): void {
    this.grnService.deleteGoodsReceipt(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Goods Receipt deleted successfully'
        });
        this.grnDeleted.emit();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to delete goods receipt'
        });
        console.error('Error deleting GRN:', error);
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
  getStatusSeverity(status: string): string {
    switch (status?.toUpperCase()) {
      case 'PENDING':
        return 'warning';
      case 'VERIFIED':
        return 'info';
      case 'ACCEPTED':
        return 'success';
      case 'REJECTED':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  /**
   * Format date
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }
}
