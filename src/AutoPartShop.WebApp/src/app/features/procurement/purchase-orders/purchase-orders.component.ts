import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { MenuModule } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { PurchaseOrdersListComponent } from './purchase-orders-list/purchase-orders-list.component';
import { PurchaseOrderService, PurchaseOrderResponse } from '../services/purchase-order.service';
import { PurchaseOrdersFormDialogComponent } from './purchase-orders-form-dialog/purchase-orders-form-dialog.component';
import { HasRoleDirective } from '../../../shared/directives/has-role.directive';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
    selector: 'app-purchase-orders',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, ToastModule, ConfirmDialogModule, Select, DatePicker, MenuModule, TooltipModule, PurchaseOrdersListComponent, PurchaseOrdersFormDialogComponent, HasRoleDirective, PageContainerComponent, PageHeaderComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './purchase-orders.component.html',
    styleUrls: ['./purchase-orders.component.css']
})
export class PurchaseOrdersComponent implements OnInit {
    private readonly poService = inject(PurchaseOrderService);
    private readonly messageService = inject(MessageService);
    private readonly router = inject(Router);

    purchaseOrders: PurchaseOrderResponse[] = [];
    displayCreateDialog = false;
    displayUpdateDialog = false;
    selectedPurchaseOrder: PurchaseOrderResponse | null = null;
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
        { label: 'Submitted', value: 'SUBMITTED' },
        { label: 'Confirmed', value: 'CONFIRMED' },
        { label: 'Cancelled', value: 'CANCELLED' },
        { label: 'Completed', value: 'COMPLETED' }
    ];

    menuItems: MenuItem[] = [
        {
            label: 'Export CSV',
            icon: 'pi pi-file',
            command: () => this.exportOrders('csv')
        },
        {
            label: 'Export JSON',
            icon: 'pi pi-file-export',
            command: () => this.exportOrders('json')
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
        this.loadPurchaseOrders();
    }

    /**
     * Format date for API - returns YYYY-MM-DD string in local timezone
     */
    private formatDateForApi(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    /**
     * Load purchase orders from API
     */
    loadPurchaseOrders(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): void {
        if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
            pageNumber = 1;
        }
        if (!pageSize || isNaN(pageSize) || pageSize < 1) {
            pageSize = 10;
        }
        let fromDateStr: string | undefined;
        let toDateStr: string | undefined;

        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            // Format as YYYY-MM-DD to preserve local date
            fromDateStr = this.formatDateForApi(this.dateRange[0]);
            toDateStr = this.formatDateForApi(this.dateRange[1]);
        }

        this.loading = true;
        this.poService
            .getPurchaseOrders({
                search: searchTerm,
                pageNumber: pageNumber,
                pageSize: pageSize,
                status: this.filterStatus ?? '',
                fromDate: fromDateStr,
                toDate: toDateStr
            })
            .subscribe({
                next: (response) => {
                    this.purchaseOrders = response.data;
                    this.totalRecords = response.pagination.totalCount;
                    this.rows = response.pagination.pageSize;
                    this.currentPage = response.pagination.pageNumber;
                    this.loading = false;
                },
                error: (error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load purchase orders'
                    });
                    console.error('Error loading purchase orders:', error);
                    this.loading = false;
                }
            });
    }

    /**
     * Handle create button click
     */
    onCreateClick(): void {
        this.router.navigate(['/procurement/purchase-orders/create']);
    }

    /**
     * Check if any filter is active
     */
    hasActiveFilters(): boolean {
        return !!(this.searchTerm || this.filterStatus || (this.dateRange && this.dateRange.length > 0));
    }

    /**
     * Handle search
     */
    onSearch(): void {
        this.loadPurchaseOrders(1, this.rows, this.searchTerm);
    }

    /**
     * Handle search clear
     */
    clearSearch(): void {
        this.searchTerm = '';
        this.loadPurchaseOrders(1, this.rows);
    }

    /**
     * Handle filter change
     */
    onFilterChange(): void {
        // TODO: Implement filtering logic with status and date range
        this.loadPurchaseOrders(1, this.rows, this.searchTerm);
    }

    /**
     * Clear all filters
     */
    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = null;
        this.dateRange = null;
        this.loadPurchaseOrders(1, this.rows);
    }

    /**
     * Export orders to CSV or JSON
     */
    exportOrders(format: 'csv' | 'json'): void {
        if (this.purchaseOrders.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Data',
                detail: 'No purchase orders to export'
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
        const headers = ['PO Number', 'Supplier', 'Order Date', 'Delivery Date', 'Status', 'Grand Total', 'Outstanding'];
        const rows = this.purchaseOrders.map((po) => [
            po.poNumber,
            po.supplierName || '',
            po.orderDate ? new Date(po.orderDate).toLocaleDateString() : '',
            po.deliveryDate ? new Date(po.deliveryDate).toLocaleDateString() : '',
            po.status,
            po.grandTotal.toString(),
            po.outstandingAmount.toString()
        ]);

        const csvContent = [headers.join(','), ...rows.map((row) => row.join(','))].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `purchase-orders-${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Purchase orders exported to CSV'
        });
    }

    /**
     * Export to JSON
     */
    private exportToJSON(): void {
        const jsonContent = JSON.stringify(this.purchaseOrders, null, 2);
        const blob = new Blob([jsonContent], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `purchase-orders-${new Date().toISOString().split('T')[0]}.json`;
        a.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Purchase orders exported to JSON'
        });
    }

    /**
     * Handle edit click
     */
    onEditClick(po: PurchaseOrderResponse): void {
        this.selectedPurchaseOrder = po;
        this.displayUpdateDialog = true;
    }

    /**
     * Handle page change
     */
    onPageChange(event: { page: number; rows: number }): void {
        this.loadPurchaseOrders(event.page, event.rows, this.searchTerm);
    }

    /**
     * Handle purchase order created
     */
    onPurchaseOrderCreated(po: PurchaseOrderResponse): void {
        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Purchase Order '${po.poNumber}' created successfully`
        });
        this.loadPurchaseOrders(1, this.rows, this.searchTerm);
    }

    /**
     * Handle purchase order updated
     */
    onPurchaseOrderUpdated(po: PurchaseOrderResponse): void {
        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Purchase Order '${po.poNumber}' updated successfully`
        });
        this.loadPurchaseOrders(this.currentPage, this.rows, this.searchTerm);
    }

    /**
     * Handle purchase order deleted
     */
    onPurchaseOrderDeleted(): void {
        this.loadPurchaseOrders(this.currentPage, this.rows, this.searchTerm);
    }
}
