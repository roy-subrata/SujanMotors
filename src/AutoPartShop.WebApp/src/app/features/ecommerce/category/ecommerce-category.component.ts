import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../services/catalog.service';
import { CartService } from '../services/cart.service';
import { ProductCardComponent } from '../components/product-card/product-card.component';
import { SelectModule } from 'primeng/select';
import {
  CatalogCategory,
  CatalogFilter,
  CatalogFilterResponse,
  CatalogProductListItem,
  CatalogSearchRequest,
} from '../models/catalog.model';

type SelectedFilters = Record<string, Set<string>>;
type RangeFilters = Record<string, { min?: number | null; max?: number | null } | undefined>;

@Component({
  selector: 'app-ecommerce-category',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProductCardComponent, SelectModule],
  templateUrl: './ecommerce-category.component.html',
  styleUrls: ['./ecommerce-category.component.css'],
})
export class EcommerceCategoryComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly cartService = inject(CartService);
  private readonly route = inject(ActivatedRoute);

  categoryId = '';
  categoryName = '';
  filters?: CatalogFilterResponse;
  products: CatalogProductListItem[] = [];
  loading = true;
  categories: CatalogCategory[] = [];
  flatCategories: CatalogCategory[] = [];

  searchTerm = '';
  inStockOnly = false;
  priceMin?: number | null;
  priceMax?: number | null;
  sortBy = 'relevance';
  vehicleMake = '';
  vehicleModel = '';
  vehicleYear = '';
  vehicleBodyStyle = '';

  readonly vehicleMakes = ['Toyota', 'Honda', 'Nissan', 'Mitsubishi', 'Suzuki', 'Hyundai'];
  readonly vehicleModels: Record<string, string[]> = {
    Toyota: ['Corolla', 'Camry', 'Hilux', 'Prado'],
    Honda: ['Civic', 'Accord', 'CR-V'],
    Nissan: ['Sunny', 'X-Trail', 'Navara'],
    Mitsubishi: ['Lancer', 'Pajero', 'Triton'],
    Suzuki: ['Swift', 'Alto', 'Wagon R'],
    Hyundai: ['Elantra', 'Tucson', 'Santa Fe'],
  };
  readonly vehicleYears = ['2024', '2023', '2022', '2021', '2020', '2019', '2018'];
  readonly bodyStyles = ['Sedan', 'SUV', 'Hatchback', 'Pickup', 'Coupe', 'Van'];

  selectedFilters: SelectedFilters = {};
  rangeFilters: RangeFilters = {};

  pageNumber = 1;
  pageSize = 12;
  totalCount = 0;
  viewMode: 'grid' | 'list' = 'grid';

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get pages(): number[] {
    const total = this.totalPages;
    const pages: number[] = [];
    for (let i = 1; i <= total && i <= 5; i++) pages.push(i);
    return pages;
  }

  ngOnInit(): void {
    this.catalogService.getCategories().subscribe({
      next: data => {
        this.categories = data;
        this.flatCategories = this.buildFlatCategories(data);
      },
    });

    this.route.paramMap.subscribe(params => {
      this.categoryId = params.get('categoryId') ?? '';
      if (this.categoryId) {
        this.loadFilters();
        this.search();
      }
    });
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

  toggleOption(filter: CatalogFilter, option: string): void {
    const key = filter.attributeId;
    if (!this.selectedFilters[key]) this.selectedFilters[key] = new Set();
    if (this.selectedFilters[key].has(option)) {
      this.selectedFilters[key].delete(option);
    } else {
      this.selectedFilters[key].add(option);
    }
  }

  setRange(filter: CatalogFilter, min?: number | null, max?: number | null): void {
    this.rangeFilters[filter.attributeId] = { min, max };
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.inStockOnly = false;
    this.selectedFilters = {};
    this.rangeFilters = {};
    this.sortBy = 'relevance';
    this.vehicleMake = '';
    this.vehicleModel = '';
    this.vehicleYear = '';
    this.vehicleBodyStyle = '';
    this.pageNumber = 1;
    this.search();
  }

  search(): void {
    this.loading = true;
    const request: CatalogSearchRequest = {
      search: this.searchTerm,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      categoryId: this.categoryId,
      includeDescendants: true,
      priceMin: this.priceMin ?? undefined,
      priceMax: this.priceMax ?? undefined,
      inStockOnly: this.inStockOnly,
      attributeFilters: [],
    };

    request.attributeFilters = Object.entries(this.selectedFilters).map(([attributeId, values]) => ({
      attributeId,
      values: Array.from(values),
    }));

    if (this.vehicleMake) {
      request.attributeFilters.push({ attributeId: 'vehicle_make', values: [this.vehicleMake] });
    }
    if (this.vehicleModel) {
      request.attributeFilters.push({ attributeId: 'vehicle_model', values: [this.vehicleModel] });
    }
    if (this.vehicleYear) {
      request.attributeFilters.push({ attributeId: 'vehicle_year', values: [this.vehicleYear] });
    }
    if (this.vehicleBodyStyle) {
      request.attributeFilters.push({ attributeId: 'vehicle_body', values: [this.vehicleBodyStyle] });
    }

    Object.entries(this.rangeFilters).forEach(([attributeId, range]) => {
      if (!range) return;
      const entry = request.attributeFilters.find(x => x.attributeId === attributeId);
      if (entry) {
        entry.min = range.min ?? undefined;
        entry.max = range.max ?? undefined;
      } else {
        request.attributeFilters.push({ attributeId, values: [], min: range.min ?? undefined, max: range.max ?? undefined });
      }
    });

    this.catalogService.searchProducts(request).subscribe({
      next: data => {
        this.products = this.sortProducts(data.items);
        this.totalCount = data.totalCount;
        if (this.products.length > 0) {
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

  applyFilters(): void {
    this.pageNumber = 1;
    this.search();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.search();
    }
  }

  isOptionSelected(filter: CatalogFilter, option: string): boolean {
    return this.selectedFilters[filter.attributeId]?.has(option) ?? false;
  }

  onAddToCart(item: CatalogProductListItem): void {
    this.cartService.addItem({
      partId: item.partId,
      variantId: item.variantId,
      name: item.name,
      categoryName: item.categoryName,
      price: item.price,
      currency: item.currency,
      quantity: 1,
      primaryImageUrl: item.primaryImageUrl,
      inStock: item.inStock,
    });
  }

  trackById(_index: number, item: CatalogProductListItem) {
    return item.partId;
  }

  trackByCategory(_index: number, item: CatalogCategory) {
    return item.id;
  }

  get modelsForMake(): string[] {
    return this.vehicleMake ? this.vehicleModels[this.vehicleMake] ?? [] : [];
  }

  private buildFlatCategories(categories: CatalogCategory[]): CatalogCategory[] {
    const childrenMap = new Map<string | null, CatalogCategory[]>();
    for (const cat of categories) {
      const parentKey = cat.parentCategoryId ?? null;
      const list = childrenMap.get(parentKey) ?? [];
      list.push(cat);
      childrenMap.set(parentKey, list);
    }

    for (const [, list] of childrenMap) {
      list.sort((a, b) => a.displayOrder - b.displayOrder);
    }

    const result: CatalogCategory[] = [];
    const visit = (parentId: string | null, depth: number) => {
      const children = childrenMap.get(parentId) ?? [];
      for (const child of children) {
        result.push({ ...child, depthLevel: depth });
        visit(child.id, depth + 1);
      }
    };

    visit(null, 0);
    return result;
  }
}
