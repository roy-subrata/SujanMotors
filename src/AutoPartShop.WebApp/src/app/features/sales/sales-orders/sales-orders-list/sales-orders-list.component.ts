/* ============================================================
   SALES ORDERS LIST COMPONENT
   ============================================================

   PURPOSE: Enterprise-grade data listing page with:
   - Backend-ready pagination & filtering
   - Responsive design (table on desktop, cards on mobile)
   - PrimeNG components + Tailwind CSS utilities

   TEMPLATE USAGE:
   - Copy this file for similar listing pages
   - Replace 'SalesOrder' with your entity name
   - Update service, routes, and status options

   SECTIONS:
   1. Imports & Component Setup
   2. Properties & State
   3. Lifecycle Hooks
   4. Data Loading (Backend Integration)
   5. Filter Methods
   6. Pagination Methods
   7. CRUD Operations
   8. Utility Methods
   ============================================================ */

import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';

// ===================== PRIMENG IMPORTS =====================
// Import only the PrimeNG modules you need
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { PanelModule } from 'primeng/panel';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';

// ===================== PRIMENG SERVICES =====================
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';

// ===================== APPLICATION IMPORTS =====================
// Replace these with your entity-specific service and types
import { SalesOrderService, SalesOrderResponse } from '../../services/sales-order.service';
import { CurrencyService } from '../../../../shared/services/currency.service';

/* ============================================================
   COMPONENT DECORATOR
   ============================================================ */
@Component({
    selector: 'app-sales-orders-list',
    standalone: true,
    imports: [
        // Angular Core
        CommonModule,
        FormsModule,
        // PrimeNG Modules
        TableModule,
        ButtonModule,
        InputTextModule,
        Select,
        DatePickerModule,
        PanelModule,
        CardModule,
        TagModule,
        MenuModule,
        TooltipModule,
        ToastModule,
        ConfirmDialogModule,
        PaginatorModule,
        SkeletonModule,
        InputGroupModule,
        InputGroupAddonModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './sales-orders-list.component.html',
    styleUrls: ['./sales-orders-list.component.css']
})
export class SalesOrdersListComponent implements OnInit {
    /* ============================================================
     SECTION 1: DEPENDENCY INJECTION
     ============================================================
     Use inject() for cleaner dependency injection.
     Replace services with your entity-specific services.
  */
    private readonly salesOrderService = inject(SalesOrderService);
    private readonly currencyService = inject(CurrencyService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    /* ============================================================
     SECTION 2: VIEW CHILD REFERENCES
     ============================================================
     References to PrimeNG components for programmatic control
  */
    @ViewChild('actionMenu') actionMenu!: Menu;

    /* ============================================================
     SECTION 3: DATA STATE
     ============================================================
     Main data arrays and loading states
  */
    // Primary data array - replace type with your entity
    salesOrders: SalesOrderResponse[] = [];

    // Selected item for context menu actions
    selectedOrder: SalesOrderResponse | null = null;

    // Loading state for showing skeletons
    loading = false;

    /* ============================================================
     SECTION 4: PAGINATION STATE
     ============================================================
     Backend pagination configuration
  */
    // Total records from backend (for paginator)
    totalRecords = 0;

    // Current page (1-based for backend API)
    pageNumber = 1;

    // Records per page
    pageSize = 10;

    // First record index (0-based for PrimeNG paginator)
    first = 0;

    // Available page size options
    pageSizeOptions = [10, 20, 50];

    /* ============================================================
     SECTION 5: FILTER STATE
     ============================================================
     Filter values bound to form inputs
  */
    // Text search filter
    searchTerm = '';

    // Status dropdown filter
    filterStatus = '';

    // Date range filter [startDate, endDate]
    dateRange: Date[] = [];

    /* ============================================================
     SECTION 6: DROPDOWN OPTIONS
     ============================================================
     Static options for dropdowns - customize per entity
  */
    statusOptions = [
        { label: 'All Statuses',       value: '' },
        { label: 'Pending',            value: 'PENDING' },
        { label: 'Confirmed',          value: 'CONFIRMED' },
        { label: 'Ready for Delivery', value: 'READY_FOR_DELIVERY' },
        { label: 'Delivered',          value: 'DELIVERED' },
        { label: 'Cancelled',          value: 'CANCELLED' }
    ];

    /* ============================================================
     SECTION 7: MENU ITEMS
     ============================================================
     Action menu items - dynamically set based on selected row
  */
    actionMenuItems: MenuItem[] = [];

    /* ============================================================
     SECTION 8: RESPONSIVE BREAKPOINT
     ============================================================
     Track screen size for responsive layout
  */
   // isMobile = false;

    // Expose Math for template usage
    Math = Math;

    /* ============================================================
     LIFECYCLE HOOKS
     ============================================================ */

    ngOnInit(): void {
        // Check initial screen size
      //  this.checkScreenSize();

        // Listen for window resize
        window.addEventListener('resize', () => this.checkScreenSize());

        // Read query params for initial filters (e.g., status from dashboard link)
        this.route.queryParams.subscribe((params) => {
            if (params['status']) {
                this.filterStatus = params['status'];
            }
        });

        // Initial data load
        this.loadData();
    }

    /* ============================================================
     SECTION: RESPONSIVE HANDLING
     ============================================================
     Detect mobile breakpoint for switching between table and cards
  */
    private checkScreenSize(): void {
        // Mobile breakpoint at 768px (md in Tailwind)
      //  this.isMobile = window.innerWidth < 768;
    }

    /* ============================================================
     SECTION: DATA LOADING
     ============================================================
     Backend integration for fetching data with pagination & filters
  */

    /**
     * Main data loading method
     * Called on: initial load, filter change, pagination change
     *
     * TEMPLATE NOTE: Replace service call with your entity service
     */
    loadData(): void {
        this.loading = true;
        if (this.dateRange && this.dateRange[0] && !this.dateRange[1]) return;
        // Build API parameters
        const fromDate = this.dateRange?.[0] ? this.formatDateForApi(this.dateRange[0]) : undefined;
        const toDate = this.dateRange?.[1] ? this.formatDateForApi(this.dateRange[1]) : undefined;

        // Call backend service
        // TEMPLATE NOTE: Update this service call for your entity
        this.salesOrderService
            .getSalesOrders({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                search: this.searchTerm,
                status: this.filterStatus,
                fromDate: fromDate,
                toDate: toDate
            })
            .subscribe({
                next: (response) => {
                    this.salesOrders = response.data;
                    this.totalRecords = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Error loading data:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load sales orders'
                    });
                    this.loading = false;
                }
            });
    }

    /**
     * Handle PrimeNG table lazy load event
     * Triggered when: sorting, pagination via table
     */
    onLazyLoad(event: TableLazyLoadEvent): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    /* ============================================================
     SECTION: FILTER METHODS
     ============================================================
     Handle filter interactions and reset
  */

    /**
     * Check if any filter is active
     * Used to show/hide "Clear Filters" button
     */
    hasActiveFilters(): boolean {
        return !!(this.searchTerm || this.filterStatus || (this.dateRange && this.dateRange.length > 0));
    }

    /**
     * Handle search input (on Enter key)
     */
    onSearch(): void {
        this.resetPagination();
        this.loadData();
    }

    /**
     * Handle filter dropdown changes
     */
    onFilterChange(): void {
        this.resetPagination();
        this.loadData();
    }

    /**
     * Clear all filters and reload
     */
    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.dateRange = [];
        this.resetPagination();
        this.loadData();
    }

    /**
     * Reset pagination to first page
     */
    private resetPagination(): void {
        this.pageNumber = 1;
        this.first = 0;
    }

    /* ============================================================
     SECTION: PAGINATION METHODS
     ============================================================
     Handle PrimeNG paginator events
  */

    /**
     * Handle paginator page change
     */
    onPageChange(event: PaginatorState): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    /* ============================================================
     SECTION: ACTION MENU
     ============================================================
     Dynamic menu items based on selected row
  */

    /**
     * Show action menu for a row
     * Dynamically builds menu items based on row state
     */
    showActionMenu(event: Event, order: SalesOrderResponse): void {
        this.selectedOrder = order;

        // Build menu items dynamically based on order state
        this.actionMenuItems = [
            {
                label: 'View Details',
                icon: 'pi pi-eye',
                command: () => this.viewOrder(order)
            },
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                command: () => this.editOrder(order),
                visible: order.status === 'DRAFT'
            },
            {
                label: 'Confirm Order',
                icon: 'pi pi-check-circle',
                command: () => this.confirmOrder(order),
                visible: order.status === 'DRAFT'
            },
            { separator: true },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                command: () => this.deleteOrder(order),
                visible: order.status === 'DRAFT',
                styleClass: 'text-red-600'
            }
        ];

        this.actionMenu.toggle(event);
    }

    /* ============================================================
     SECTION: CRUD OPERATIONS
     ============================================================
     Navigation and data manipulation methods
  */

    /**
     * Navigate to create new order page
     */
    createOrder(): void {
        this.router.navigate(['/sales/sales-orders/create']);
    }

    /**
     * Navigate to view order details
     */
    viewOrder(order: SalesOrderResponse): void {
        this.router.navigate(['/sales/sales-orders/view'], {
            queryParams: { id: order.id }
        });
    }

    /**
     * Navigate to edit order
     */
    editOrder(order: SalesOrderResponse): void {
        this.router.navigate(['/sales/sales-orders/edit'], {
            queryParams: { id: order.id }
        });
    }

    /**
     * Confirm a draft order
     */
    confirmOrder(order: SalesOrderResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to confirm Sales Order ${order.soNumber}?`,
            header: 'Confirm Order',
            icon: 'pi pi-check-circle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.salesOrderService.confirmSalesOrder(order.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Sales order confirmed successfully'
                        });
                        this.loadData();
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

    /**
     * Delete a draft order
     */
    deleteOrder(order: SalesOrderResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete Sales Order ${order.soNumber}? This action cannot be undone.`,
            header: 'Delete Order',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.salesOrderService.deleteSalesOrder(order.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Sales order deleted successfully'
                        });
                        this.loadData();
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

    /**
     * Refresh data (manual reload)
     */
    refreshData(): void {
        this.loadData();
    }

    /* ============================================================
     SECTION: UTILITY / FORMATTING METHODS
     ============================================================
     Helper methods for formatting display values
  */

    /**
     * Format date for API calls (YYYY-MM-DD)
     */
    private formatDateForApi(date: Date): string {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }

    /**
     * Format date for display
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
     * Format currency for display
     */
    formatCurrency(amount: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currency);
    }

    /**
     * Get PrimeNG tag severity based on status
     *
     * TEMPLATE NOTE: Customize severity mapping for your entity statuses
     * Available severities: 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'
     */
    getStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            PENDING:            'secondary',
            DRAFT:              'secondary',       // legacy
            CONFIRMED:          'info',
            READY_FOR_DELIVERY: 'warn',
            DELIVERED:          'success',
            CANCELLED:          'danger',
            // legacy statuses
            PAID: 'info', PACKED: 'warn', PARTIALLY_SHIPPED: 'warn', SHIPPED: 'info',
            COMPLETED: 'success', RETURNED: 'danger'
        };
        return map[status] ?? 'secondary';
    }

    formatStatus(status: string): string {
        const labels: Record<string, string> = {
            PENDING:            'Pending',
            DRAFT:              'Pending',
            CONFIRMED:          'Confirmed',
            READY_FOR_DELIVERY: 'Ready for Delivery',
            DELIVERED:          'Delivered',
            CANCELLED:          'Cancelled',
            PAID: 'Paid', PACKED: 'Packed', PARTIALLY_SHIPPED: 'Partially Shipped',
            SHIPPED: 'Shipped', COMPLETED: 'Completed', RETURNED: 'Returned'
        };
        return labels[status] ?? (status ?? '-').split('_')
            .map(w => w.charAt(0) + w.slice(1).toLowerCase()).join(' ');
    }
}
