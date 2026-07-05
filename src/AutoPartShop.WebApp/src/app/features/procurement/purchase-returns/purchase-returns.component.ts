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
import { MenuModule } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { PurchaseReturnsListComponent } from './purchase-returns-list/purchase-returns-list.component';
import { PurchaseReturnService, PurchaseReturnResponse } from '../services/purchase-return.service';
import { HasRoleDirective } from '../../../shared/directives/has-role.directive';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
  selector: 'app-purchase-returns',
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
    MenuModule,
    TooltipModule,
    PurchaseReturnsListComponent,
    HasRoleDirective,
    PageContainerComponent,
    PageHeaderComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './purchase-returns.component.html',
  styleUrls: ['./purchase-returns.component.css']
})
export class PurchaseReturnsComponent implements OnInit {
  private readonly prService = inject(PurchaseReturnService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);

  purchaseReturns: PurchaseReturnResponse[] = [];
  displayCreateDialog = false;
  selectedPurchaseReturn: PurchaseReturnResponse | null = null;
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;
  searchTerm = '';
  filterStatus: string | null = null;
  dateRange: Date[] | null = null;

  statusOptions = [
    { label: 'All', value: null },
    { label: 'Draft', value: 'DRAFT' },
    { label: 'Pending', value: 'PENDING' },
    { label: 'Approved', value: 'APPROVED' },
    { label: 'Rejected', value: 'REJECTED' },
    { label: 'Completed', value: 'COMPLETED' }
  ];

  menuItems: MenuItem[] = [
    {
      label: 'Export CSV',
      icon: 'pi pi-file',
      command: () => this.exportReturns('csv')
    },
    {
      label: 'Export JSON',
      icon: 'pi pi-file-export',
      command: () => this.exportReturns('json')
    },
    { separator: true },
    {
      label: 'Clear Filters',
      icon: 'pi pi-filter-slash',
      command: () => this.clearFilters()
    }
  ];

  constructor() {}

  ngOnInit(): void {
    this.loadPurchaseReturns();
  }

  /**
   * Load purchase returns from API
   */
  loadPurchaseReturns(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): void {
    if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
      pageNumber = 1;
    }
    if (!pageSize || isNaN(pageSize) || pageSize < 1) {
      pageSize = 10;
    }

    this.loading = true;
    this.prService.getPurchaseReturns(pageNumber, pageSize, searchTerm).subscribe({
      next: (response) => {
        console.log('Purchase Returns API Response:', response);
        console.log('Purchase Returns Items:', response.items);
        if (response.items && response.items.length > 0) {
          console.log('First Item Sample:', response.items[0]);
          console.log('First Item Supplier Name:', response.items[0].supplierName);
          console.log('First Item Supplier Code:', response.items[0].supplierCode);
          console.log('First Item Refund Amount:', response.items[0].refundAmount);
        }

        this.purchaseReturns = response.items;
        this.totalRecords = response.totalCount;
        this.rows = response.pageSize;
        this.currentPage = response.pageNumber;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load purchase returns'
        });
        console.error('Error loading purchase returns:', error);
        this.loading = false;
      }
    });
  }

  /**
   * Handle create button click
   */
  onCreateClick(): void {
    this.router.navigate(['/procurement/purchase-returns/create']);
  }

  /**
   * Handle search
   */
  onSearch(): void {
    this.loadPurchaseReturns(1, this.rows, this.searchTerm);
  }

  /**
   * Handle search clear
   */
  clearSearch(): void {
    this.searchTerm = '';
    this.loadPurchaseReturns(1, this.rows);
  }

  /**
   * Check if any filter is active
   */
  hasActiveFilters(): boolean {
    return !!(this.searchTerm || this.filterStatus || (this.dateRange && this.dateRange.length > 0));
  }

  /**
   * Handle filter change
   */
  onFilterChange(): void {
    // TODO: Implement filtering logic with status and date range
    this.loadPurchaseReturns(1, this.rows, this.searchTerm);
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.filterStatus = null;
    this.dateRange = null;
    this.loadPurchaseReturns(1, this.rows);
  }

  /**
   * Export returns to CSV or JSON
   */
  exportReturns(format: 'csv' | 'json'): void {
    if (this.purchaseReturns.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Data',
        detail: 'No purchase returns to export'
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
    const headers = ['Return Number', 'Supplier', 'Return Date', 'Status', 'Refund Amount'];
    const rows = this.purchaseReturns.map(pr => [
      pr.returnNumber,
      pr.supplierName || '',
      pr.returnDate ? new Date(pr.returnDate).toLocaleDateString() : '',
      pr.status,
      pr.refundAmount.toString()
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `purchase-returns-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);

    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Purchase returns exported to CSV'
    });
  }

  /**
   * Export to JSON
   */
  private exportToJSON(): void {
    const jsonContent = JSON.stringify(this.purchaseReturns, null, 2);
    const blob = new Blob([jsonContent], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `purchase-returns-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    window.URL.revokeObjectURL(url);

    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Purchase returns exported to JSON'
    });
  }

  /**
   * Handle page change
   */
  onPageChange(event: { page: number; rows: number }): void {
    this.loadPurchaseReturns(event.page + 1, event.rows, this.searchTerm);
  }

  /**
   * Handle purchase return created
   */
  onPurchaseReturnCreated(): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Purchase Return created successfully'
    });
    this.loadPurchaseReturns(1, this.rows, this.searchTerm);
  }

  /**
   * Handle purchase return deleted
   */
  onPurchaseReturnDeleted(): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Purchase Return deleted successfully'
    });
    this.loadPurchaseReturns(this.currentPage, this.rows, this.searchTerm);
  }
}
