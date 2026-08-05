import { Routes } from '@angular/router';
import { authGuard } from '../../shared/guards/auth.guard';

export const notificationsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./inbox.component').then(m => m.InboxComponent),
    canActivate: [authGuard]
  }
];
