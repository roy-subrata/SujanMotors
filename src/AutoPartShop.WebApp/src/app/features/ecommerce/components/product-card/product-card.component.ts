import { Component, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { inject } from '@angular/core';
import { CartService } from '../../services/cart.service';
import { CatalogProductListItem } from '../../models/catalog.model';

type BtnState = 'idle' | 'loading' | 'added' | 'error';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './product-card.component.html',
  styleUrls: ['./product-card.component.css'],
})
export class ProductCardComponent {
  private readonly cartService = inject(CartService);

  product = input.required<CatalogProductListItem>();

  btnState = signal<BtnState>('idle');

  onAddToCart(e: Event): void {
    e.preventDefault();
    e.stopPropagation();

    if (!this.product().inStock || this.btnState() === 'loading') return;

    this.btnState.set('loading');

    this.cartService.addItem({
      partId: this.product().partId,
      variantId: this.product().variantId,
      name: this.product().name,
      categoryName: this.product().categoryName,
      price: this.product().price,
      currency: this.product().currency,
      quantity: 1,
      primaryImageUrl: this.product().primaryImageUrl,
      inStock: this.product().inStock,
    }).subscribe({
      next: () => {
        this.btnState.set('added');
        this.cartService.sidebarOpen.set(true);
        setTimeout(() => this.btnState.set('idle'), 2000);
      },
      error: () => {
        this.btnState.set('error');
        setTimeout(() => this.btnState.set('idle'), 2500);
      },
    });
  }
}
