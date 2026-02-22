import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CatalogProductListItem } from '../../models/catalog.model';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './product-card.component.html',
  styleUrls: ['./product-card.component.css'],
})
export class ProductCardComponent {
  product = input.required<CatalogProductListItem>();
  addToCart = output<CatalogProductListItem>();

  onAddToCart(e: Event): void {
    e.preventDefault();
    e.stopPropagation();
    this.addToCart.emit(this.product());
  }
}
