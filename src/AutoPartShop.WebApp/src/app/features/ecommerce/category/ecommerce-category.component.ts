import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CatalogService } from '../services/catalog.service';
import { CartService } from '../services/cart.service';
import { ProductCardComponent } from '../components/product-card/product-card.component';
import { SelectModule } from 'primeng/select';
import { environment } from 'src/environments/environment';
import {
  CatalogCategory,
  CatalogFilter,
  CatalogFilterResponse,
  CatalogProductListItem,
  CatalogSearchRequest,
  VehicleOption,
} from '../models/catalog.model';
import { catchError, debounceTime, of, Subject, takeUntil } from 'rxjs';

type SelectedFilters = Record<string, Set<string>>;
type RangeFilters = Record<string, { min?: number | null; max?: number | null } | undefined>;

@Component({
  selector: 'app-ecommerce-category',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProductCardComponent, SelectModule],
  templateUrl: './ecommerce-category.component.html',
  styleUrls: ['./ecommerce-category.component.css'],
})
export class EcommerceCategoryComponent implements OnInit, OnDestroy {
  private readonly catalogService = inject(CatalogService);
  private readonly cartService = inject(CartService);
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);
  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  categoryId = '';
  categoryName = '';
  filters?: CatalogFilterResponse;
  products: CatalogProductListItem[] = [];
  loading = true;
  categories: CatalogCategory[] = [];
  flatCategories: CatalogCategory[] = [];

  // Search & filter state
  searchTerm = '';
  inStockOnly = false;
  priceMin?: number | null;
  priceMax?: number | null;
  sortBy = 'relevance';
  selectedFilters: SelectedFilters = {};
  rangeFilters: RangeFilters = {};

  // Pagination
  pageNumber = 1;
  pageSize = 12;
  totalCount = 0;
  viewMode: 'grid' | 'list' = 'grid';

  // Vehicle fitment — always visible, loaded dynamically
  vehicles: VehicleOption[] = [];
  vehiclesLoading = true;
  vehicleMake = '';
  vehicleModel = '';
  vehicleYear = '';
  selectedVehicleId: string | null = null;

  get vehicleMakeOptions(): string[] {
    return [...new Set(this.vehicles.map(v => v.make))].sort();
  }

  get vehicleModelOptions(): string[] {
    if (!this.vehicleMake) return [];
    return [...new Set(
      this.vehicles.filter(v => v.make === this.vehicleMake).map(v => v.model)
    )].sort();
  }

  get vehicleYearOptions(): string[] {
    if (!this.vehicleMake || !this.vehicleModel) return [];
    return [...new Set(
      this.vehicles
        .filter(v => v.make === this.vehicleMake && v.model === this.vehicleModel)
        .map(v => String(v.year))
    )].sort((a, b) => Number(b) - Number(a));
  }

  get priceMinPlaceholder(): string {
    const v = this.filters?.priceRange?.min;
    return v != null ? String(v) : 'Min';
  }

  get priceMaxPlaceholder(): string {
    const v = this.filters?.priceRange?.max;
    return v != null ? String(v) : 'Max';
  }

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  get pages(): number[] {
    const total = this.totalPages;
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const start = Math.max(1, Math.min(this.pageNumber - 2, total - 4));
    const end = Math.min(total, start + 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  ngOnInit(): void {
    // Load categories (kept for potential future use / search context)
    this.catalogService.getCategories().subscribe({
      next: data => {
        this.categories = data;
        this.flatCategories = this.buildFlatCategories(data);
      },
    });

    // Auto-search with 300ms debounce when filters change
    this.filterChange$.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => {
      this.pageNumber = 1;
      this.search();
    });

    // Load vehicles for fitment filter — always shown, loading state while fetching
    this.http.get<VehicleOption[]>(`${environment.apiUrl}/v1/vehicles`)
      .pipe(catchError(() => of([])), takeUntil(this.destroy$))
      .subscribe(data => {
        this.vehicles = data;
        this.vehiclesLoading = false;
      });

    // React to route param changes
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.categoryId = params.get('categoryId') ?? '';
      this.resetFilters();
      if (this.categoryId) {
        this.loadFilters();
        this.search();
      }
    });

    // Support ?q= query param for global search
    this.route.queryParamMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const q = params.get('q');
      if (q) {
        this.searchTerm = q;
        this.search();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadFilters(): void {
    this.catalogService.getFilters(this.categoryId, true).subscribe({
      next: data => {
        this.filters = data;
        this.priceMin = data.priceRange?.min ?? null;
        this.priceMax = data.priceRange?.max ?? null;
      },
    });
  }

  onMakeChange(): void {
    this.vehicleModel = '';
    this.vehicleYear = '';
    this.selectedVehicleId = null;
  }

  onModelChange(): void {
    this.vehicleYear = '';
    this.selectedVehicleId = null;
  }

  onYearChange(): void {
    const match = this.vehicles.find(v =>
      v.make === this.vehicleMake &&
      v.model === this.vehicleModel &&
      String(v.year) === this.vehicleYear
    );
    this.selectedVehicleId = match?.id ?? null;
    this.pageNumber = 1;
    this.search();
  }

  toggleOption(filter: CatalogFilter, option: string): void {
    const key = filter.attributeId;
    if (!this.selectedFilters[key]) this.selectedFilters[key] = new Set();
    if (this.selectedFilters[key].has(option)) {
      this.selectedFilters[key].delete(option);
    } else {
      this.selectedFilters[key].add(option);
    }
    this.filterChange$.next(); // auto-search after 300ms
  }

  isOptionSelected(filter: CatalogFilter, option: string): boolean {
    return this.selectedFilters[filter.attributeId]?.has(option) ?? false;
  }

  setRange(filter: CatalogFilter, min?: number | null, max?: number | null): void {
    this.rangeFilters[filter.attributeId] = { min, max };
    this.filterChange$.next();
  }

  // Count of active attribute filters (for badge)
  get activeFilterCount(): number {
    const attrCount = Object.values(this.selectedFilters)
      .reduce((acc, set) => acc + set.size, 0);
    const hasVehicle = this.selectedVehicleId ? 1 : 0;
    const hasPrice = (this.priceMin != null || this.priceMax != null) ? 1 : 0;
    const hasStock = this.inStockOnly ? 1 : 0;
    return attrCount + hasVehicle + hasPrice + hasStock;
  }

  // Active attribute filter chips for display
  get activeFilterChips(): { label: string; key: string; value: string }[] {
    const chips: { label: string; key: string; value: string }[] = [];
    for (const [attrId, values] of Object.entries(this.selectedFilters)) {
      const filter = this.filters?.filters.find(f => f.attributeId === attrId);
      for (const val of values) {
        chips.push({ label: `${filter?.name ?? 'Filter'}: ${val}`, key: attrId, value: val });
      }
    }
    return chips;
  }

  removeChip(key: string, value: string): void {
    this.selectedFilters[key]?.delete(value);
    this.filterChange$.next();
  }

  getRangePlaceholder(filter: CatalogFilter, bound: 'min' | 'max'): string {
    const v = bound === 'min' ? filter.min : filter.max;
    return v != null ? String(v) : bound === 'min' ? 'Min' : 'Max';
  }

  isRangeFilter(filter: CatalogFilter): boolean {
    return filter.filterType === 'range' || filter.min != null || filter.max != null;
  }

  isSelectFilter(filter: CatalogFilter): boolean {
    return !this.isRangeFilter(filter) && (filter.options?.length ?? 0) > 0;
  }

  onInStockChange(): void { this.filterChange$.next(); }
  onSearchTermChange(): void { this.filterChange$.next(); }
  onPriceChange(): void { this.filterChange$.next(); }

  applyFilters(): void {
    this.pageNumber = 1;
    this.search();
  }

  clearFilters(): void {
    this.resetFilters();
    this.search();
  }

  private resetFilters(): void {
    this.searchTerm = '';
    this.inStockOnly = false;
    this.selectedFilters = {};
    this.rangeFilters = {};
    this.sortBy = 'relevance';
    this.vehicleMake = '';
    this.vehicleModel = '';
    this.vehicleYear = '';
    this.selectedVehicleId = null;
    this.pageNumber = 1;
    // Don't reset vehiclesLoading — vehicles are already loaded
  }

  search(): void {
    this.loading = true;

    type AttrFilter = { attributeId: string; values: string[]; min?: number; max?: number };

    // Skip empty Sets — an empty filter sends noise to the backend
    const attributeFilters: AttrFilter[] = Object.entries(this.selectedFilters)
      .filter(([, values]) => values.size > 0)
      .map(([attributeId, values]) => ({ attributeId, values: Array.from(values) }));

    Object.entries(this.rangeFilters).forEach(([attributeId, range]) => {
      if (!range) return;
      const entry = attributeFilters.find(x => x.attributeId === attributeId);
      if (entry) {
        if (range.min != null) entry.min = range.min;
        if (range.max != null) entry.max = range.max;
      } else {
        const f: AttrFilter = { attributeId, values: [] };
        if (range.min != null) f.min = range.min;
        if (range.max != null) f.max = range.max;
        attributeFilters.push(f);
      }
    });

    const request: CatalogSearchRequest = {
      search: this.searchTerm,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      categoryId: this.categoryId || null,
      includeDescendants: true,
      priceMin: this.priceMin ?? null,
      priceMax: this.priceMax ?? null,
      inStockOnly: this.inStockOnly,
      attributeFilters,
      vehicleId: this.selectedVehicleId ?? null,
    };

    this.catalogService.searchProducts(request).subscribe({
      next: data => {
        this.products = this.sortProducts(data.items);
        this.totalCount = data.totalCount;
        if (this.products.length > 0 && !this.categoryName) {
          this.categoryName = this.products[0].categoryName;
        }
        this.loading = false;
      },
      error: () => (this.loading = false),
    });
  }

  sortProducts(items: CatalogProductListItem[]): CatalogProductListItem[] {
    const sorted = [...items];
    switch (this.sortBy) {
      case 'price_asc': return sorted.sort((a, b) => a.price - b.price);
      case 'price_desc': return sorted.sort((a, b) => b.price - a.price);
      case 'name': return sorted.sort((a, b) => a.name.localeCompare(b.name));
      default: return sorted;
    }
  }

  onSortChange(): void {
    this.products = this.sortProducts(this.products);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.search();
    }
  }


  trackById(_: number, item: CatalogProductListItem) { return item.partId; }
  trackByCategory(_: number, item: CatalogCategory) { return item.id; }

  private buildFlatCategories(categories: CatalogCategory[]): CatalogCategory[] {
    const childrenMap = new Map<string | null, CatalogCategory[]>();
    for (const cat of categories) {
      const key = cat.parentCategoryId ?? null;
      const list = childrenMap.get(key) ?? [];
      list.push(cat);
      childrenMap.set(key, list);
    }
    for (const [, list] of childrenMap) list.sort((a, b) => a.displayOrder - b.displayOrder);

    const result: CatalogCategory[] = [];
    const visit = (parentId: string | null, depth: number) => {
      for (const child of childrenMap.get(parentId) ?? []) {
        result.push({ ...child, depthLevel: depth });
        visit(child.id, depth + 1);
      }
    };
    visit(null, 0);
    return result;
  }
}
