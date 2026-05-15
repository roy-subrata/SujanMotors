import { Component, OnInit, OnDestroy, inject, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../services/catalog.service';
import { CatalogLandingResponse, CatalogProductListItem } from '../models/catalog.model';
import { ProductCardComponent } from '../components/product-card/product-card.component';

interface Banner {
  title: string;
  subtitle: string;
  cta: string;
  route: string | string[];
  theme: string;
  icon: string;
  tag: string;
}

@Component({
  selector: 'app-ecommerce-landing',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProductCardComponent],
  templateUrl: './ecommerce-landing.component.html',
  styleUrls: ['./ecommerce-landing.component.css'],
})
export class EcommerceLandingComponent implements OnInit, OnDestroy {
  private readonly catalogService = inject(CatalogService);

  @ViewChild('saleTrack') saleTrack?: ElementRef<HTMLDivElement>;
  private bannerTimer?: ReturnType<typeof setInterval>;

  landing?: CatalogLandingResponse;
  loading = true;
  activeTab: 'featured' | 'popular' | 'latest' = 'featured';
  newsletterEmail = '';
  newsletterSubmitted = false;
  activeBannerIndex = 0;

  banners: Banner[] = [];

  readonly categoryIcons: Record<string, string> = {
    'Engine Parts': 'pi-cog',
    'Brake System': 'pi-stop-circle',
    'Brake Parts': 'pi-stop-circle',
    'Suspension': 'pi-car',
    'Electrical': 'pi-bolt',
    'Body Parts': 'pi-shield',
    'Oils & Fluids': 'pi-filter',
    'Filters': 'pi-filter-slash',
    'Tires & Wheels': 'pi-circle',
  };

  readonly brands = ['Bosch', 'Denso', 'NGK', 'Brembo', 'Hella', 'Monroe', 'Mann', 'Castrol', 'Continental', 'ACDelco'];

  readonly themes = ['primary', 'steel', 'midnight'];

  get topCategories() {
    return (this.landing?.categories ?? []).filter(c => !c.parentCategoryId);
  }

  get firstCategoryId(): string {
    return this.topCategories[0]?.id ?? '';
  }

  ngOnInit(): void {
    this.catalogService.getLanding().subscribe({
      next: data => {
        this.landing = data;
        this.loading = false;
        this.buildBanners(data);
      },
      error: () => (this.loading = false),
    });

    this.bannerTimer = setInterval(() => {
      if (this.banners.length > 0)
        this.activeBannerIndex = (this.activeBannerIndex + 1) % this.banners.length;
    }, 5000);
  }

  ngOnDestroy(): void {
    if (this.bannerTimer) clearInterval(this.bannerTimer);
  }

  private buildBanners(data: CatalogLandingResponse): void {
    const built: Banner[] = [];
    const themes = ['primary', 'steel', 'midnight'];

    // Banner 1: Sale — if there are discounted products
    const saleItems = data.featured.filter(p => p.isOnSale);
    if (saleItems.length > 0) {
      built.push({
        title: 'Flash Sale — Up to 30% Off',
        subtitle: `${saleItems.length} product${saleItems.length > 1 ? 's' : ''} on sale right now. Genuine parts at unbeatable prices.`,
        cta: 'Shop Sale',
        route: '/shop/sale',
        theme: themes[0],
        icon: 'pi-bolt',
        tag: 'Limited Offer',
      });
    }

    // Banners 2–4: top categories from real DB data
    const topCats = data.categories.filter(c => !c.parentCategoryId).slice(0, 3);
    topCats.forEach((cat, i) => {
      built.push({
        title: cat.name,
        subtitle: `Browse our full range of ${cat.name.toLowerCase()} — genuine parts with warranty.`,
        cta: `Shop ${cat.name}`,
        route: ['/shop/category', cat.id],
        theme: themes[(i + 1) % themes.length],
        icon: this.categoryIcons[cat.name]?.replace('pi-', 'pi-') ?? 'pi-box',
        tag: 'In Stock',
      });
    });

    // Banner: New arrivals
    if (data.latest.length > 0) {
      built.push({
        title: 'New Arrivals',
        subtitle: `Fresh stock just landed — ${data.latest.length} new parts added to our catalog.`,
        cta: 'See What\'s New',
        route: '/shop',
        theme: 'midnight',
        icon: 'pi-star',
        tag: 'Just In',
      });
    }

    // Fallback: always show at least one banner
    if (built.length === 0) {
      built.push({
        title: 'Quality Auto Parts',
        subtitle: 'Genuine parts, competitive prices, fast delivery across Bangladesh.',
        cta: 'Browse Products',
        route: '/shop',
        theme: 'primary',
        icon: 'pi-cog',
        tag: 'Premium Store',
      });
    }

    this.banners = built;
  }

  get tabbedProducts(): CatalogProductListItem[] {
    if (!this.landing) return [];
    switch (this.activeTab) {
      case 'featured': return this.landing.featured;
      case 'popular': return this.landing.popular;
      case 'latest': return this.landing.latest;
    }
  }

  get saleProducts(): CatalogProductListItem[] {
    if (!this.landing) return [];
    return [...this.landing.featured, ...this.landing.popular]
      .filter(p => p.isOnSale)
      .slice(0, 10);
  }


  getCategoryIcon(name: string): string {
    return this.categoryIcons[name] || 'pi-cog';
  }

  onNewsletterSubmit(): void {
    if (this.newsletterEmail.trim()) this.newsletterSubmitted = true;
  }

  setBanner(index: number): void {
    if (index >= 0 && index < this.banners.length) this.activeBannerIndex = index;
  }

  nextBanner(direction: 1 | -1): void {
    this.activeBannerIndex = (this.activeBannerIndex + direction + this.banners.length) % this.banners.length;
  }

  scrollSale(direction: 1 | -1): void {
    const track = this.saleTrack?.nativeElement;
    if (!track) return;
    track.scrollBy({ left: direction * Math.max(280, track.clientWidth * 0.8), behavior: 'smooth' });
  }

  trackById(_: number, item: CatalogProductListItem) { return item.partId; }
}
