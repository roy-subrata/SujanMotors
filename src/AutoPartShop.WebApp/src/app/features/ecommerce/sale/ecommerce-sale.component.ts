import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CatalogService } from '../services/catalog.service';
import { CartService } from '../services/cart.service';
import { CatalogProductListItem } from '../models/catalog.model';
import { ProductCardComponent } from '../components/product-card/product-card.component';

@Component({
  selector: 'app-ecommerce-sale',
  standalone: true,
  imports: [CommonModule, RouterModule, ProductCardComponent],
  templateUrl: './ecommerce-sale.component.html',
  styleUrls: ['./ecommerce-sale.component.css'],
})
export class EcommerceSaleComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly cartService = inject(CartService);

  products: CatalogProductListItem[] = [];
  loading = true;

  ngOnInit(): void {
    this.catalogService.getSaleProducts().subscribe({
      next: items => {
        this.products = items;
        this.loading = false;
      },
      error: () => (this.loading = false),
    });
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
}
