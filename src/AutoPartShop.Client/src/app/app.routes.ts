import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'inventory',
    children: [
      {
        path: 'categories',
        loadComponent: () => import('./features/inventory/categories/categories.component').then(m => m.CategoriesComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./features/inventory/products/products.component').then(m => m.ProductsComponent)
      },
      {
        path: 'stock',
        loadComponent: () => import('./features/inventory/stock/stock.component').then(m => m.StockComponent)
      }
    ]
  },
  {
    path: 'orders',
    children: [
      {
        path: '',
        loadComponent: () => import('./features/orders/orders-list/orders-list.component').then(m => m.OrdersListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./features/orders/orders-create/orders-create.component').then(m => m.OrdersCreateComponent)
      }
    ]
  },
  {
    path: 'reports',
    loadComponent: () => import('./features/reports/reports.component').then(m => m.ReportsComponent)
  },
  {
    path: 'settings',
    loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent)
  }
];
