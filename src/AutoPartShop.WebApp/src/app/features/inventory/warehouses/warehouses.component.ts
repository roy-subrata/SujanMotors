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

@Component({
  selector: 'app-warehouses',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToastModule,
    ConfirmDialogModule,
    WarehousesListComponent
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
  loadWarehouses(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): void {
    if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
      pageNumber = 1;
    }
    if (!pageSize || isNaN(pageSize) || pageSize < 1) {
      pageSize = 10;
    }

    this.loading = true;
    this.warehouseService.getAllWarehouses().subscribe({
      next: (warehouses: WarehouseResponse[]) => {
        // Apply search filter if provided
        let filtered = warehouses;
        if (searchTerm) {
          const lowerSearch = searchTerm.toLowerCase();
          filtered = warehouses.filter(w =>
            w.name.toLowerCase().includes(lowerSearch) ||
            w.location.toLowerCase().includes(lowerSearch)
          );
        }

        this.totalRecords = filtered.length;

        // Apply pagination
        const startIndex = (pageNumber - 1) * pageSize;
        this.warehouses = filtered.slice(startIndex, startIndex + pageSize);
        this.rows = pageSize;
        this.currentPage = pageNumber;
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
  onSearch(query: string): void {
    this.searchTerm = query;
    this.loadWarehouses(1, this.rows, query);
  }

  /**
   * Handle search clear
   */
  onSearchClear(): void {
    this.searchTerm = '';
    this.loadWarehouses(1, this.rows);
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
