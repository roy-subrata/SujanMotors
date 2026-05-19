import { Routes } from '@angular/router';
import { EcommerceShellComponent } from './shell/ecommerce-shell.component';
import { EcommerceLandingComponent } from './landing/ecommerce-landing.component';
import { EcommerceCategoryComponent } from './category/ecommerce-category.component';
import { EcommerceProductComponent } from './product/ecommerce-product.component';
import { EcommerceCartComponent } from './cart/ecommerce-cart.component';
import { EcommerceCheckoutComponent } from './checkout/ecommerce-checkout.component';
import { OrderConfirmationComponent } from './order-confirmation/order-confirmation.component';
import { EcommerceSaleComponent } from './sale/ecommerce-sale.component';
import { UnifiedLoginComponent } from '../../pages/login/unified-login.component';
import { EcommerceRegisterComponent } from './auth/ecommerce-register.component';
import { InstoreCheckoutComponent } from './instore/instore-checkout.component';
import { customerAuthGuard } from './guards/customer-auth.guard';
import { authGuard } from '../../shared/guards/auth.guard';

export const ecommerceRoutes: Routes = [
  // Auth pages — outside the shell (no nav/footer) — customer mode default
  { path: 'login', component: UnifiedLoginComponent, data: { mode: 'customer' } },
  { path: 'register', component: EcommerceRegisterComponent },

  {
    path: '',
    component: EcommerceShellComponent,
    children: [
      { path: '', component: EcommerceLandingComponent },
      { path: 'sale', component: EcommerceSaleComponent },
      { path: 'category/:categoryId', component: EcommerceCategoryComponent },
      { path: 'product/:partId', component: EcommerceProductComponent },
      { path: 'cart', component: EcommerceCartComponent },
      // Online customer checkout — requires customer JWT
      { path: 'checkout', component: EcommerceCheckoutComponent, canActivate: [customerAuthGuard] },
      // Salesperson in-store checkout — requires staff JWT
      { path: 'instore-checkout', component: InstoreCheckoutComponent, canActivate: [authGuard] },
      { path: 'order-confirmation/:orderId', component: OrderConfirmationComponent },
      { path: 'search', component: EcommerceCategoryComponent }
    ]
  }
];
