import { Component, OnInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SalesReturnService, SalesReturnResponse } from '../../services/sales-return.service';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { PanelModule } from 'primeng/panel';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { MenuModule, Menu } from 'primeng/menu';
import { TagModule } from 'primeng/tag';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { CurrencyService } from '../../../../shared/services/currency.service';

@Component({
  selector: 'app-sales-returns-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    Select,
    DatePickerModule,
    PanelModule,
    CardModule,
    TooltipModule,
    ToastModule,
    ConfirmDialogModule,
    PaginatorModule,
    MenuModule,
    TagModule,
    InputGroupModule,
    InputGroupAddonModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './sales-returns-list.component.html',
  styleUrls: ['./sales-returns-list.component.css']
})
export class SalesReturnsListComponent implements OnInit {
  @ViewChild('actionMenu') actionMenu!: Menu;

  private readonly salesReturnService = inject(SalesReturnService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly currencyService = inject(CurrencyService);

  salesReturns: SalesReturnResponse[] = [];
  loading = false;
  totalRecords = 0;
  pageNumber = 1;
  pageSize = 10;
  first = 0;
  pageSizeOptions = [10, 20, 50];
  pageSizeSelectOptions = this.pageSizeOptions.map((size) => ({ label: size.toString(), value: size }));
  Math = Math;

  // Filters
  searchTerm = '';
  filterStatus = '';
  dateRange: Date[] = [];

  // Action menu
  actionMenuItems: MenuItem[] = [];
  selectedReturn: SalesReturnResponse | null = null;

  statusOptions = [
    { label: 'All Statuses', value: '' },
    { label: 'Pending', value: 'PENDING' },
    { label: 'Approved', value: 'APPROVED' },
    { label: 'Received', value: 'RECEIVED' },
    { label: 'Rejected', value: 'REJECTED' },
    { label: 'Processed', value: 'PROCESSED' }
  ];

  ngOnInit(): void {
    this.loadSalesReturns();
  }

  /**
   * Show action menu
   */
  showActionMenu(event: Event, salesReturn: SalesReturnResponse): void {
    this.selectedReturn = salesReturn;
    this.actionMenuItems = [
      {
        label: 'View',
        icon: 'pi pi-eye',
        command: () => this.viewReturn(salesReturn)
      },
      { separator: true },
      {
        label: 'Approve',
        icon: 'pi pi-check',
        command: () => this.approveReturn(salesReturn, new Event('click')),
        visible: salesReturn.status === 'PENDING'
      },
      {
        label: 'Reject',
        icon: 'pi pi-times',
        command: () => this.rejectReturn(salesReturn, new Event('click')),
        visible: salesReturn.status === 'PENDING'
      },
      {
        label: 'Receive',
        icon: 'pi pi-inbox',
        command: () => this.receiveReturn(salesReturn, new Event('click')),
        visible: salesReturn.status === 'APPROVED'
      },
      {
        label: 'Process',
        icon: 'pi pi-cog',
        command: () => this.processReturn(salesReturn, new Event('click')),
        visible: salesReturn.status === 'RECEIVED'
      }
    ];

    this.actionMenu.toggle(event);
  }

  loadSalesReturns(): void {
    this.loading = true;

    this.salesReturnService.getSalesReturns(this.pageNumber, this.pageSize, this.searchTerm || undefined).subscribe({
      next: (response) => {
        // Apply filters if needed
        let filteredData = response.data;

        if (this.filterStatus) {
          filteredData = filteredData.filter(ret => ret.status === this.filterStatus);
        }

        this.salesReturns = filteredData;
        this.totalRecords = response.pagination.totalCount;
        this.loading = false;
      },
      error: (err) => {
        // If list endpoint doesn't exist, try to get all returns
        this.salesReturnService.getAllSalesReturns().subscribe({
          next: (returns) => {
            let filteredData = returns;

            if (this.searchTerm) {
              filteredData = filteredData.filter(r =>
                r.returnNumber?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
                r.salesOrderId?.toLowerCase().includes(this.searchTerm.toLowerCase())
              );
            }

            if (this.filterStatus) {
              filteredData = filteredData.filter(r => r.status === this.filterStatus);
            }

            this.salesReturns = filteredData;
            this.totalRecords = filteredData.length;
            this.loading = false;
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to load sales returns'
            });
            console.error('Error loading sales returns:', err);
            this.loading = false;
          }
        });
      }
    });
  }

  onSearch(): void {
    this.resetPagination();
    this.loadSalesReturns();
  }

  onFilterChange(): void {
    this.resetPagination();
    this.loadSalesReturns();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.resetPagination();
    this.loadSalesReturns();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.filterStatus = '';
    this.dateRange = [];
    this.resetPagination();
    this.loadSalesReturns();
  }

  hasActiveFilters(): boolean {
    return !!(this.searchTerm || this.filterStatus || (this.dateRange && this.dateRange.length > 0));
  }

  onPageChange(event: PaginatorState): void {
    this.first = event.first ?? 0;
    this.pageSize = event.rows ?? 10;
    this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
    this.loadSalesReturns();
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.pageSize = event.rows ?? 10;
    this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
    this.loadSalesReturns();
  }

  refreshData(): void {
    this.loadSalesReturns();
  }

  private resetPagination(): void {
    this.pageNumber = 1;
    this.first = 0;
  }

  exportReturns(format: 'csv' | 'json'): void {
    const dataToExport = this.salesReturns;

    if (dataToExport.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Data',
        detail: 'No sales returns available to export'
      });
      return;
    }

    if (format === 'csv') {
      this.exportToCSV(dataToExport);
    } else {
      this.exportToJSON(dataToExport);
    }
  }

  private exportToCSV(data: SalesReturnResponse[]): void {
    const headers = ['Return #', 'Sales Order', 'Date', 'Reason', 'Refund Amount', 'Status'];
    const csvData = data.map(ret => [
      ret.returnNumber,
      ret.salesOrderId,
      this.formatDate(ret.createdAt),
      ret.reason,
      ret.totalRefundAmount.toString(),
      ret.status
    ]);

    const csvContent = [
      headers.join(','),
      ...csvData.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `sales_returns_${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
    window.URL.revokeObjectURL(url);

    this.messageService.add({
      severity: 'success',
      summary: 'Export Complete',
      detail: 'Sales returns exported as CSV'
    });
  }

  private exportToJSON(data: SalesReturnResponse[]): void {
    const jsonContent = JSON.stringify(data, null, 2);
    const blob = new Blob([jsonContent], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `sales_returns_${new Date().toISOString().split('T')[0]}.json`;
    link.click();
    window.URL.revokeObjectURL(url);

    this.messageService.add({
      severity: 'success',
      summary: 'Export Complete',
      detail: 'Sales returns exported as JSON'
    });
  }

  createReturn(): void {
    this.router.navigate(['/sales/sales-returns/create']);
  }

  viewReturn(salesReturn: SalesReturnResponse): void {
    this.router.navigate(['/sales/sales-returns/view'], { queryParams: { id: salesReturn.id } });
  }

  approveReturn(salesReturn: SalesReturnResponse, event: Event): void {
    event.stopPropagation();

    this.confirmationService.confirm({
      message: `Are you sure you want to approve return ${salesReturn.returnNumber}?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.approveSalesReturn(salesReturn.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Return approved successfully'
            });
            this.loadSalesReturns();
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: err?.error?.message || 'Failed to approve return'
            });
          }
        });
      }
    });
  }

  rejectReturn(salesReturn: SalesReturnResponse, event: Event): void {
    event.stopPropagation();

    // TODO: Add dialog to get rejection reason
    const reason = prompt(`Enter reason for rejecting return ${salesReturn.returnNumber}:`);
    if (reason === null) return;

    this.salesReturnService.rejectSalesReturn(salesReturn.id, reason).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Return rejected successfully'
        });
        this.loadSalesReturns();
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: err?.error?.message || 'Failed to reject return'
        });
      }
    });
  }

  receiveReturn(salesReturn: SalesReturnResponse, event: Event): void {
    event.stopPropagation();

    this.confirmationService.confirm({
      message: `Mark return ${salesReturn.returnNumber} as received?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.receiveSalesReturn(salesReturn.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Return marked as received'
            });
            this.loadSalesReturns();
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: err?.error?.message || 'Failed to mark return as received'
            });
          }
        });
      }
    });
  }

  processReturn(salesReturn: SalesReturnResponse, event: Event): void {
    event.stopPropagation();

    this.confirmationService.confirm({
      message: `Process return ${salesReturn.returnNumber}? This will complete the return.`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.processSalesReturn(salesReturn.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Return processed successfully'
            });
            this.loadSalesReturns();
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: err?.error?.message || 'Failed to process return'
            });
          }
        });
      }
    });
  }

  getStatusSeverity(status: string): string {
    const severityMap: Record<string, string> = {
      PENDING: 'warn',
      APPROVED: 'info',
      RECEIVED: 'primary',
      REJECTED: 'danger',
      PROCESSED: 'success'
    };
    return severityMap[status] || 'secondary';
  }

  formatCurrency(amount: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(amount, currency);
  }

  formatDate(date: string): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  formatStatus(status: string): string {
    if (!status) return '-';
    return status
      .split('_')
      .map((word) => word.charAt(0) + word.slice(1).toLowerCase())
      .join(' ');
  }
}
