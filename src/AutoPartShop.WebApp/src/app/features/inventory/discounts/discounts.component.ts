import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';
import { Select } from 'primeng/select';
import { MessageService, ConfirmationService } from 'primeng/api';
import { DiscountResponse, DiscountService } from '../services/discount.service';
import { DiscountsListComponent } from './discounts-list/discounts-list.component';
import { DiscountFormDialogComponent } from './discount-form-dialog/discount-form-dialog.component';
import { tap } from 'rxjs';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
  selector: 'app-discounts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ToastModule,
    ConfirmDialogModule,
    ButtonModule,
    Select,
    DiscountsListComponent,
    DiscountFormDialogComponent,
    PageContainerComponent,
    PageHeaderComponent
  ],
  providers: [DiscountService, MessageService, ConfirmationService],
  templateUrl: './discounts.component.html',
  styleUrls: ['./discounts.component.css']
})
export class DiscountsComponent implements OnInit {
  private readonly discountService = inject(DiscountService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  // Data
  discounts: DiscountResponse[] = [];
  filteredDiscounts: DiscountResponse[] = [];
  public selectedDiscount: DiscountResponse | null = null;

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
  filterScope: 'VARIANT' | 'PRODUCT' | 'CART' | null = null;
  filterStatus: boolean | null = null;

  // Scope options for dropdown
  scopeOptions = [
    { label: 'All Scopes', value: null },
    { label: 'Variant Level', value: 'VARIANT' },
    { label: 'Product Level', value: 'PRODUCT' },
    { label: 'Cart Level', value: 'CART' }
  ];

  // Status options for dropdown
  statusOptions = [
    { label: 'All Statuses', value: null },
    { label: 'Active', value: true },
    { label: 'Inactive', value: false }
  ];

  ngOnInit(): void {
    this.loadDiscounts();
  }

  /**
   * Load all discounts and apply client-side filters
   */
  loadDiscounts(): void {
    this.loading = true;
    this.discountService.getAllDiscounts().subscribe({
      next: (response: DiscountResponse[]) => {
        this.discounts = response || [];
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load discounts'
        });
        console.error('Error loading discounts:', error);
        this.loading = false;
      }
    });
  }

  /**
   * Apply client-side filters and update pagination
   */
  applyFilters(): void {
    let result = [...this.discounts];

    if (this.searchTerm && this.searchTerm.trim()) {
      const term = this.searchTerm.trim().toLowerCase();
      result = result.filter(d =>
        d.name.toLowerCase().includes(term) ||
        (d.description && d.description.toLowerCase().includes(term)) ||
        (d.promoCode && d.promoCode.toLowerCase().includes(term))
      );
    }

    if (this.filterScope !== null) {
      result = result.filter(d => d.scope === this.filterScope);
    }

    if (this.filterStatus !== null) {
      result = result.filter(d => d.isActive === this.filterStatus);
    }

    this.totalRecords = result.length;
    const start = (this.currentPage - 1) * this.rows;
    this.filteredDiscounts = result.slice(start, start + this.rows);
  }

  /**
   * Handle search button click
   */
  onSearch(): void {
    this.currentPage = 1;
    this.applyFilters();
  }

  /**
   * Handle filter changes
   */
  onFilterChange(): void {
    this.currentPage = 1;
    this.applyFilters();
  }

  /**
   * Clear search input only
   */
  clearSearchInput(): void {
    this.searchTerm = '';
  }

  /**
   * Clear all filters and reload
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.filterScope = null;
    this.filterStatus = null;
    this.currentPage = 1;
    this.applyFilters();
  }

  /**
   * Refresh current data
   */
  refreshData(): void {
    this.loadDiscounts();
  }

  /**
   * Check if any filters are active
   */
  hasActiveFilters(): boolean {
    return !!this.searchTerm || this.filterScope !== null || this.filterStatus !== null;
  }

  /**
   * Status label helper
   */
  getStatusLabel(isActive: boolean | null): string {
    if (isActive === true) return 'Active';
    if (isActive === false) return 'Inactive';
    return 'All';
  }

  /**
   * Scope label helper
   */
  getScopeLabel(scope: 'VARIANT' | 'PRODUCT' | 'CART' | null): string {
    if (scope === 'VARIANT') return 'Variant';
    if (scope === 'PRODUCT') return 'Product';
    if (scope === 'CART') return 'Cart';
    return 'All';
  }

  /**
   * Handle page change from list component
   */
  onPageChange(event: { page: number; rows: number }): void {
    this.currentPage = event.page;
    this.rows = event.rows;
    this.applyFilters();
  }

  /**
   * Trigger create dialog
   */
  createDiscount(): void {
    this.displayCreateDialog = true;
    this.displayUpdateDialog = false;
  }

  /**
   * Handle create success
   */
  onCreateSuccess(): void {
    this.loadDiscounts();
  }

  /**
   * Handle update success
   */
  onUpdateSuccess(): void {
    this.loadDiscounts();
  }

  /**
   * Handle edit discount
   */
  selectAndOpenUpdate(discount: DiscountResponse): void {
    this.selectedDiscount = discount;
    this.displayUpdateDialog = true;
  }

  /**
   * Handle toggle active/inactive
   */
  selectAndToggleActive(discount: DiscountResponse): void {
    const updatedRequest = {
      id: discount.id,
      name: discount.name,
      description: discount.description,
      type: discount.type,
      value: discount.value,
      promoCode: discount.promoCode,
      minimumCartAmount: discount.minimumCartAmount,
      startDate: discount.startDate,
      endDate: discount.endDate,
      isActive: !discount.isActive
    };

    this.discountService.updateDiscount(discount.id, updatedRequest).subscribe({
      next: () => {
        const action = !discount.isActive ? 'activated' : 'deactivated';
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Discount "${discount.name}" ${action} successfully`
        });
        this.loadDiscounts();
      },
      error: (err) => {
        console.error('Failed to toggle discount status', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update discount status'
        });
      }
    });
  }

  /**
   * Handle delete discount
   */
  selectAndDelete(discount: DiscountResponse): void {
    this.selectedDiscount = discount;
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${discount.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.discountService.deleteDiscount(discount.id)
          .pipe(
            tap(() => {
              this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Discount deleted successfully'
              });
              this.selectedDiscount = null;
              this.loadDiscounts();
            })
          )
          .subscribe({
            error: (err) => {
              console.error('Failed to delete discount', err);
              this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to delete discount'
              });
            }
          });
      }
    });
  }

  /**
   * Handle create dialog visibility change
   */
  onDisplayCreateDialogChange(isVisible: boolean): void {
    if (!isVisible) {
      this.displayCreateDialog = false;
    }
  }

  /**
   * Handle update dialog visibility change
   */
  onDisplayUpdateDialogChange(isVisible: boolean): void {
    if (!isVisible) {
      this.displayUpdateDialog = false;
    }
  }
}
