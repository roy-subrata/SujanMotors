import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../services/catalog.service';
import { CartService } from '../services/cart.service';
import { ProductCardComponent } from '../components/product-card/product-card.component';
import { CatalogProductDetail, CatalogProductListItem, CatalogVariant } from '../models/catalog.model';

@Component({
  selector: 'app-ecommerce-product',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProductCardComponent],
  templateUrl: './ecommerce-product.component.html',
  styleUrls: ['./ecommerce-product.component.css'],
})
export class EcommerceProductComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly cartService = inject(CartService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  product?: CatalogProductDetail;
  selectedVariant?: CatalogVariant;
  loading = true;
  quantity = 1;
  activeTab: 'description' | 'specs' | 'reviews' = 'description';
  addedToCart = false;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const partId = params.get('partId');
      if (partId) {
        this.loading = true;
        this.quantity = 1;
        this.activeTab = 'description';
        this.addedToCart = false;
        this.catalogService.getProductDetail(partId).subscribe({
          next: data => {
            this.product = data;
            this.selectedVariant = data.variants[0];
            this.loading = false;
          },
          error: () => (this.loading = false),
        });
      }
    });
  }

  selectVariant(variant: CatalogVariant): void {
    this.selectedVariant = variant;
  }

  get currentPrice(): number {
    return this.selectedVariant?.price ?? 0;
  }

  get currentCurrency(): string {
    return this.selectedVariant?.currency ?? 'BDT';
  }

  get showSale(): boolean {
    return !!this.product?.isOnSale && !!this.product?.originalPrice;
  }

  addToCart(): void {
    if (!this.product || !this.selectedVariant) return;
    this.cartService.addItem({
      partId: this.product.partId,
      variantId: this.selectedVariant.variantId,
      name: this.product.name,
      variantName: this.selectedVariant.name,
      categoryName: this.product.categoryName,
      price: this.selectedVariant.price,
      currency: this.selectedVariant.currency,
      quantity: this.quantity,
      primaryImageUrl: this.product.primaryImageUrl,
      inStock: this.selectedVariant.inStock,
    });
    this.addedToCart = true;
    setTimeout(() => (this.addedToCart = false), 2000);
  }

  buyNow(): void {
    this.addToCart();
    this.router.navigate(['/shop/checkout']);
  }

  onRelatedAddToCart(item: CatalogProductListItem): void {
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
}
