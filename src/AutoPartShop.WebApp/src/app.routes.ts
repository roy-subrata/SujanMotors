import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { Dashboard } from './app/pages/dashboard/dashboard';
import { Landing } from './app/pages/landing/landing';
import { Notfound } from './app/pages/notfound/notfound';
import { QuickSaleComponent } from './app/features/sales/quick-sale/quick-sale.component';
import { LoginComponent } from './app/pages/login/login.component';
import { AdminSettingsComponent } from './app/pages/admin-settings/admin-settings.component';
import { authGuard } from './app/shared/guards/auth.guard';
import { roleGuard } from './app/shared/guards/role.guard';

export const appRoutes: Routes = [
    // Login - standalone (no layout)
    { path: 'login', component: LoginComponent },

    // Quick Sale - standalone layout (no sidebar/header)
    { path: 'quick-sale', component: QuickSaleComponent },

    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', component: Dashboard },
            { path: 'inventory', loadChildren: () => import('./app/features/inventory/inventory.routes').then(m => m.inventoryRoutes) },
            { path: 'procurement', loadChildren: () => import('./app/features/procurement/procurement.routes').then(m => m.procurementRoutes) },
            { path: 'sales', loadChildren: () => import('./app/features/sales/sales.routes').then(m => m.salesRoutes) },
            { path: 'audit', loadChildren: () => import('./app/features/audit/audit.routes').then(m => m.auditRoutes) },
            {
                path: 'admin-settings',
                component: AdminSettingsComponent,
                canActivate: [roleGuard],
                data: { roles: ['Admin'] }
            }
        ]
    },
    { path: 'landing', component: Landing },
    { path: 'notfound', component: Notfound },
    { path: 'unauthorized', component: Notfound }, // You can create a dedicated unauthorized page later
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
