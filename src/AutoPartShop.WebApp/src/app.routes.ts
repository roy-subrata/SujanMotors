import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { DashboardComponent } from './app/features/dashboard/dashboard.component';
import { QuickSaleShortcutComponent } from './app/features/sales/quick-sale-shortcut/quick-sale-shortcut.component';
import { UnifiedLoginComponent } from './app/pages/login/unified-login.component';
import { UnauthorizedComponent } from './app/pages/unauthorized/unauthorized.component';
import { AdminSettingsComponent } from './app/pages/admin-settings/admin-settings.component';
import { authGuard } from './app/shared/guards/auth.guard';
import { roleGuard } from './app/shared/guards/role.guard';
import { permissionGuard } from './app/shared/guards/permission.guard';

export const appRoutes: Routes = [
    // Login - standalone (no layout) — staff sign in
    { path: 'login', component: UnifiedLoginComponent },

    // Access denied - standalone (no layout) — guard/403 landing page
    { path: 'unauthorized', component: UnauthorizedComponent },

    // Quick Sale (POS) - standalone layout (no sidebar/header) — auth required
    { path: 'quick-sale-shortcut', component: QuickSaleShortcutComponent, canActivate: [authGuard] },
    { path: 'pos', component: QuickSaleShortcutComponent, canActivate: [authGuard] },

    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            // Financial KPIs — restricted like reports (product decision 2026-08-25).
            { path: '', component: DashboardComponent, canActivate: [permissionGuard], data: { permissions: ['reports.view'] } },
            { path: 'financial-dashboard', component: DashboardComponent, canActivate: [permissionGuard], data: { permissions: ['reports.view'] } },
            {
                path: 'notifications',
                loadChildren: () => import('./app/features/notifications/notifications.routes').then(m => m.notificationsRoutes)
            },
            {
                path: 'shortcuts',
                loadComponent: () => import('./app/features/shortcuts/keyboard-shortcuts.component').then(m => m.KeyboardShortcutsComponent)
            },
            {
                path: 'inventory',
                loadChildren: () => import('./app/features/inventory/inventory.routes').then(m => m.inventoryRoutes),
                canActivate: [permissionGuard],
                data: { permissions: ['inventory.view'] }
            },
            {
                path: 'procurement',
                loadChildren: () => import('./app/features/procurement/procurement.routes').then(m => m.procurementRoutes),
                canActivate: [permissionGuard],
                data: { permissions: ['procurement.view'] }
            },
            {
                path: 'sales',
                loadChildren: () => import('./app/features/sales/sales.routes').then(m => m.salesRoutes),
                canActivate: [permissionGuard],
                data: { permissions: ['sales.view'] }
            },
            {
                path: 'warranty',
                loadChildren: () => import('./app/features/warranty/warranty.routes').then(m => m.warrantyRoutes),
                canActivate: [permissionGuard],
                data: { permissions: ['sales.view'] }
            },
            {
                path: 'finance',
                loadChildren: () => import('./app/features/finance/finance.routes').then(m => m.financeRoutes),
                canActivate: [permissionGuard],
                data: { permissions: ['reports.view'] }
            },
            {
                path: 'reports',
                loadChildren: () => import('./app/features/reports/reports.routes').then(m => m.reportsRoutes),
                canActivate: [permissionGuard],
                data: { permissions: ['reports.view'] }
            },
            {
                path: 'hr',
                loadChildren: () => import('./app/features/hr/hr.routes').then(m => m.hrRoutes),
                canActivate: [roleGuard],
                data: { roles: ['Admin', 'Manager'] }
            },
            {
                path: 'audit',
                loadChildren: () => import('./app/features/audit/audit.routes').then(m => m.auditRoutes),
                canActivate: [permissionGuard],
                data: { permissions: ['audit.view'] }
            },
            { path: 'admin', loadChildren: () => import('./app/features/admin/admin.routes').then(m => m.adminRoutes) },
            {
                path: 'admin-settings',
                component: AdminSettingsComponent,
                canActivate: [roleGuard],
                data: { roles: ['Admin'] }
            }
        ]
    },
    { path: '**', redirectTo: '' }
];
