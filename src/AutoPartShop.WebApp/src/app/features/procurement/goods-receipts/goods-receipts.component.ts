import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';
import { GoodsReceiptsListComponent } from './goods-receipts-list.component';
import { GoodsReceiptService, GoodsReceiptResponse } from '../services/goods-receipt.service';
import { HasRoleDirective } from '../../../shared/directives/has-role.directive';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';

@Component({
  selector: 'app-goods-receipts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToastModule,
    ConfirmDialogModule,
    CardModule,
    Select,
    DatePicker,
    TooltipModule,
    GoodsReceiptsListComponent,
    HasRoleDirective,
    PageContainerComponent,
    PageHeaderComponent,
    FilterBarComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './goods-receipts.component.html',
  styleUrls: ['./goods-receipts.component.css']
})
export class GoodsReceiptsComponent implements OnInit {
  private readonly grnService = inject(GoodsReceiptService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  goodsReceipts: GoodsReceiptResponse[] = [];
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;
  searchTerm = '';
  filterStatus: string | null = null;
  dateRange: Date[] = [];

  statusOptions = [
    { label: 'All', value: null },
    { label: 'Pending', value: 'PENDING' },
    { label: 'Verified', value: 'VERIFIED' },
    { label: 'Accepted', value: 'ACCEPTED' },
    { label: 'Rejected', value: 'REJECTED' }
  ];

  constructor() {}

  ngOnInit(): void {
    this.loadGoodsReceipts();
  }

  /**
   * Load goods receipts from API
   */
  loadGoodsReceipts(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): void {
    if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
      pageNumber = 1;
    }
    if (!pageSize || isNaN(pageSize) || pageSize < 1) {
      pageSize = 10;
    }

    this.loading = true;
    this.grnService.getGoodsReceipts(pageNumber, pageSize, searchTerm).subscribe({
      next: (response) => {
        this.goodsReceipts = response.items;
        this.totalRecords = response.totalCount;
        this.rows = response.pageSize;
        this.currentPage = response.pageNumber;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load goods receipts'
        });
        console.error('Error loading goods receipts:', error);
        this.loading = false;
      }
    });
  }

  /**
   * Handle create button click
   */
  onCreateClick(): void {
    this.router.navigate(['/procurement/goods-receipts/create']);
  }

  /**
   * Refresh data
   */
  refreshData(): void {
    this.loadGoodsReceipts(this.currentPage, this.rows, this.searchTerm);
  }

  /**
   * Handle search
   */
  onSearch(): void {
    this.loadGoodsReceipts(1, this.rows, this.searchTerm);
  }

  /**
   * Handle search clear
   */
  clearSearch(): void {
    this.searchTerm = '';
    this.loadGoodsReceipts(1, this.rows);
  }

  /**
   * Handle filter change
   */
  onFilterChange(): void {
    this.loadGoodsReceipts(1, this.rows, this.searchTerm);
  }

  /**
   * Check if any filter is active
   */
  hasActiveFilters(): boolean {
    return !!(this.searchTerm || this.filterStatus || (this.dateRange && this.dateRange.length > 0));
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.filterStatus = null;
    this.dateRange = [];
    this.loadGoodsReceipts(1, this.rows);
  }

  /**
   * Export receipts to CSV or JSON
   */
  exportReceipts(format: 'csv' | 'json'): void {
    if (this.goodsReceipts.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Data',
        detail: 'No goods receipts to export'
      });
      return;
    }

    if (format === 'csv') {
      this.exportToCSV();
    } else {
      this.exportToJSON();
    }
  }

  /**
   * Export to CSV
   */
  private exportToCSV(): void {
    const headers = ['GRN Number', 'PO Number', 'Warehouse', 'Received Date', 'Status', 'Items', 'Verified By'];
    const rows = this.goodsReceipts.map(grn => [
      grn.grnNumber,
      grn.poNumber || '',
      grn.warehouseName || '',
      grn.receivedDate ? new Date(grn.receivedDate).toLocaleDateString() : '',
      grn.status,
      grn.totalItemsReceived?.toString() || '0',
      grn.verifiedBy || 'Pending'
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `goods-receipts-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);

    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Goods receipts exported to CSV'
    });
  }

  /**
   * Export to JSON
   */
  private exportToJSON(): void {
    const jsonContent = JSON.stringify(this.goodsReceipts, null, 2);
    const blob = new Blob([jsonContent], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `goods-receipts-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    window.URL.revokeObjectURL(url);

    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Goods receipts exported to JSON'
    });
  }

  /**
   * Handle page change
   */
  onPageChange(event: { page: number; rows: number }): void {
    this.loadGoodsReceipts(event.page, event.rows, this.searchTerm);
  }

  /**
   * Handle goods receipt deleted
   */
  onGoodsReceiptDeleted(): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Goods Receipt deleted successfully'
    });
    this.loadGoodsReceipts(this.currentPage, this.rows, this.searchTerm);
  }
}
