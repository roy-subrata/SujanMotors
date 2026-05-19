import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Subject, takeUntil } from 'rxjs';
import { CatalogService } from '../services/catalog.service';
import { CartService } from '../services/cart.service';
import { ShopPoliciesService } from '../services/shop-policies.service';
import { ProductCardComponent } from '../components/product-card/product-card.component';
import {
  CatalogMedia,
  CatalogProductDetail,
  CatalogVariant,
  ShopPolicies,
  DEFAULT_SHOP_POLICIES,
} from '../models/catalog.model';

@Component({
  selector: 'app-ecommerce-product',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProductCardComponent],
  templateUrl: './ecommerce-product.component.html',
  styleUrls: ['./ecommerce-product.component.css'],
})
export class EcommerceProductComponent implements OnInit, OnDestroy {
  private readonly catalogService = inject(CatalogService);
  private readonly cartService = inject(CartService);
  private readonly shopPoliciesService = inject(ShopPoliciesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroy$ = new Subject<void>();

  product?: CatalogProductDetail;
  notFound = false;
  selectedVariant?: CatalogVariant;
  loading = true;
  quantity = 1;
  activeTab: 'description' | 'specs' | 'reviews' = 'description';
  addedToCart = false;
  outOfStock = false;
  policies: ShopPolicies = DEFAULT_SHOP_POLICIES;

  activeMediaIndex = 0;

  // Demo media shown when the product has no media in the database yet.
  // Remove this once the backend is populating ProductMedia rows.
  private readonly demoMedia: CatalogMedia[] = [
    { url: 'https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?w=600&q=80', mediaType: 'image', sortOrder: 0, isPrimary: true },
    { url: 'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&q=80', mediaType: 'image', sortOrder: 1, isPrimary: false },
    { url: 'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=600&q=80', mediaType: 'image', sortOrder: 2, isPrimary: false },
    { url: 'https://www.youtube.com/watch?v=ysz5S6PUM-U', mediaType: 'video', sortOrder: 3, isPrimary: false },
  ];

  get mediaToShow(): CatalogMedia[] {
    const m = this.product?.media ?? [];
    return m.length > 0 ? m : this.demoMedia;
  }

  ngOnInit(): void {
    this.shopPoliciesService.get().pipe(takeUntil(this.destroy$)).subscribe(p => (this.policies = p));

    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const partId = params.get('partId');
      if (!partId) return;

      this.loading = true;
      this.notFound = false;
      this.product = undefined;
      this.quantity = 1;
      this.activeTab = 'description';
      this.addedToCart = false;
      this.activeMediaIndex = 0;

      this.catalogService.getProductDetail(partId).subscribe({
        next: data => {
          if (data) {
            this.product = data;
            this.selectedVariant = data.variants?.[0];
            // Start on whichever item is flagged primary (works for real + demo media)
            const primaryIdx = this.mediaToShow.findIndex(m => m.isPrimary);
            this.activeMediaIndex = primaryIdx >= 0 ? primaryIdx : 0;
          } else {
            this.notFound = true;
          }
          this.loading = false;
        },
        error: () => {
          this.notFound = true;
          this.loading = false;
        },
      });
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Gallery ───────────────────────────────────────────────────────────────

  get activeMedia(): CatalogMedia | null {
    return this.mediaToShow[this.activeMediaIndex] ?? null;
  }

  setActiveMedia(index: number): void {
    this.activeMediaIndex = index;
  }

  isYouTube(url: string): boolean {
    return url.includes('youtube.com') || url.includes('youtu.be');
  }

  youTubeEmbedUrl(url: string): SafeResourceUrl {
    const id = this.extractYouTubeId(url);
    return this.sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.youtube.com/embed/${id}?rel=0`
    );
  }

  youTubeThumbnail(url: string): string {
    const id = this.extractYouTubeId(url);
    return `https://img.youtube.com/vi/${id}/mqdefault.jpg`;
  }

  private extractYouTubeId(url: string): string {
    const match = url.match(/(?:v=|youtu\.be\/)([A-Za-z0-9_-]{11})/);
    return match?.[1] ?? '';
  }

  // ── Variant / pricing ─────────────────────────────────────────────────────

  selectVariant(variant: CatalogVariant): void {
    this.selectedVariant = variant;
    this.outOfStock = false;
  }

  get currentPrice(): number {
    return this.selectedVariant?.price ?? this.product?.salePrice ?? this.product?.basePrice ?? 0;
  }

  get currentOriginalPrice(): number | null {
    return this.selectedVariant?.originalPrice ?? this.product?.originalPrice ?? null;
  }

  get currentCurrency(): string {
    return this.selectedVariant?.currency ?? this.product?.currency ?? 'BDT';
  }

  get isAddable(): boolean {
    if (this.product?.variants?.length) return !!this.selectedVariant?.inStock;
    return this.product?.inStock ?? false;
  }

  get currentSpecs(): import('../models/catalog.model').CatalogAttributeGroup[] {
    return this.selectedVariant?.specifications?.length
      ? this.selectedVariant.specifications
      : this.product?.specifications ?? [];
  }

  get warrantyLabel(): string | null {
    if (!this.product?.hasWarranty) return null;
    const months = this.product.warrantyPeriodMonths;
    const type = this.product.warrantyType;
    if (!months) return type ? `${type} warranty` : 'Warranty included';
    const years = months >= 12 && months % 12 === 0 ? `${months / 12} year` : `${months} month`;
    return type ? `${years} ${type.toLowerCase()} warranty` : `${years} warranty`;
  }

  get tagList(): string[] {
    return this.product?.tags?.split(',').map(t => t.trim()).filter(t => t.length > 0) ?? [];
  }

  get showSale(): boolean {
    const isVariantSale = !!this.selectedVariant?.isOnSale && (this.selectedVariant?.originalPrice ?? 0) > 0;
    const isProductSale = !!this.product?.isOnSale && (this.product?.originalPrice ?? 0) > 0;
    return isVariantSale || isProductSale;
  }

  get discountPercent(): number {
    const orig = this.currentOriginalPrice;
    if (!orig || orig <= 0) return 0;
    return Math.round((1 - this.currentPrice / orig) * 100);
  }

  // ── Cart ──────────────────────────────────────────────────────────────────

  addToCart(): void {
    if (!this.product) return;
    this.outOfStock = false;
    this.cartService.addItem(this.buildCartItem()).subscribe({
      next: () => { this.addedToCart = true; setTimeout(() => (this.addedToCart = false), 2000); },
      error: () => { this.outOfStock = true; setTimeout(() => (this.outOfStock = false), 3000); },
    });
  }

  buyNow(): void {
    if (!this.product) return;
    this.cartService.addItem(this.buildCartItem()).subscribe({
      next: () => this.router.navigate(['/shop/checkout']),
      error: () => { this.outOfStock = true; setTimeout(() => (this.outOfStock = false), 3000); },
    });
  }

  private buildCartItem() {
    return {
      partId: this.product!.partId,
      variantId: this.selectedVariant?.variantId,
      name: this.product!.name,
      variantName: this.selectedVariant?.name,
      categoryName: this.product!.categoryName,
      price: this.currentPrice,
      currency: this.currentCurrency,
      quantity: this.quantity,
      primaryImageUrl: this.product!.primaryImageUrl,
      inStock: this.selectedVariant?.inStock ?? this.product!.inStock,
    };
  }
}
