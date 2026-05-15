import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SkeletonModule } from 'primeng/skeleton';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { BarcodeDialogComponent } from './barcode-dialog/barcode-dialog.component';
import { PartService, PartResponse } from '../services/part.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { PriceCodeService } from '@/shared/services/price-code.service';

@Component({
    selector: 'app-parts',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        Select,
        TagModule,
        MenuModule,
        TooltipModule,
        ToastModule,
        ConfirmDialogModule,
        SkeletonModule
    ],
    providers: [MessageService, ConfirmationService, DialogService],
    templateUrl: './parts.component.html',
    styleUrls: ['./parts.component.css']
})
export class PartsComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly messageService = inject(MessageService);
    private readonly dialogService = inject(DialogService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly currencyService = inject(CurrencyService);
    readonly priceCodeService = inject(PriceCodeService);
    private readonly router = inject(Router);

    @ViewChild('actionMenu') actionMenu!: Menu;

    parts: PartResponse[] = [];
    selectedPart: PartResponse | null = null;
    loading = false;
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 10;
    first = 0;
    pageSizeOptions = [10, 20, 50];
    searchTerm = '';
    filterStatus = '';

    actionMenuItems: MenuItem[] = [];

    statusOptions = [
        { label: 'All Statuses', value: '' },
        { label: 'Active', value: 'ACTIVE' },
        { label: 'Inactive', value: 'INACTIVE' }
    ];

    Math = Math;

    constructor() {}

    ngOnInit(): void {
        this.loadData();
    }

    /**
     * Load parts from API
     */
    loadData(): void {
        this.loading = true;
        const isActive = this.filterStatus === 'ACTIVE' ? true : this.filterStatus === 'INACTIVE' ? false : undefined;
        this.partService.getParts({
            search: this.searchTerm,
            pageNumber: this.pageNumber,
            pageSize: this.pageSize,
            isActive: isActive
        }).subscribe({
            next: (response) => {
                this.parts = response.data;
                this.totalRecords = response.pagination.totalCount;
                this.loading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load parts'
                });
                console.error('Error loading parts:', error);
                this.loading = false;
            }
        });
    }

    /**
     * Handle create button click
     */
    createPart(): void {
        this.router.navigate(['/inventory/parts/create']);
    }

    /**
     * Handle search
     */
    onSearch(): void {
        this.resetPagination();
        this.loadData();
    }

    /**
     * Handle filter change
     */
    onFilterChange(): void {
        this.resetPagination();
        this.loadData();
    }

    /**
     * Clear all filters
     */
    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.resetPagination();
        this.loadData();
    }

    /**
     * Check if filters are active
     */
    hasActiveFilters(): boolean {
        return !!(this.searchTerm || this.filterStatus);
    }

    /**
     * Handle PrimeNG table lazy load event
     */
    onLazyLoad(event: TableLazyLoadEvent): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    /**
     * Handle paginator page change
     */
    onPageChange(event: { first?: number; rows?: number }): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    /**
     * Reset pagination to first page
     */
    private resetPagination(): void {
        this.pageNumber = 1;
        this.first = 0;
    }

    /**
     * Show action menu for a part
     */
    showActionMenu(event: Event, part: PartResponse): void {
        this.selectedPart = part;
        this.actionMenuItems = [
            {
                label: 'View Details',
                icon: 'pi pi-eye',
                command: () => this.viewPart(part)
            },
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                command: () => this.editPart(part)
            },
            {
                label: 'Show Barcode',
                icon: 'pi pi-qrcode',
                command: () => this.showBarcode(part)
            },
            { separator: true },
            {
                label: 'Activate',
                icon: 'pi pi-check',
                command: () => this.activatePart(part),
                visible: !part.isActive
            },
            {
                label: 'Deactivate',
                icon: 'pi pi-times',
                command: () => this.deactivatePart(part),
                visible: part.isActive
            },
            { separator: true },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                command: () => this.deletePart(part),
                styleClass: 'text-red-600'
            }
        ];

        this.actionMenu.toggle(event);
    }

    /**
     * View part details
     */
    viewPart(part: PartResponse): void {
        this.router.navigate(['/inventory/parts', part.id]);
    }

    /**
     * Edit part
     */
    editPart(part: PartResponse): void {
        this.router.navigate(['/inventory/parts/edit'], {
            queryParams: { id: part.id, mode: 'edit' }
        });
    }

    /**
     * Show barcode dialog
     */
    showBarcode(part: PartResponse): void {
        this.dialogService.open(BarcodeDialogComponent, {
            data: { part: part },
            header: 'Label & Barcode Generator',
            width: '100vw',
            height: '100vh',
            styleClass: 'fullscreen-dialog',
            modal: true,
            closable: true
        });
    }

    /**
     * Activate part
     */
    private activatePart(part: PartResponse): void {
        this.partService.activatePart(part.id).subscribe({
            next: (updatedPart) => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Part activated successfully'
                });
                const index = this.parts.findIndex(p => p.id === part.id);
                if (index !== -1) {
                    this.parts[index] = updatedPart;
                    this.parts = [...this.parts];
                }
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to activate part'
                });
            }
        });
    }

    /**
     * Deactivate part
     */
    private deactivatePart(part: PartResponse): void {
        this.partService.deactivatePart(part.id).subscribe({
            next: (updatedPart) => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Part deactivated successfully'
                });
                const index = this.parts.findIndex(p => p.id === part.id);
                if (index !== -1) {
                    this.parts[index] = updatedPart;
                    this.parts = [...this.parts];
                }
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to deactivate part'
                });
            }
        });
    }

    /**
     * Delete part
     */
    private deletePart(part: PartResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete part "${part.name}"? This action cannot be undone.`,
            header: 'Delete Part',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.partService.deletePart(part.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Part deleted successfully'
                        });
                        this.loadData();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to delete part'
                        });
                    }
                });
            }
        });
    }

    /**
     * Refresh data
     */
    refreshData(): void {
        this.loadData();
    }

    /**
     * Format currency
     */
    formatCurrency(amount: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currency);
    }

    formatCostPrice(amount: number): string {
        const coded = this.priceCodeService.getDisplayPrice(amount);
        if (coded !== null) return coded;
        return this.formatCurrency(amount);
    }

    /**
     * Format status label
     */
    formatStatus(isActive: boolean): string {
        return isActive ? 'Active' : 'Inactive';
    }
}
