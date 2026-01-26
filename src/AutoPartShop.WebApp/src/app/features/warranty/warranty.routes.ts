import { Routes } from '@angular/router';

export const warrantyRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: 'registrations',
                loadComponent: () => import('./warranties-list/warranties-list.component').then(m => m.WarrantiesListComponent),
                data: { breadcrumb: 'Warranty Registrations' }
            },
            {
                path: 'claims',
                loadComponent: () => import('./claims-list/claims-list.component').then(m => m.ClaimsListComponent),
                data: { breadcrumb: 'Warranty Claims' }
            },
            {
                path: '',
                redirectTo: 'registrations',
                pathMatch: 'full'
            }
        ]
    }
];
