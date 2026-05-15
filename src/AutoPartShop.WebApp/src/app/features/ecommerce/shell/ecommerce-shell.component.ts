import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { RouterModule } from '@angular/router';
import { StoreHeaderComponent } from './header/store-header.component';
import { StoreFooterComponent } from './footer/store-footer.component';
import { CartSidebarComponent } from '../components/cart-sidebar/cart-sidebar.component';
import { CartService } from '../services/cart.service';

@Component({
  selector: 'app-ecommerce-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, StoreHeaderComponent, StoreFooterComponent, CartSidebarComponent],
  templateUrl: './ecommerce-shell.component.html',
  styleUrls: ['./ecommerce-shell.component.css'],
})
export class EcommerceShellComponent implements OnInit, OnDestroy {
  private readonly document = inject(DOCUMENT);
  readonly cartService = inject(CartService);

  ngOnInit(): void {
    this.document.body.classList.add('storefront-scroll');
    this.document.documentElement.classList.add('storefront-scroll');
  }

  ngOnDestroy(): void {
    this.document.body.classList.remove('storefront-scroll');
    this.document.documentElement.classList.remove('storefront-scroll');
  }
}
