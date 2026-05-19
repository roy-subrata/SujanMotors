import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { Select } from 'primeng/select';
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { GoodsReceiptService, GoodsReceiptResponse } from '../services/goods-receipt.service';

@Component({
  selector: 'app-goods-receipts-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    MenuModule,
    TooltipModule,
    Select
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './goods-receipts-list.component.html',
  styleUrls: ['./goods-receipts-list.component.css']
})
export class GoodsReceiptsListComponent implements OnInit {
  @ViewChild('actionMenu') actionMenu!: Menu;

  @Input() goodsReceipts: GoodsReceiptResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() searchTerm = '';
  @Input() hasActiveFilters = false;

  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() grnDeleted = new EventEmitter<void>();

  actionMenuItems: MenuItem[] = [];
  selectedGrn: GoodsReceiptResponse | null = null;

  pageSizeOptions = [
    { label: '10', value: 10 },
    { label: '20', value: 20 },
    { label: '50', value: 50 }
  ];

  private readonly grnService = inject(GoodsReceiptService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  ngOnInit(): void {}

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalRecords / this.rows));
  }

  get firstIndex(): number {
    return Math.max(0, (this.currentPage - 1) * this.rows);
  }

  /**
   * Show action menu
   */
  showActionMenu(event: Event, grn: GoodsReceiptResponse): void {
    this.selectedGrn = grn;

    this.actionMenuItems = [
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => this.viewGoodsReceipt(grn)
      },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => this.deleteGoodsReceipt(grn),
        visible: grn.status === 'PENDING',
        styleClass: 'text-red-600'
      }
    ];

    this.actionMenu.toggle(event);
  }

  /**
   * View goods receipt
   */
  viewGoodsReceipt(grn: GoodsReceiptResponse): void {
    this.router.navigate(['/procurement/goods-receipts/view'], { queryParams: { id: grn.id } });
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
   * Handle page size change
   */
  onPageSizeChange(): void {
    this.pageChange.emit({
      page: 1,
      rows: this.rows
    });
  }

  /**
   * Pagination helpers
   */
  goToFirstPage(): void {
    if (this.currentPage === 1) return;
    this.pageChange.emit({ page: 1, rows: this.rows });
  }

  goToPrevPage(): void {
    if (this.currentPage <= 1) return;
    this.pageChange.emit({ page: this.currentPage - 1, rows: this.rows });
  }

  goToNextPage(): void {
    if (this.currentPage >= this.totalPages) return;
    this.pageChange.emit({ page: this.currentPage + 1, rows: this.rows });
  }

  goToLastPage(): void {
    if (this.currentPage >= this.totalPages) return;
    this.pageChange.emit({ page: this.totalPages, rows: this.rows });
  }

  /**
   * Format date
   */
  formatDate(date: string): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  /**
   * Format status for display
   */
  formatStatus(status: string): string {
    if (!status) return '-';
    return status
      .split('_')
      .map((word) => word.charAt(0) + word.slice(1).toLowerCase())
      .join(' ');
  }
}
