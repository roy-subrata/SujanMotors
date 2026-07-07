import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { Dashboard } from './app/pages/dashboard/dashboard';
import { DashboardComponent } from './app/features/dashboard/dashboard.component';
import { Landing } from './app/pages/landing/landing';
import { Notfound } from './app/pages/notfound/notfound';
import { QuickSaleShortcutComponent } from './app/features/sales/quick-sale-shortcut/quick-sale-shortcut.component';
import { UnifiedLoginComponent } from './app/pages/login/unified-login.component';
import { AdminSettingsComponent } from './app/pages/admin-settings/admin-settings.component';
import { authGuard } from './app/shared/guards/auth.guard';
import { roleGuard } from './app/shared/guards/role.guard';

export const appRoutes: Routes = [
    // Login - standalone (no layout) — shared unified login, staff mode default
    { path: 'login', component: UnifiedLoginComponent, data: { mode: 'staff' } },

    // Quick Sale (POS) - standalone layout (no sidebar/header) — auth required
    { path: 'quick-sale-shortcut', component: QuickSaleShortcutComponent, canActivate: [authGuard] },
    { path: 'pos', component: QuickSaleShortcutComponent, canActivate: [authGuard] },

    // E-commerce storefront - public module
    { path: 'shop', loadChildren: () => import('./app/features/ecommerce/ecommerce.routes').then(m => m.ecommerceRoutes) },

    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', component: DashboardComponent },
            { path: 'financial-dashboard', component: Dashboard },
            { path: 'inventory', loadChildren: () => import('./app/features/inventory/inventory.routes').then(m => m.inventoryRoutes) },
            { path: 'procurement', loadChildren: () => import('./app/features/procurement/procurement.routes').then(m => m.procurementRoutes) },
            { path: 'sales', loadChildren: () => import('./app/features/sales/sales.routes').then(m => m.salesRoutes) },
            { path: 'warranty', loadChildren: () => import('./app/features/warranty/warranty.routes').then(m => m.warrantyRoutes) },
            { path: 'finance', loadChildren: () => import('./app/features/finance/finance.routes').then(m => m.financeRoutes) },
            {
                path: 'hr',
                loadChildren: () => import('./app/features/hr/hr.routes').then(m => m.hrRoutes),
                canActivate: [roleGuard],
                data: { roles: ['Admin', 'Manager'] }
            },
            { path: 'audit', loadChildren: () => import('./app/features/audit/audit.routes').then(m => m.auditRoutes) },
            { path: 'admin', loadChildren: () => import('./app/features/admin/admin.routes').then(m => m.adminRoutes) },
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
