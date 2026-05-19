import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';
import { Select } from 'primeng/select';
import { MessageService, ConfirmationService } from 'primeng/api';
import { BrandResponse, BrandService } from '../services/brand.service';
import { BrandsListComponent } from './brands-list/brands-list.component';
import { BrandsFormDialogComponent } from './brands-form-dialog/brands-form-dialog.component';
import { tap } from 'rxjs';

@Component({
  selector: 'app-brands',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ToastModule,
    ConfirmDialogModule,
    ButtonModule,
    Select,
    BrandsListComponent,
    BrandsFormDialogComponent
  ],
  providers: [BrandService, MessageService, ConfirmationService],
  templateUrl: './brands.component.html',
  styleUrls: ['./brands.component.css']
})
export class BrandsComponent implements OnInit {
  private readonly brandService = inject(BrandService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  // Data
  brands: BrandResponse[] = [];
  public selectedBrand: BrandResponse | null = null;

  // Dialog visibility
  public displayCreateDialog: boolean = false;
  public displayUpdateDialog: boolean = false;

  // Pagination & Loading
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;

  // Filters
  searchTerm = '';
  filterStatus: boolean | null = null;
  sortField = 'name';
  sortDirection: 'asc' | 'desc' = 'asc';

  // Status options for dropdown
  statusOptions = [
    { label: 'All', value: null },
    { label: 'Active', value: true },
    { label: 'Inactive', value: false }
  ];

  ngOnInit(): void {
    this.loadBrands();
  }

  /**
   * Load brands with current filters
   */
  loadBrands(pageNumber: number = 1, pageSize: number = 10): void {
    if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
      pageNumber = 1;
    }
    if (!pageSize || isNaN(pageSize) || pageSize < 1) {
      pageSize = 10;
    }

    this.loading = true;
    this.brandService
      .getBrands({
        search: this.searchTerm,
        pageNumber: pageNumber,
        pageSize: pageSize,
        isActive: this.filterStatus,
        sorts: [{
          field: this.sortField,
          direction: this.sortDirection
        }]
      })
      .subscribe({
        next: (response: any) => {
          // Extract data from response
          this.brands = response.data || response.items || [];

          // totalCount is nested in pagination object
          const pagination = response.pagination || {};
          this.totalRecords = pagination.totalCount || response.totalCount || this.brands.length;

          // Update pagination state
          this.rows = pagination.pageSize || pageSize;
          this.currentPage = pagination.pageNumber || pageNumber;

          this.loading = false;
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load brands'
          });
          console.error('Error loading brands:', error);
          this.loading = false;
        }
      });
  }

  /**
   * Handle search button click - applies all filters
   */
  onSearch(): void {
    this.loadBrands(1, this.rows);
  }

  /**
   * Handle filter changes (status)
   */
  onFilterChange(): void {
    this.loadBrands(1, this.rows);
  }

  /**
   * Clear search input only (does not trigger search)
   */
  clearSearchInput(): void {
    this.searchTerm = '';
  }

  /**
   * Clear all filters and reload
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.filterStatus = null;
    this.loadBrands(1, this.rows);
  }

  /**
   * Refresh current page
   */
  refreshData(): void {
    this.loadBrands(this.currentPage, this.rows);
  }

  /**
   * Check if any filters are active
   */
  hasActiveFilters(): boolean {
    return !!this.searchTerm || this.filterStatus !== null;
  }

  /**
   * Status label helper for filter chips
   */
  getStatusLabel(isActive: boolean | null): string {
    if (isActive === true) {
      return 'Active';
    }
    if (isActive === false) {
      return 'Inactive';
    }
    return 'All';
  }

  /**
   * Handle page change from list component
   */
  onPageChange(event: { page: number; rows: number }): void {
    this.loadBrands(event.page, event.rows);
  }

  /**
   * Trigger create dialog
   */
  onNewBrandClick() {
    this.displayCreateDialog = true;
    this.displayUpdateDialog = false;
  }

  /**
   * Alias for header action button
   */
  createBrand(): void {
    this.onNewBrandClick();
  }

  /**
   * Handle create success
   */
  onCreateSuccess() {
    this.loadBrands(this.currentPage, this.rows);
  }

  /**
   * Handle update success
   */
  onUpdateSuccess() {
    this.loadBrands(this.currentPage, this.rows);
  }

  /**
   * Handle edit brand
   */
  selectAndOpenUpdate(brand: BrandResponse) {
    this.selectedBrand = brand;
    this.displayUpdateDialog = true;
  }

  /**
   * Handle delete brand
   */
  selectAndDelete(brand: BrandResponse) {
    this.selectedBrand = brand;
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${brand.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.brandService.deleteBrand(brand.id)
          .pipe(
            tap(() => {
              this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Brand deleted successfully'
              });
              this.selectedBrand = null;
              this.loadBrands(this.currentPage, this.rows);
            })
          )
          .subscribe({
            error: (err) => {
              console.error('Failed to delete brand', err);
              this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to delete brand'
              });
            }
          });
      }
    });
  }

  /**
   * Handle create dialog visibility change
   */
  onDisplayCreateDialogChange(isVisible: boolean) {
    if (!isVisible) {
      this.displayCreateDialog = false;
    }
  }

  /**
   * Handle update dialog visibility change
   */
  onDisplayUpdateDialogChange(isVisible: boolean) {
    if (!isVisible) {
      this.displayUpdateDialog = false;
    }
  }
}
