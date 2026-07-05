import { Component, OnInit, inject, ViewChild, DestroyRef } from '@angular/core';
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
import { I18nService } from '@/shared/services/i18n.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

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
    InputGroupAddonModule,
    PageContainerComponent,
    PageHeaderComponent
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
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  salesReturns: SalesReturnResponse[] = [];
  loading = false;
  totalRecords = 0;
  pageNumber = 1;
  pageSize = 10;
  first = 0;
  pageSizeOptions = [10, 20, 50];
  pageSizeSelectOptions = this.pageSizeOptions.map((size) => ({ label: size.toString(), value: size }));
  Math = Math;

  searchTerm = '';
  filterStatus = '';
  dateRange: Date[] = [];

  actionMenuItems: MenuItem[] = [];
  selectedReturn: SalesReturnResponse | null = null;

  statusOptions: { label: string; value: string }[] = [];

  ngOnInit(): void {
    this.buildStatusOptions();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.buildStatusOptions();
    });
    this.loadSalesReturns();
  }

  private buildStatusOptions(): void {
    this.statusOptions = [
      { label: this.i18n.t('salesReturns.statusOptions.allStatuses'), value: '' },
      { label: this.i18n.t('salesReturns.statusOptions.pending'),    value: 'PENDING' },
      { label: this.i18n.t('salesReturns.statusOptions.approved'),   value: 'APPROVED' },
      { label: this.i18n.t('salesReturns.statusOptions.received'),   value: 'RECEIVED' },
      { label: this.i18n.t('salesReturns.statusOptions.rejected'),   value: 'REJECTED' },
      { label: this.i18n.t('salesReturns.statusOptions.processed'),  value: 'PROCESSED' }
    ];
  }

  private buildActionMenuItems(salesReturn: SalesReturnResponse): MenuItem[] {
    return [
      {
        label: this.i18n.t('common.actions.view'),
        icon: 'pi pi-eye',
        command: () => this.viewReturn(salesReturn)
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.approve'),
        icon: 'pi pi-check',
        command: () => this.approveReturn(salesReturn, new Event('click')),
        visible: salesReturn.status === 'PENDING'
      },
      {
        label: this.i18n.t('common.actions.reject'),
        icon: 'pi pi-times',
        command: () => this.rejectReturn(salesReturn, new Event('click')),
        visible: salesReturn.status === 'PENDING'
      },
      {
        label: this.i18n.t('common.actions.receive'),
        icon: 'pi pi-inbox',
        command: () => this.receiveReturn(salesReturn, new Event('click')),
        visible: salesReturn.status === 'APPROVED'
      },
      {
        label: this.i18n.t('common.actions.process'),
        icon: 'pi pi-cog',
        command: () => this.processReturn(salesReturn, new Event('click')),
        visible: salesReturn.status === 'RECEIVED'
      }
    ];
  }

  showActionMenu(event: Event, salesReturn: SalesReturnResponse): void {
    this.selectedReturn = salesReturn;
    this.actionMenuItems = this.buildActionMenuItems(salesReturn);
    this.actionMenu.toggle(event);
  }

  loadSalesReturns(): void {
    this.loading = true;

    this.salesReturnService.getSalesReturns(this.pageNumber, this.pageSize, this.searchTerm || undefined).subscribe({
      next: (response) => {
        let filteredData = response.data;
        if (this.filterStatus) {
          filteredData = filteredData.filter(ret => ret.status === this.filterStatus);
        }
        this.salesReturns = filteredData;
        this.totalRecords = response.pagination.totalCount;
        this.loading = false;
      },
      error: (err) => {
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
              summary: this.i18n.t('common.messages.error'),
              detail: this.i18n.t('salesReturns.messages.loadFailed')
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
        summary: this.i18n.t('common.messages.warning'),
        detail: this.i18n.t('salesReturns.messages.exportNoData')
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
      summary: this.i18n.t('common.messages.success'),
      detail: this.i18n.t('salesReturns.messages.exportCSVSuccess')
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
      summary: this.i18n.t('common.messages.success'),
      detail: this.i18n.t('salesReturns.messages.exportJSONSuccess')
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
      message: this.i18n.t('salesReturns.messages.approveConfirm', { number: salesReturn.returnNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.approveSalesReturn(salesReturn.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('salesReturns.messages.approveSuccess')
            });
            this.loadSalesReturns();
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || this.i18n.t('salesReturns.messages.approveFailed'))
            });
          }
        });
      }
    });
  }

  rejectReturn(salesReturn: SalesReturnResponse, event: Event): void {
    event.stopPropagation();

    const reason = prompt(this.i18n.t('salesReturns.messages.rejectConfirm', { number: salesReturn.returnNumber }));
    if (reason === null) return;

    this.salesReturnService.rejectSalesReturn(salesReturn.id, reason).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('salesReturns.messages.rejectSuccess')
        });
        this.loadSalesReturns();
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || this.i18n.t('salesReturns.messages.rejectFailed'))
        });
      }
    });
  }

  receiveReturn(salesReturn: SalesReturnResponse, event: Event): void {
    event.stopPropagation();

    this.confirmationService.confirm({
      message: this.i18n.t('salesReturns.messages.receiveConfirm', { number: salesReturn.returnNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.receiveSalesReturn(salesReturn.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('salesReturns.messages.receiveSuccess')
            });
            this.loadSalesReturns();
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || this.i18n.t('salesReturns.messages.receiveFailed'))
            });
          }
        });
      }
    });
  }

  processReturn(salesReturn: SalesReturnResponse, event: Event): void {
    event.stopPropagation();

    this.confirmationService.confirm({
      message: this.i18n.t('salesReturns.messages.processConfirm', { number: salesReturn.returnNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.processSalesReturn(salesReturn.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('salesReturns.messages.processSuccess')
            });
            this.loadSalesReturns();
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || this.i18n.t('salesReturns.messages.processFailed'))
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
