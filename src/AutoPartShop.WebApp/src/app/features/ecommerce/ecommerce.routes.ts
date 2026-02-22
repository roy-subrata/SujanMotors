import { Routes } from '@angular/router';
import { EcommerceShellComponent } from './shell/ecommerce-shell.component';
import { EcommerceLandingComponent } from './landing/ecommerce-landing.component';
import { EcommerceCategoryComponent } from './category/ecommerce-category.component';
import { EcommerceProductComponent } from './product/ecommerce-product.component';
import { EcommerceCartComponent } from './cart/ecommerce-cart.component';
import { EcommerceCheckoutComponent } from './checkout/ecommerce-checkout.component';
import { OrderConfirmationComponent } from './order-confirmation/order-confirmation.component';
import { EcommerceSaleComponent } from './sale/ecommerce-sale.component';

export const ecommerceRoutes: Routes = [
  {
    path: '',
    component: EcommerceShellComponent,
    children: [
      { path: '', component: EcommerceLandingComponent },
      { path: 'sale', component: EcommerceSaleComponent },
      { path: 'category/:categoryId', component: EcommerceCategoryComponent },
      { path: 'product/:partId', component: EcommerceProductComponent },
      { path: 'cart', component: EcommerceCartComponent },
      { path: 'checkout', component: EcommerceCheckoutComponent },
      { path: 'order-confirmation/:orderId', component: OrderConfirmationComponent }
    ]
  }
];
