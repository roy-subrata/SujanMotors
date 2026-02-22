import { Component, inject, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { CatalogService } from '../../services/catalog.service';
import { CatalogCategory } from '../../models/catalog.model';

@Component({
  selector: 'app-store-header',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './store-header.component.html',
  styleUrls: ['./store-header.component.css'],
})
export class StoreHeaderComponent implements OnInit {
  readonly cartService = inject(CartService);
  private readonly catalogService = inject(CatalogService);
  private readonly router = inject(Router);

  toggleCart = output<void>();

  categories = signal<CatalogCategory[]>([]);
  searchTerm = '';
  mobileMenuOpen = false;

  ngOnInit(): void {
    this.catalogService.getLanding().subscribe(data => {
      this.categories.set(data.categories.filter(c => !c.parentCategoryId));
    });
  }

  onSearch(): void {
    if (this.searchTerm.trim()) {
      this.router.navigate(['/shop/category', this.categories()[0]?.id || 'cat-1'], {
        queryParams: { q: this.searchTerm.trim() },
      });
    }
  }

  onCartClick(): void {
    this.toggleCart.emit();
  }
}
