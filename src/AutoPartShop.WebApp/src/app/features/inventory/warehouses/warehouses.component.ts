import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { WarehousesListComponent } from './warehouses-list.component';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
  selector: 'app-warehouses',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToastModule,
    ConfirmDialogModule,
    WarehousesListComponent,
    PageContainerComponent,
    PageHeaderComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './warehouses.component.html',
  styleUrls: ['./warehouses.component.css']
})
export class WarehousesComponent implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  warehouses: WarehouseResponse[] = [];
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;
  searchTerm = '';

  constructor() {}

  ngOnInit(): void {
    this.loadWarehouses();
  }

  /**
   * Load warehouses from API
   */
  loadWarehouses(pageNumber: number = 1, pageSize: number = 10, search: string = ''): void {
    this.loading = true;
    this.warehouseService
      .getWarehouses({
        search: search ?? '',
        pageNumber: pageNumber ?? 1,
        pageSize: pageSize ?? 10
      })
      .subscribe({
        next: (res) => {
          this.warehouses = res.data || [];
          this.totalRecords = res.pagination?.totalCount || 0;
          this.rows = res.pagination?.pageSize || pageSize || 10;
          this.currentPage = res.pagination?.pageNumber || pageNumber || 1;
          this.loading = false;
        },
        error: (error: any) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load warehouses'
          });
          console.error('Error loading warehouses:', error);
          this.loading = false;
        }
      });
  }

  /**
   * Handle create button click
   */
  onCreateClick(): void {
    this.router.navigate(['/inventory/warehouses/create']);
  }

  /**
   * Handle search
   */
  onSearch(): void {
    this.loadWarehouses(1, this.rows, this.searchTerm);
  }

  /**
   * Handle search clear
   */
  onSearchClear(): void {
    this.searchTerm = '';
    this.loadWarehouses(1, this.rows);
  }

  refreshData(): void {
    this.loadWarehouses(this.currentPage, this.rows, this.searchTerm);
  }

  createWarehouse(): void {
    this.onCreateClick();
  }

  clearFilters(): void {
    this.onSearchClear();
  }

  hasActiveFilters(): boolean {
    return !!this.searchTerm;
  }

  /**
   * Handle view click
   */
  onViewClick(warehouse: WarehouseResponse): void {
    // Navigate to view details page
  }

  /**
   * Handle edit click
   */
  onEditClick(warehouse: WarehouseResponse): void {
    // Navigate to edit page
  }

  /**
   * Handle page change
   */
  onPageChange(event: { page: number; rows: number }): void {
    this.loadWarehouses(event.page, event.rows, this.searchTerm);
  }

  /**
   * Handle warehouse deleted
   */
  onWarehouseDeleted(): void {
    this.loadWarehouses(this.currentPage, this.rows, this.searchTerm);
  }
}
