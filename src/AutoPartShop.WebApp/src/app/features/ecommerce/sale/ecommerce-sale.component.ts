import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../services/catalog.service';
import { CatalogProductListItem } from '../models/catalog.model';
import { ProductCardComponent } from '../components/product-card/product-card.component';

@Component({
  selector: 'app-ecommerce-sale',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProductCardComponent],
  templateUrl: './ecommerce-sale.component.html',
  styleUrls: ['./ecommerce-sale.component.css'],
})
export class EcommerceSaleComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);

  products: CatalogProductListItem[] = [];
  loading = true;

  pageNumber = 1;
  pageSize = 12;
  totalCount = 0;
  sortBy = 'relevance';

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }

  get pages(): number[] {
    const total = this.totalPages;
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const start = Math.max(1, Math.min(this.pageNumber - 2, total - 4));
    const end = Math.min(total, start + 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.catalogService.getSaleProducts(this.pageNumber, this.pageSize).subscribe({
      next: data => {
        this.products = this.sortProducts(data.items);
        this.totalCount = data.totalCount;
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
      this.loadProducts();
    }
  }

  trackById(_index: number, item: CatalogProductListItem) {
    return item.partId;
  }
}
