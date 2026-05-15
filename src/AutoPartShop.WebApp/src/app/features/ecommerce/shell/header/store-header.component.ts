import { Component, inject, OnInit, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { CatalogService } from '../../services/catalog.service';
import { CustomerAuthService } from '../../services/customer-auth.service';
import { AuthService } from '../../../../shared/services/auth.service';
import { CatalogCategory } from '../../models/catalog.model';

export interface NavCategory {
  id: string;
  name: string;
  children: CatalogCategory[];
}

const MAX_VISIBLE = 7;

@Component({
  selector: 'app-store-header',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './store-header.component.html',
  styleUrls: ['./store-header.component.css'],
})
export class StoreHeaderComponent implements OnInit {
  readonly cartService = inject(CartService);
  readonly customerAuthService = inject(CustomerAuthService);
  readonly staffAuthService = inject(AuthService);
  private readonly catalogService = inject(CatalogService);
  private readonly router = inject(Router);

  // Keep for backward compat (search fallback)
  categories = signal<CatalogCategory[]>([]);

  // Mega menu data
  navCategories = signal<NavCategory[]>([]);
  readonly visibleCats = computed(() => this.navCategories().slice(0, MAX_VISIBLE));
  readonly overflowCats = computed(() => this.navCategories().slice(MAX_VISIBLE));

  // Mobile accordion & More dropdown state
  mobileMenuOpen = false;
  activeMenuId = signal<string | null>(null);
  moreOpen = signal(false);

  searchTerm = '';

  ngOnInit(): void {
    this.catalogService.getLanding().subscribe({
      next: data => {
        const all = data.categories;

        // Build parent → children map
        const childMap = new Map<string, CatalogCategory[]>();
        for (const cat of all) {
          if (!cat.parentCategoryId) continue;
          const list = childMap.get(cat.parentCategoryId) ?? [];
          list.push(cat);
          childMap.set(cat.parentCategoryId, list);
        }

        const roots = all
          .filter(c => !c.parentCategoryId)
          .sort((a, b) => a.displayOrder - b.displayOrder);

        this.navCategories.set(roots.map(r => ({
          id: r.id,
          name: r.name,
          children: (childMap.get(r.id) ?? []).sort((a, b) => a.displayOrder - b.displayOrder),
        })));

        // backward compat
        this.categories.set(roots.slice(0, 8));
      },
    });
  }

  // Toggle category sub-menu (mobile only — desktop uses CSS :hover)
  toggleMenu(id: string, event: Event): void {
    event.preventDefault();
    this.activeMenuId.set(this.activeMenuId() === id ? null : id);
    this.moreOpen.set(false);
  }

  toggleMore(event: Event): void {
    event.preventDefault();
    this.moreOpen.set(!this.moreOpen());
    this.activeMenuId.set(null);
  }

  closeAll(): void {
    this.activeMenuId.set(null);
    this.moreOpen.set(false);
  }

  // Close dropdowns when clicking outside (desktop)
  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const target = e.target as HTMLElement;
    if (!target.closest('.nav-item') && !target.closest('.more-item')) {
      this.activeMenuId.set(null);
      this.moreOpen.set(false);
    }
  }

  onSearch(): void {
    const term = this.searchTerm.trim();
    if (!term) return;
    const firstCatId = this.categories()[0]?.id;
    if (firstCatId) {
      this.router.navigate(['/shop/category', firstCatId], { queryParams: { q: term } });
    } else {
      this.router.navigate(['/shop/search'], { queryParams: { q: term } });
    }
    this.searchTerm = '';
    this.mobileMenuOpen = false;
  }

  onCartClick(): void {
    this.cartService.sidebarOpen.set(true);
  }

  onNavLink(): void {
    this.mobileMenuOpen = false;
    this.closeAll();
  }
}
