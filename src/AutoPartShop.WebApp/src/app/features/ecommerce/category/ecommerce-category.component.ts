import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CatalogService } from '../services/catalog.service';
import {
  CatalogFilter,
  CatalogFilterResponse,
  CatalogProductListItem,
  CatalogSearchRequest
} from '../models/catalog.model';

type SelectedFilters = Record<string, Set<string>>;
type RangeFilters = Record<string, { min?: number | null; max?: number | null } | undefined>;

@Component({
  selector: 'app-ecommerce-category',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ButtonModule, InputTextModule],
  templateUrl: './ecommerce-category.component.html',
  styleUrls: ['./ecommerce-category.component.css']
})
export class EcommerceCategoryComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);

  categoryId = '';
  filters?: CatalogFilterResponse;
  products: CatalogProductListItem[] = [];
  loading = true;

  searchTerm = '';
  inStockOnly = false;
  priceMin?: number | null;
  priceMax?: number | null;

  selectedFilters: SelectedFilters = {};
  rangeFilters: RangeFilters = {};

  pageNumber = 1;
  pageSize = 12;
  totalCount = 0;

  ngOnInit(): void {
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
      next: (data) => {
        this.filters = data;
        this.priceMin = data.priceRange?.min ?? null;
        this.priceMax = data.priceRange?.max ?? null;
      }
    });
  }

  toggleOption(filter: CatalogFilter, option: string): void {
    const key = filter.attributeId;
    if (!this.selectedFilters[key]) {
      this.selectedFilters[key] = new Set();
    }
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
      attributeFilters: []
    };

    request.attributeFilters = Object.entries(this.selectedFilters).map(([attributeId, values]) => ({
      attributeId,
      values: Array.from(values)
    }));

    Object.entries(this.rangeFilters).forEach(([attributeId, range]) => {
      if (!range) {
        return;
      }

      const entry = request.attributeFilters.find(x => x.attributeId === attributeId);
      if (entry) {
        entry.min = range.min ?? undefined;
        entry.max = range.max ?? undefined;
      } else {
        request.attributeFilters.push({
          attributeId,
          values: [],
          min: range.min ?? undefined,
          max: range.max ?? undefined
        });
      }
    });

    this.catalogService.searchProducts(request).subscribe({
      next: (data) => {
        this.products = data.items;
        this.totalCount = data.totalCount;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.pageNumber = 1;
    this.search();
  }

  isOptionSelected(filter: CatalogFilter, option: string): boolean {
    return this.selectedFilters[filter.attributeId]?.has(option) ?? false;
  }
}
