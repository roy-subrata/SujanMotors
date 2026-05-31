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
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { TooltipModule } from 'primeng/tooltip';
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
    TooltipModule,
    BrandsListComponent,
    BrandsFormDialogComponent,
    PageContainerComponent,
    PageHeaderComponent,
    FilterBarComponent
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
  selectedBrand: BrandResponse | null = null;

  // Dialog visibility
  displayCreateDialog = false;
  displayUpdateDialog = false;

  // Pagination & loading
  loading = false;
  togglingStatusId: string | null = null;   // prevents double-click on toggle
  totalRecords = 0;
  rows = 10;
  currentPage = 1;

  // Filters
  searchTerm = '';
  filterStatus: boolean | null = null;
  filterCountry = '';

  statusOptions = [
    { label: 'All',      value: null  },
    { label: 'Active',   value: true  },
    { label: 'Inactive', value: false }
  ];

  ngOnInit(): void {
    this.loadBrands();
  }

  loadBrands(page = 1, pageSize = this.rows): void {
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;

    this.loading = true;
    this.brandService.getBrands({
      search: this.searchTerm || undefined,
      isActive: this.filterStatus,
      country: this.filterCountry || undefined,
      page,
      pageSize
    }).subscribe({
      next: (response) => {
        this.brands       = response.data ?? [];
        this.totalRecords = response.pagination.totalCount;
        this.rows         = response.pagination.pageSize;
        this.currentPage  = response.pagination.page;
        this.loading      = false;
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load brands' });
        this.loading = false;
      }
    });
  }

  onSearch(): void        { this.loadBrands(1, this.rows); }
  onFilterChange(): void  { this.loadBrands(1, this.rows); }
  refreshData(): void     { this.loadBrands(this.currentPage, this.rows); }

  clearFilters(): void {
    this.searchTerm    = '';
    this.filterStatus  = null;
    this.filterCountry = '';
    this.loadBrands(1, this.rows);
  }

  hasActiveFilters(): boolean {
    return !!this.searchTerm || this.filterStatus !== null || !!this.filterCountry;
  }

  onPageChange(event: { page: number; rows: number }): void {
    this.loadBrands(event.page, event.rows);
  }

  // ── Dialogs ────────────────────────────────────────────────────────────────

  onNewBrandClick(): void {
    this.displayCreateDialog = true;
    this.displayUpdateDialog = false;
  }

  /** Alias used by the template header button. */
  createBrand(): void { this.onNewBrandClick(); }

  /** Clear search text only — does not reload. */
  clearSearchInput(): void { this.searchTerm = ''; }

  /** Label for status filter chip. */
  getStatusLabel(isActive: boolean | null): string {
    if (isActive === true)  return 'Active';
    if (isActive === false) return 'Inactive';
    return 'All';
  }

  selectAndOpenUpdate(brand: BrandResponse): void {
    this.selectedBrand       = brand;
    this.displayUpdateDialog = true;
  }

  toggleBrandStatus(brand: BrandResponse): void {
    if (this.togglingStatusId === brand.id) return; // guard against double-click
    this.togglingStatusId = brand.id;

    this.brandService.updateBrand(brand.id, {
      name: brand.name,
      code: brand.code,
      description: brand.description,
      logoUrl: brand.logoUrl,
      website: brand.website,
      country: brand.country,
      contactEmail: brand.contactEmail,
      contactPhone: brand.contactPhone,
      displayOrder: brand.displayOrder,
      isActive: !brand.isActive
    }).subscribe({
      next: () => {
        const action = brand.isActive ? 'deactivated' : 'activated';
        this.messageService.add({ severity: 'success', summary: 'Updated', detail: `"${brand.name}" ${action}` });
        this.togglingStatusId = null;
        this.loadBrands(this.currentPage, this.rows);
      },
      error: () => {
        this.togglingStatusId = null;
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to update brand status' });
      }
    });
  }

  onCreateSuccess(): void { this.loadBrands(this.currentPage, this.rows); }
  onUpdateSuccess(): void { this.loadBrands(this.currentPage, this.rows); }

  onDisplayCreateDialogChange(isVisible: boolean): void {
    if (!isVisible) this.displayCreateDialog = false;
  }
  onDisplayUpdateDialogChange(isVisible: boolean): void {
    if (!isVisible) this.displayUpdateDialog = false;
  }

  // ── Delete ─────────────────────────────────────────────────────────────────

  selectAndDelete(brand: BrandResponse): void {
    this.selectedBrand = brand;
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${brand.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.brandService.deleteBrand(brand.id)
          .pipe(tap(() => {
            this.messageService.add({ severity: 'success', summary: 'Deleted', detail: `"${brand.name}" deleted` });
            this.selectedBrand = null;
            // If we deleted the last item on a page beyond page 1, go back a page
            const isLastItemOnPage = this.brands.length === 1 && this.currentPage > 1;
            this.loadBrands(isLastItemOnPage ? this.currentPage - 1 : this.currentPage, this.rows);
          }))
          .subscribe({
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete brand' })
          });
      }
    });
  }
}
