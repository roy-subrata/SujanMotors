import { Component, OnInit, ViewChild, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';

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

import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';

import { SalesOrderService, SalesOrderResponse } from '../../services/sales-order.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

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
        InputGroupAddonModule,
        PageContainerComponent,
        PageHeaderComponent,
        FilterBarComponent,
        DataPaginationComponent
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './sales-orders-list.component.html',
    styleUrls: ['./sales-orders-list.component.css']
})
export class SalesOrdersListComponent implements OnInit {
    private readonly salesOrderService = inject(SalesOrderService);
    private readonly currencyService = inject(CurrencyService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    @ViewChild('actionMenu') actionMenu!: Menu;

    salesOrders: SalesOrderResponse[] = [];
    selectedOrder: SalesOrderResponse | null = null;
    loading = false;

    totalRecords = 0;
    pageNumber = 1;
    pageSize = 10;
    first = 0;
    pageSizeOptions = [10, 20, 50];

    searchTerm = '';
    filterStatus = '';
    dateRange: Date[] = [];

    statusOptions: { label: string; value: string }[] = [];

    actionMenuItems: MenuItem[] = [];

    Math = Math;

    ngOnInit(): void {
        this.buildStatusOptions();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStatusOptions();
            if (this.selectedOrder) this.buildActionMenuItems(this.selectedOrder);
        });

        this.route.queryParams.subscribe((params) => {
            if (params['status']) {
                this.filterStatus = params['status'];
            }
        });

        this.loadData();
    }

    private buildStatusOptions(): void {
        this.statusOptions = [
            { label: this.i18n.t('salesOrders.statusOptions.allStatuses'), value: '' },
            { label: this.i18n.t('salesOrders.statusOptions.pending'),      value: 'PENDING' },
            { label: this.i18n.t('salesOrders.statusOptions.confirmed'),     value: 'CONFIRMED' },
            { label: this.i18n.t('salesOrders.statusOptions.readyForDelivery'), value: 'READY_FOR_DELIVERY' },
            { label: this.i18n.t('salesOrders.statusOptions.delivered'),     value: 'DELIVERED' },
            { label: this.i18n.t('salesOrders.statusOptions.cancelled'),     value: 'CANCELLED' }
        ];
    }

    private buildActionMenuItems(order: SalesOrderResponse): void {
        const cancellable = ['PENDING', 'DRAFT', 'CONFIRMED'].includes(order.status);
        this.actionMenuItems = [
            {
                label: this.i18n.t('common.actions.viewDetails'),
                icon: 'pi pi-eye',
                command: () => this.viewOrder(order)
            },
            {
                label: this.i18n.t('common.actions.edit'),
                icon: 'pi pi-pencil',
                command: () => this.editOrder(order),
                visible: order.status === 'DRAFT' || order.status === 'PENDING'
            },
            {
                label: this.i18n.t('common.actions.confirmOrder'),
                icon: 'pi pi-check-circle',
                command: () => this.confirmOrder(order),
                visible: order.status === 'DRAFT' || order.status === 'PENDING'
            },
            { separator: true },
            {
                label: 'Cancel Order',
                icon: 'pi pi-times-circle',
                command: () => this.cancelOrder(order),
                visible: cancellable,
                styleClass: 'text-orange-600'
            },
            {
                label: this.i18n.t('common.actions.delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteOrder(order),
                visible: order.status === 'DRAFT',
                styleClass: 'text-red-600'
            }
        ];
    }

    loadData(): void {
        this.loading = true;
        if (this.dateRange && this.dateRange[0] && !this.dateRange[1]) return;
        const fromDate = this.dateRange?.[0] ? this.formatDateForApi(this.dateRange[0]) : undefined;
        const toDate = this.dateRange?.[1] ? this.formatDateForApi(this.dateRange[1]) : undefined;

        this.salesOrderService
            .getSalesOrders({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                search: this.searchTerm,
                status: this.filterStatus,
                fromDate,
                toDate
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
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('salesOrders.messages.loadFailed')
                    });
                    this.loading = false;
                }
            });
    }

    onLazyLoad(event: TableLazyLoadEvent): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    hasActiveFilters(): boolean {
        return !!(this.searchTerm || this.filterStatus || (this.dateRange && this.dateRange.length > 0));
    }

    onSearch(): void {
        this.resetPagination();
        this.loadData();
    }

    onFilterChange(): void {
        this.resetPagination();
        this.loadData();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.dateRange = [];
        this.resetPagination();
        this.loadData();
    }

    private resetPagination(): void {
        this.pageNumber = 1;
        this.first = 0;
    }

    onPageChange(event: PaginatorState): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    showActionMenu(event: Event, order: SalesOrderResponse): void {
        this.selectedOrder = order;
        this.buildActionMenuItems(order);
        this.actionMenu.toggle(event);
    }

    createOrder(): void {
        this.router.navigate(['/sales/sales-orders/create']);
    }

    viewOrder(order: SalesOrderResponse): void {
        this.router.navigate(['/sales/sales-orders/view'], { queryParams: { id: order.id } });
    }

    editOrder(order: SalesOrderResponse): void {
        this.router.navigate(['/sales/sales-orders/edit'], { queryParams: { id: order.id } });
    }

    confirmOrder(order: SalesOrderResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('salesOrders.messages.confirmOrderConfirm', { number: order.soNumber }),
            header: this.i18n.t('salesOrders.confirmOrder'),
            icon: 'pi pi-check-circle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.salesOrderService.confirmSalesOrder(order.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('salesOrders.messages.confirmOrderSuccess')
                        });
                        this.loadData();
                    },
                    error: () => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: this.i18n.t('salesOrders.messages.confirmOrderFailed')
                        });
                    }
                });
            }
        });
    }

    cancelOrder(order: SalesOrderResponse): void {
        this.confirmationService.confirm({
            message: `Cancel order ${order.soNumber}? This will reverse any stock deductions and cannot be undone.`,
            header: 'Cancel Sales Order',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-warning',
            accept: () => {
                this.salesOrderService.cancelSalesOrder(order.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: `Order ${order.soNumber} has been cancelled.`
                        });
                        this.loadData();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: err?.error?.message ?? 'Failed to cancel order.'
                        });
                    }
                });
            }
        });
    }

    deleteOrder(order: SalesOrderResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('salesOrders.messages.deleteConfirm', { number: order.soNumber }),
            header: this.i18n.t('common.messages.confirmDeletion'),
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.salesOrderService.deleteSalesOrder(order.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('salesOrders.messages.deleteSuccess')
                        });
                        this.loadData();
                    },
                    error: () => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: this.i18n.t('salesOrders.messages.deleteFailed')
                        });
                    }
                });
            }
        });
    }

    refreshData(): void {
        this.loadData();
    }

    private formatDateForApi(date: Date): string {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }

    formatDate(date: string): string {
        if (!date) return '-';
        return new Date(date).toLocaleDateString('en-IN', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
        });
    }

    formatCurrency(amount: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currency);
    }

    getStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            PENDING:            'secondary',
            DRAFT:              'secondary',
            CONFIRMED:          'info',
            READY_FOR_DELIVERY: 'warn',
            DELIVERED:          'success',
            CANCELLED:          'danger',
            PAID: 'info', PACKED: 'warn', PARTIALLY_SHIPPED: 'warn', SHIPPED: 'info',
            COMPLETED: 'success', RETURNED: 'danger'
        };
        return map[status] ?? 'secondary';
    }

    formatStatus(status: string): string {
        const labels: Record<string, string> = {
            PENDING:            this.i18n.t('salesOrders.statusOptions.pending'),
            DRAFT:              this.i18n.t('salesOrders.statusOptions.pending'),
            CONFIRMED:          this.i18n.t('salesOrders.statusOptions.confirmed'),
            READY_FOR_DELIVERY: this.i18n.t('salesOrders.statusOptions.readyForDelivery'),
            DELIVERED:          this.i18n.t('salesOrders.statusOptions.delivered'),
            CANCELLED:          this.i18n.t('salesOrders.statusOptions.cancelled'),
            PAID:               this.i18n.t('salesOrders.statusOptions.paid'),
            PACKED:             this.i18n.t('salesOrders.statusOptions.packed'),
            PARTIALLY_SHIPPED:  this.i18n.t('salesOrders.statusOptions.partiallyShipped'),
            SHIPPED:            this.i18n.t('salesOrders.statusOptions.shipped'),
            COMPLETED:          this.i18n.t('salesOrders.statusOptions.completed'),
            RETURNED:           this.i18n.t('salesOrders.statusOptions.returned')
        };
        return labels[status] ?? (status ?? '-').split('_')
            .map(w => w.charAt(0) + w.slice(1).toLowerCase()).join(' ');
    }
}
