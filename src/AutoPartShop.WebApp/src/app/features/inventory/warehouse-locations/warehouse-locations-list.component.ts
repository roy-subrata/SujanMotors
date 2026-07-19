import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MenuModule, Menu } from 'primeng/menu';
import { DialogService } from 'primeng/dynamicdialog';

import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';

import { WarehouseLocationService, WarehouseLocationResponse } from '../services/warehouse-location.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { CategoryService, CategoryResponse } from '../services/category.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { getZoneColor } from './zone-color';
import { LocationLabelDialogComponent } from './location-label-dialog/location-label-dialog.component';

/**
 * Warehouse Locations list — standalone physical bin/shelf slots
 * (Zone-Aisle-Rack-Bin), independent of any product. See
 * `WarehouseLocationService` for the backend contract. Not to be confused
 * with the per-product `ProductLocation` (Section/Shelf) feature.
 */
@Component({
    selector: 'app-warehouse-locations-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        Select,
        TooltipModule,
        ToastModule,
        ConfirmDialogModule,
        MenuModule,
        PageContainerComponent,
        PageHeaderComponent,
        FilterBarComponent,
        DataPaginationComponent
    ],
    providers: [MessageService, ConfirmationService, DialogService],
    templateUrl: './warehouse-locations-list.component.html',
    styleUrls: ['./warehouse-locations-list.component.css']
})
export class WarehouseLocationsListComponent implements OnInit {
    private readonly locationService = inject(WarehouseLocationService);
    private readonly warehouseService = inject(WarehouseService);
    private readonly categoryService = inject(CategoryService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly dialogService = inject(DialogService);

    @ViewChild('actionMenu') actionMenu!: Menu;

    locations: WarehouseLocationResponse[] = [];
    selectedLocation: WarehouseLocationResponse | null = null;
    loading = false;

    totalRecords = 0;
    pageNumber = 1;
    pageSize = 20;
    first = 0;
    pageSizeOptions = [10, 20, 50, 100];

    searchTerm = '';
    filterWarehouseId: string | null = null;
    filterCategoryId: string | null = null;

    warehouseOptions: { label: string; value: string }[] = [];
    categoryOptions: { label: string; value: string }[] = [];

    actionMenuItems: MenuItem[] = [];

    ngOnInit(): void {
        this.loadFilterOptions();
        this.loadData();
    }

    private loadFilterOptions(): void {
        this.warehouseService.getAllWarehouses().subscribe({
            next: (warehouses: WarehouseResponse[]) => {
                this.warehouseOptions = warehouses.map((w) => ({ label: w.name, value: w.id }));
            },
            error: () => { /* filter picker is a nicety — table still works without it */ }
        });

        this.categoryService.getAllCategories().subscribe({
            next: (categories: CategoryResponse[]) => {
                this.categoryOptions = categories.map((c) => ({ label: c.name, value: c.id }));
            },
            error: () => { /* filter picker is a nicety — table still works without it */ }
        });
    }

    loadData(): void {
        this.loading = true;
        this.locationService
            .getList({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                search: this.searchTerm || undefined,
                warehouseId: this.filterWarehouseId,
                categoryId: this.filterCategoryId
            })
            .subscribe({
                next: (response) => {
                    this.locations = response.data;
                    this.totalRecords = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Error loading warehouse locations:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load warehouse locations'
                    });
                    this.loading = false;
                }
            });
    }

    onLazyLoad(event: TableLazyLoadEvent): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? this.pageSize;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    onPageChange(event: { first: number; rows: number }): void {
        this.first = event.first;
        this.pageSize = event.rows;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    hasActiveFilters(): boolean {
        return !!(this.searchTerm || this.filterWarehouseId || this.filterCategoryId);
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
        this.filterWarehouseId = null;
        this.filterCategoryId = null;
        this.resetPagination();
        this.loadData();
    }

    private resetPagination(): void {
        this.pageNumber = 1;
        this.first = 0;
    }

    refreshData(): void {
        this.loadData();
    }

    getWarehouseLabel(id: string | null): string {
        return this.warehouseOptions.find((w) => w.value === id)?.label ?? '';
    }

    getCategoryLabel(id: string | null): string {
        return this.categoryOptions.find((c) => c.value === id)?.label ?? '';
    }

    zoneColor(zone: string): string {
        return getZoneColor(zone);
    }

    createLocation(): void {
        this.router.navigate(['/inventory/warehouse-locations/create']);
    }

    editLocation(location: WarehouseLocationResponse): void {
        this.router.navigate(['/inventory/warehouse-locations/edit'], { queryParams: { id: location.id } });
    }

    /** Opens the print dialog for this location's barcode/QR label. */
    printLabel(location: WarehouseLocationResponse): void {
        this.dialogService.open(LocationLabelDialogComponent, {
            data: { location },
            header: 'Print Location Label',
            width: '760px',
            modal: true,
            closable: true
        });
    }

    deleteLocation(location: WarehouseLocationResponse): void {
        this.confirmationService.confirm({
            message: `Delete location "${location.locationCode}"? This cannot be undone.`,
            header: 'Confirm Deletion',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.locationService.delete(location.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Deleted',
                            detail: `Location "${location.locationCode}" deleted`
                        });
                        // If we deleted the last item on a page beyond page 1, go back a page.
                        const isLastItemOnPage = this.locations.length === 1 && this.pageNumber > 1;
                        if (isLastItemOnPage) {
                            this.pageNumber -= 1;
                            this.first = Math.max(0, this.first - this.pageSize);
                        }
                        this.loadData();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message ?? 'Failed to delete location'
                        });
                    }
                });
            }
        });
    }

    private buildActionMenuItems(location: WarehouseLocationResponse): void {
        this.actionMenuItems = [
            { label: 'Edit', icon: 'pi pi-pencil', command: () => this.editLocation(location) },
            { label: 'Print Label', icon: 'pi pi-print', command: () => this.printLabel(location) },
            { separator: true },
            { label: 'Delete', icon: 'pi pi-trash', command: () => this.deleteLocation(location), styleClass: 'text-red-600' }
        ];
    }

    showActionMenu(event: Event, location: WarehouseLocationResponse): void {
        this.selectedLocation = location;
        this.buildActionMenuItems(location);
        this.actionMenu.toggle(event);
    }
}
