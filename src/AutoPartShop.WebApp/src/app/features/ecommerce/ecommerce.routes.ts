import { Routes } from '@angular/router';
import { EcommerceShellComponent } from './shell/ecommerce-shell.component';
import { EcommerceLandingComponent } from './landing/ecommerce-landing.component';
import { EcommerceCategoryComponent } from './category/ecommerce-category.component';
import { EcommerceProductComponent } from './product/ecommerce-product.component';

export const ecommerceRoutes: Routes = [
  {
    path: '',
    component: EcommerceShellComponent,
    children: [
      { path: '', component: EcommerceLandingComponent },
      { path: 'category/:categoryId', component: EcommerceCategoryComponent },
      { path: 'product/:partId', component: EcommerceProductComponent }
    ]
  }
];
