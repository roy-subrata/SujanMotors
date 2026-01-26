import { Routes } from '@angular/router';
import { roleGuard } from '../../shared/guards/role.guard';

export const adminRoutes: Routes = [
  {
    path: 'currencies',
    loadComponent: () => import('./currencies/currencies-list.component').then(m => m.CurrenciesListComponent),
    canActivate: [roleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'exchange-rates',
    loadComponent: () => import('./exchange-rates/exchange-rates-list.component').then(m => m.ExchangeRatesListComponent),
    canActivate: [roleGuard],
    data: { roles: ['Admin'] }
  }
];
