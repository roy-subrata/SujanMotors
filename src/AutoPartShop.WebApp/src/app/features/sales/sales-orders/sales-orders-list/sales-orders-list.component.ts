import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SalesOrderService, SalesOrderResponse } from '../../services/sales-order.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';


@Component({
  selector: 'app-sales-orders-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    Select,
    DatePickerModule,
    CardModule,
    TagModule,
    TooltipModule,
    ToastModule,
    ConfirmDialogModule,
    PaginatorModule,
    ContextMenuModule,
    RippleModule
  ],
  providers: [MessageService, ConfirmationService, DialogService],
  templateUrl: './sales-orders-list.component.html',
  styleUrls: ['./sales-orders-list.component.css']
})
export class SalesOrdersListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  private readonly salesOrderService = inject(SalesOrderService);
  private readonly currencyService = inject(CurrencyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly dialogService = inject(DialogService);

  salesOrders: SalesOrderResponse[] = [];
  loading = false;
  totalRecords = 0;
  pageNumber = 1;
  pageSize = 25;
  pageSizeOptions = [10, 25, 50, 100];

  contextMenuItems: MenuItem[] = [];
  selectedSalesOrder: SalesOrderResponse | null = null;
  private dialogRef: DynamicDialogRef | undefined;

  // Filters
  searchTerm = '';
  filterStatus = '';
  dateRange: Date[] = [];

  statusOptions = [
    { label: 'All Statuses', value: '' },
    { label: 'Draft', value: 'DRAFT' },
    { label: 'Confirmed', value: 'CONFIRMED' },
    { label: 'Partially Shipped', value: 'PARTIALLY_SHIPPED' },
    { label: 'Shipped', value: 'SHIPPED' },
    { label: 'Delivered', value: 'DELIVERED' },
    { label: 'Cancelled', value: 'CANCELLED' }
  ];

  ngOnInit(): void {
    // Check for query parameters
    this.route.queryParams.subscribe(params => {
      if (params['status']) {
        this.filterStatus = params['status'];
      }
    });

    this.loadSalesOrders();
    this.initializeContextMenu();
  }

  loadSalesOrders(): void {
    this.loading = true;

    const fromDate = this.dateRange && this.dateRange.length > 0 ? this.formatDateForApi(this.dateRange[0]) : undefined;
    const toDate = this.dateRange && this.dateRange.length > 1 ? this.formatDateForApi(this.dateRange[1]) : undefined;

    const filter: any = {
      searchTerm: this.searchTerm,
      status: this.filterStatus,
      fromDate: fromDate,
      toDate: toDate
    };

    this.salesOrderService.getSalesOrders(this.pageNumber, this.pageSize, filter.searchTerm || undefined).subscribe({
      next: (response) => {
        // Apply filters if needed
        let filteredData = response.data;

        if (this.filterStatus) {
          filteredData = filteredData.filter(order => order.status === this.filterStatus);
        }

        this.salesOrders = filteredData;
        this.totalRecords = response.pagination.totalCount;
        this.loading = false;
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load sales orders'
        });
        console.error('Error loading sales orders:', err);
        this.loading = false;
      }
    });
  }

  onSearch(): void {
    this.pageNumber = 1;
    this.loadSalesOrders();
  }

  onFilterChange(): void {
    this.pageNumber = 1;
    this.loadSalesOrders();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.pageNumber = 1;
    this.loadSalesOrders();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.filterStatus = '';
    this.dateRange = [];
    this.pageNumber = 1;
    this.loadSalesOrders();
  }

  onPageChange(event: PaginatorState): void {
    this.pageNumber = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? this.pageSize;
    this.loadSalesOrders();
  }

  exportOrders(format: 'csv' | 'json'): void {
    const dataToExport = this.salesOrders;

    if (dataToExport.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Data',
        detail: 'No sales orders available to export'
      });
      return;
    }

    if (format === 'csv') {
      this.exportToCSV(dataToExport);
    } else {
      this.exportToJSON(dataToExport);
    }
  }

  private exportToCSV(data: SalesOrderResponse[]): void {
    const headers = ['SO Number', 'Customer', 'Email', 'Phone', 'Order Date', 'Delivery Date', 'Status', 'Grand Total', 'Outstanding'];
    const csvData = data.map(order => [
      order.soNumber,
      order.customerName || '',
      order.customerEmail || '',
      order.customerPhone || '',
      this.formatDate(order.orderDate),
      this.formatDate(order.deliveryDate),
      order.status,
      order.grandTotal.toString(),
      order.outstandingAmount.toString()
    ]);

    const csvContent = [
      headers.join(','),
      ...csvData.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `sales_orders_${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
    window.URL.revokeObjectURL(url);

    this.messageService.add({
      severity: 'success',
      summary: 'Export Complete',
      detail: 'Sales orders exported as CSV'
    });
  }

  private exportToJSON(data: SalesOrderResponse[]): void {
    const jsonContent = JSON.stringify(data, null, 2);
    const blob = new Blob([jsonContent], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `sales_orders_${new Date().toISOString().split('T')[0]}.json`;
    link.click();
    window.URL.revokeObjectURL(url);

    this.messageService.add({
      severity: 'success',
      summary: 'Export Complete',
      detail: 'Sales orders exported as JSON'
    });
  }

  private formatDateForApi(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  createSalesOrder(): void {
    this.router.navigate(['/sales/sales-orders/create']);
  }

  viewSalesOrder(salesOrder: SalesOrderResponse): void {
    this.router.navigate(['/sales/sales-orders/view'], {
      queryParams: { id: salesOrder.id }
    });
  }

  editSalesOrder(salesOrder: SalesOrderResponse): void {
    this.router.navigate(['/sales/sales-orders/edit'], {
      queryParams: { id: salesOrder.id }
    });
  }

  confirmSalesOrder(salesOrder: SalesOrderResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to confirm Sales Order ${salesOrder.soNumber}?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesOrderService.confirmSalesOrder(salesOrder.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Sales order confirmed successfully'
            });
            this.loadSalesOrders();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to confirm sales order'
            });
          }
        });
      }
    });
  }

  deleteSalesOrder(salesOrder: SalesOrderResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete Sales Order ${salesOrder.soNumber}?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesOrderService.deleteSalesOrder(salesOrder.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Sales order deleted successfully'
            });
            this.loadSalesOrders();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete sales order'
            });
          }
        });
      }
    });
  }

  getStatusSeverity(status: string): string {
    const severityMap: Record<string, string> = {
      DRAFT: 'secondary',
      CONFIRMED: 'info',
      PARTIALLY_SHIPPED: 'warning',
      SHIPPED: 'primary',
      DELIVERED: 'success',
      CANCELLED: 'danger'
    };
    return severityMap[status] || 'secondary';
  }

  formatCurrency(amount: number): string {
    const currency = this.currencyService.selectedCurrency() || 'BDT';
    return this.currencyService.formatCurrency(amount, currency);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }

  /**
   * Initialize context menu items
   */
  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedSalesOrder) {
            this.viewSalesOrder(this.selectedSalesOrder);
          }
        }
      },
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedSalesOrder) {
            this.editSalesOrder(this.selectedSalesOrder);
          }
        },
        visible: this.selectedSalesOrder ? this.selectedSalesOrder.status === 'DRAFT' : false
      },
      {
        label: 'Confirm',
        icon: 'pi pi-check-circle',
        command: () => {
          if (this.selectedSalesOrder) {
            this.confirmSalesOrder(this.selectedSalesOrder);
          }
        },
        visible: this.selectedSalesOrder ? this.selectedSalesOrder.status === 'DRAFT' : false
      },
      {
        label: 'Record Payment',
        icon: 'pi pi-wallet',
        command: () => {
          if (this.selectedSalesOrder) {
            this.recordPayment(this.selectedSalesOrder);
          }
        },
        visible: this.selectedSalesOrder ?
          (['CONFIRMED', 'PARTIALLY_SHIPPED', 'SHIPPED', 'DELIVERED'].includes(this.selectedSalesOrder.status) &&
           this.selectedSalesOrder.outstandingAmount > 0) : false
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedSalesOrder) {
            this.deleteSalesOrder(this.selectedSalesOrder);
          }
        },
        visible: this.selectedSalesOrder ? this.selectedSalesOrder.status === 'DRAFT' : false,
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  /**
   * Show context menu
   */
  showContextMenu(event: MouseEvent, salesOrder: SalesOrderResponse): void {
    event.preventDefault();
    event.stopPropagation();
    this.selectedSalesOrder = salesOrder;
    this.initializeContextMenu();
    this.contextMenu?.show(event);
  }

  /**
   * Navigate to record payment for sales order
   */
  recordPayment(salesOrder: SalesOrderResponse): void {
    this.router.navigate(['/sales/customer-payments/new'], {
      queryParams: {
        customerId: salesOrder.customerId,
        salesOrderId: salesOrder.id
      }
    });
  }
}
