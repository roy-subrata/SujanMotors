import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { SuppliersListComponent } from './suppliers-list/suppliers-list.component';
import { SuppliersFormDialogComponent } from './suppliers-form-dialog/suppliers-form-dialog.component';
import { SupplierService, SupplierResponse } from '../services/supplier.service';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToastModule,
    ConfirmDialogModule,
    SuppliersListComponent,
    SuppliersFormDialogComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './suppliers.component.html',
  styleUrls: ['./suppliers.component.css']
})
export class SuppliersComponent implements OnInit {
  private readonly supplierService = inject(SupplierService);
  private readonly messageService = inject(MessageService);

  suppliers: SupplierResponse[] = [];
  displayCreateDialog = false;
  displayUpdateDialog = false;
  selectedSupplier: SupplierResponse | null = null;
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;
  searchTerm = '';

  constructor() {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  /**
   * Load suppliers from API
   */
  loadSuppliers(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): void {
    // Validate page number
    if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
      pageNumber = 1;
    }
    if (!pageSize || isNaN(pageSize) || pageSize < 1) {
      pageSize = 10;
    }

    this.loading = true;
    this.supplierService.getSuppliers(pageNumber, pageSize, searchTerm).subscribe({
      next: (response) => {
        this.suppliers = response.items;
        this.totalRecords = response.totalCount;
        this.rows = response.pageSize;
        this.currentPage = response.pageNumber;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load suppliers'
        });
        console.error('Error loading suppliers:', error);
        this.loading = false;
      }
    });
  }

  /**
   * Handle create button click
   */
  onCreateClick(): void {
    this.displayCreateDialog = true;
  }

  /**
   * Handle search
   */
  onSearch(query: string): void {
    this.searchTerm = query;
    this.loadSuppliers(1, this.rows, query);
  }

  /**
   * Handle search clear
   */
  onSearchClear(): void {
    this.searchTerm = '';
    this.loadSuppliers(1, this.rows);
  }

  /**
   * Handle edit click
   */
  onEditClick(supplier: SupplierResponse): void {
    this.selectedSupplier = supplier;
    this.displayUpdateDialog = true;
  }

  /**
   * Handle page change
   */
  onPageChange(event: { page: number; rows: number }): void {
    this.loadSuppliers(event.page, event.rows, this.searchTerm);
  }

  /**
   * Handle supplier created
   */
  onSupplierCreated(supplier: SupplierResponse): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: `Supplier '${supplier.name}' created successfully`
    });
    this.loadSuppliers(1, this.rows, this.searchTerm);
  }

  /**
   * Handle supplier updated
   */
  onSupplierUpdated(supplier: SupplierResponse): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: `Supplier '${supplier.name}' updated successfully`
    });
    this.loadSuppliers(this.currentPage, this.rows, this.searchTerm);
  }

  /**
   * Handle supplier deleted
   */
  onSupplierDeleted(): void {
    this.loadSuppliers(this.currentPage, this.rows, this.searchTerm);
  }
}
