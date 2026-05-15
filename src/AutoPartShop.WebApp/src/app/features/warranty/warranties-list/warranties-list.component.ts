import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { WarrantyService, WarrantyRegistrationResponse, CreateWarrantyRegistrationRequest } from '../services/warranty.service';
import { SalesOrderService, SalesOrderResponse, SalesOrderLineResponse } from '../../sales/services/sales-order.service';
import { CurrencyService } from '../../../shared/services/currency.service';

@Component({
    selector: 'app-warranties-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        TagModule,
        ToastModule,
        CardModule,
        TooltipModule,
        DialogModule,
        SelectModule,
        DatePickerModule,
        TextareaModule,
        InputNumberModule,
        ConfirmDialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './warranties-list.component.html',
    styleUrls: ['./warranties-list.component.css']
})
export class WarrantiesListComponent implements OnInit {
    private readonly warrantyService = inject(WarrantyService);
    private readonly salesOrderService = inject(SalesOrderService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    readonly currencyService = inject(CurrencyService);

    warranties: WarrantyRegistrationResponse[] = [];
    filteredWarranties: WarrantyRegistrationResponse[] = [];
    isLoading = false;
    searchText = '';
    selectedStatus = '';

    // Summary metrics
    totalActive = 0;
    totalExpiringSoon = 0;
    totalClaimed = 0;
    totalVoid = 0;

    statuses = [
        { label: 'All Statuses', value: '' },
        { label: 'Active', value: 'ACTIVE' },
        { label: 'Expired', value: 'EXPIRED' },
        { label: 'Claimed', value: 'CLAIMED' },
        { label: 'Void', value: 'VOID' }
    ];

    // Create dialog
    showCreateDialog = false;
    isCreating = false;
    soNumberSearch = '';
    isLoadingOrder = false;
    selectedOrder: SalesOrderResponse | null = null;
    selectedLineId = '';
    warrantyTypes = [
        { label: 'Manufacturer Coverage', value: 'MANUFACTURER' },
        { label: 'Seller Coverage', value: 'SELLER' },
        { label: 'Extended Coverage', value: 'EXTENDED' }
    ];
    newWarranty = {
        warrantyStartDate: new Date(),
        warrantyPeriodMonths: 12,
        warrantyType: 'MANUFACTURER',
        warrantyTerms: '',
        certificateNumber: ''
    };

    // Void dialog
    showVoidDialog = false;
    selectedWarranty: WarrantyRegistrationResponse | null = null;
    voidReason = '';

    // Detail dialog
    showDetailDialog = false;
    detailWarranty: WarrantyRegistrationResponse | null = null;

    ngOnInit(): void {
        this.loadWarranties();
    }

    loadWarranties(): void {
        this.isLoading = true;
        this.warrantyService.getAllWarranties().subscribe({
            next: (data) => {
                this.warranties = data;
                this.applyFilters();
                this.updateMetrics();
                this.isLoading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to load warranties')
                });
                this.isLoading = false;
            }
        });
    }

    updateMetrics(): void {
        this.totalActive = this.warranties.filter(w => w.status === 'ACTIVE').length;
        this.totalExpiringSoon = this.warranties.filter(w => w.status === 'ACTIVE' && w.daysUntilExpiry >= 0 && w.daysUntilExpiry <= 30).length;
        this.totalClaimed = this.warranties.filter(w => w.status === 'CLAIMED').length;
        this.totalVoid = this.warranties.filter(w => w.status === 'VOID').length;
    }

    applyFilters(): void {
        const search = this.searchText.toLowerCase();
        this.filteredWarranties = this.warranties.filter(warranty => {
            const matchesSearch = !search ||
                warranty.warrantyNumber.toLowerCase().includes(search) ||
                warranty.partName.toLowerCase().includes(search) ||
                warranty.customerName.toLowerCase().includes(search) ||
                warranty.certificateNumber?.toLowerCase().includes(search) ||
                warranty.salesOrderNumber?.toLowerCase().includes(search);

            const matchesStatus = !this.selectedStatus || warranty.status === this.selectedStatus;

            return matchesSearch && matchesStatus;
        });
    }

    onSearch(): void {
        this.applyFilters();
    }

    onStatusChange(): void {
        this.applyFilters();
    }

    clearSearch(): void {
        this.searchText = '';
        this.applyFilters();
    }

    getStatusSeverity(status: string): 'success' | 'warn' | 'info' | 'danger' | 'secondary' {
        switch (status) {
            case 'ACTIVE': return 'success';
            case 'EXPIRED': return 'warn';
            case 'CLAIMED': return 'info';
            case 'VOID': return 'danger';
            default: return 'secondary';
        }
    }

    getDaysUntilExpiryClass(warranty: WarrantyRegistrationResponse): string {
        if (warranty.status === 'EXPIRED' || warranty.status === 'VOID') return 'expiry-expired';
        if (warranty.daysUntilExpiry < 0) return 'expiry-expired';
        if (warranty.daysUntilExpiry <= 30) return 'expiry-warning';
        return 'expiry-ok';
    }

    getDaysUntilExpiryDisplay(warranty: WarrantyRegistrationResponse): string {
        if (warranty.status === 'EXPIRED') return 'Expired';
        if (warranty.status === 'VOID') return 'Voided';
        if (warranty.daysUntilExpiry < 0) return 'Expired';
        if (warranty.daysUntilExpiry === 0) return 'Expires today';
        if (warranty.daysUntilExpiry === 1) return '1 day left';
        return `${warranty.daysUntilExpiry} days left`;
    }

    formatDate(date: Date | string | null): string {
        if (!date) return '-';
        return new Date(date).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    // ==================== CREATE DIALOG ====================

    openCreateDialog(): void {
        this.soNumberSearch = '';
        this.selectedOrder = null;
        this.selectedLineId = '';
        this.newWarranty = {
            warrantyStartDate: new Date(),
            warrantyPeriodMonths: 12,
            warrantyType: 'MANUFACTURER',
            warrantyTerms: '',
            certificateNumber: ''
        };
        this.showCreateDialog = true;
    }

    searchSalesOrder(): void {
        if (!this.soNumberSearch.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please enter a sales order number' });
            return;
        }

        this.isLoadingOrder = true;
        this.selectedOrder = null;
        this.selectedLineId = '';

        this.salesOrderService.getSalesOrderByNumber(this.soNumberSearch.trim()).subscribe({
            next: (order) => {
                this.selectedOrder = order;
                this.isLoadingOrder = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Not Found',
                    detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Sales order not found')
                });
                this.isLoadingOrder = false;
            }
        });
    }

    getSelectedLine(): SalesOrderLineResponse | null {
        if (!this.selectedOrder || !this.selectedLineId) return null;
        return this.selectedOrder.lines.find(l => l.id === this.selectedLineId) || null;
    }

    onLineSelect(): void {
        const line = this.getSelectedLine();
        if (line && this.selectedOrder) {
            this.newWarranty.warrantyStartDate = new Date(this.selectedOrder.orderDate);
        }
    }

    canCreateWarranty(): boolean {
        return !!this.selectedOrder &&
            !!this.selectedLineId &&
            this.newWarranty.warrantyPeriodMonths > 0 &&
            !!this.newWarranty.warrantyType &&
            !!this.newWarranty.warrantyTerms.trim();
    }

    createWarranty(): void {
        if (!this.canCreateWarranty() || !this.selectedOrder) return;

        const line = this.getSelectedLine();
        if (!line) return;

        this.isCreating = true;

        const request: CreateWarrantyRegistrationRequest = {
            partId: line.partId,
            salesOrderId: this.selectedOrder.id,
            salesOrderLineId: line.id,
            customerId: this.selectedOrder.customerId,
            saleDate: new Date(this.selectedOrder.orderDate),
            warrantyStartDate: this.newWarranty.warrantyStartDate,
            warrantyPeriodMonths: this.newWarranty.warrantyPeriodMonths,
            warrantyType: this.newWarranty.warrantyType,
            warrantyTerms: this.newWarranty.warrantyTerms,
            certificateNumber: this.newWarranty.certificateNumber || undefined
        };

        this.warrantyService.createWarranty(request).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Warranty registered successfully'
                });
                this.showCreateDialog = false;
                this.isCreating = false;
                this.loadWarranties();
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to create warranty')
                });
                this.isCreating = false;
            }
        });
    }

    // ==================== VOID DIALOG ====================

    openVoidDialog(warranty: WarrantyRegistrationResponse): void {
        this.selectedWarranty = warranty;
        this.voidReason = '';
        this.showVoidDialog = true;
    }

    confirmVoid(): void {
        if (!this.selectedWarranty || !this.voidReason.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please enter a reason for voiding' });
            return;
        }

        this.warrantyService.voidWarranty(this.selectedWarranty.id, { reason: this.voidReason }).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Warranty voided successfully'
                });
                this.showVoidDialog = false;
                this.loadWarranties();
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to void warranty')
                });
            }
        });
    }

    // ==================== DETAIL DIALOG ====================

    openDetailDialog(warranty: WarrantyRegistrationResponse): void {
        this.detailWarranty = warranty;
        this.showDetailDialog = true;
    }

    // ==================== DELETE ====================

    deleteWarranty(warranty: WarrantyRegistrationResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete warranty ${warranty.warrantyNumber}? This action cannot be undone.`,
            header: 'Confirm Delete',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.warrantyService.deleteWarranty(warranty.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Warranty deleted successfully'
                        });
                        this.loadWarranties();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to delete warranty')
                        });
                    }
                });
            }
        });
    }
}
