import { Component, OnInit, OnDestroy, inject, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../services/catalog.service';
import { CartService } from '../services/cart.service';
import { CatalogLandingResponse, CatalogProductListItem } from '../models/catalog.model';
import { ProductCardComponent } from '../components/product-card/product-card.component';

@Component({
  selector: 'app-ecommerce-landing',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProductCardComponent],
  templateUrl: './ecommerce-landing.component.html',
  styleUrls: ['./ecommerce-landing.component.css'],
})
export class EcommerceLandingComponent implements OnInit, OnDestroy {
  private readonly catalogService = inject(CatalogService);
  private readonly cartService = inject(CartService);

  @ViewChild('saleTrack') saleTrack?: ElementRef<HTMLDivElement>;
  private bannerTimer?: ReturnType<typeof setInterval>;

  landing?: CatalogLandingResponse;
  loading = true;
  searchTerm = '';
  activeTab: 'featured' | 'popular' | 'latest' = 'featured';
  newsletterEmail = '';
  newsletterSubmitted = false;
  activeBannerIndex = 0;

  readonly categoryIcons: Record<string, string> = {
    'Engine Parts': 'pi-cog',
    'Brake System': 'pi-stop-circle',
    'Suspension': 'pi-car',
    'Electrical': 'pi-bolt',
    'Body Parts': 'pi-shield',
    'Oils & Fluids': 'pi-filter',
    'Filters': 'pi-filter-slash',
    'Tires & Wheels': 'pi-circle',
  };

  readonly brands = ['Bosch', 'Denso', 'NGK', 'Brembo', 'Hella', 'Monroe', 'Mann', 'Castrol', 'Continental', 'ACDelco'];
  readonly banners = [
    {
      title: 'Flash Deals on Brake Systems',
      subtitle: 'Up to 30% off on pads, rotors, and kits.',
      cta: 'Shop Sale',
      route: '/shop/sale',
      theme: 'ember',
      icon: 'pi-bolt',
      image: 'https://picsum.photos/seed/sale-brake/520/360',
    },
    {
      title: 'Engine Performance Week',
      subtitle: 'Premium gaskets, sensors, and pumps.',
      cta: 'Explore Engine Parts',
      route: '/shop/category/cat-1',
      theme: 'steel',
      icon: 'pi-cog',
      image: 'https://picsum.photos/seed/sale-engine/520/360',
    },
    {
      title: 'Electric Essentials',
      subtitle: 'Batteries, lighting, and starters in stock.',
      cta: 'Shop Electrical',
      route: '/shop/category/cat-4',
      theme: 'midnight',
      icon: 'pi-bolt',
      image: 'https://picsum.photos/seed/sale-electric/520/360',
    },
  ];

  get topCategories() {
    return (this.landing?.categories ?? []).filter(c => !c.parentCategoryId);
  }

  get firstCategoryId(): string {
    return this.topCategories.length > 0 ? this.topCategories[0].id : 'cat-1';
  }

  ngOnInit(): void {
    this.catalogService.getLanding().subscribe({
      next: data => {
        this.landing = data;
        this.loading = false;
      },
      error: () => (this.loading = false),
    });

    this.bannerTimer = setInterval(() => {
      this.activeBannerIndex = (this.activeBannerIndex + 1) % this.banners.length;
    }, 6000);
  }

  ngOnDestroy(): void {
    if (this.bannerTimer) clearInterval(this.bannerTimer);
  }

  get tabbedProducts(): CatalogProductListItem[] {
    if (!this.landing) return [];
    switch (this.activeTab) {
      case 'featured': return this.landing.featured;
      case 'popular': return this.landing.popular;
      case 'latest': return this.landing.latest;
    }
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

  getCategoryIcon(name: string): string {
    return this.categoryIcons[name] || 'pi-cog';
  }

  get saleProducts(): CatalogProductListItem[] {
    if (!this.landing) return [];
    return [...this.landing.featured, ...this.landing.popular].slice(0, 10);
  }

  onNewsletterSubmit(): void {
    if (this.newsletterEmail.trim()) {
      this.newsletterSubmitted = true;
    }
  }

  setBanner(index: number): void {
    if (index >= 0 && index < this.banners.length) {
      this.activeBannerIndex = index;
    }
  }

  nextBanner(direction: 1 | -1): void {
    const next = (this.activeBannerIndex + direction + this.banners.length) % this.banners.length;
    this.activeBannerIndex = next;
  }

  scrollSale(direction: 1 | -1): void {
    const track = this.saleTrack?.nativeElement;
    if (!track) return;
    const scrollBy = Math.max(280, track.clientWidth * 0.8);
    track.scrollBy({ left: direction * scrollBy, behavior: 'smooth' });
  }

  trackById(_index: number, item: CatalogProductListItem) {
    return item.partId;
  }
}
