import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';
import { AuthService } from '../../shared/services/auth.service';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    template: `<ul class="layout-menu">
        <ng-container *ngFor="let item of model; let i = index">
            <li app-menuitem *ngIf="!item.separator" [item]="item" [index]="i" [root]="true"></li>
            <li *ngIf="item.separator" class="menu-separator"></li>
        </ng-container>

    </ul> `
})
export class AppMenu implements OnInit {
    private authService = inject(AuthService);
    model: MenuItem[] = [];

    hasAdminRole(): boolean {
        return this.authService.hasRole('Admin');
    }

    ngOnInit() {
        this.model = [
            {
                label: 'Inventory',
                icon: 'pi pi-box',
                items: [
                    {
                        label: 'Categories',
                        icon: 'pi pi-list',
                        routerLink: ['/inventory/categories']
                    },
                    {
                        label: 'Brands',
                        icon: 'pi pi-tag',
                        routerLink: ['/inventory/brands']
                    },
                    {
                        label: 'Units',
                        icon: 'pi pi-sitemap',
                        routerLink: ['/inventory/units']
                    },
                    {
                        label: 'Parts',
                        icon: 'pi pi-shopping-bag',
                        routerLink: ['/inventory/parts']
                    },
                    {
                        label: 'Suppliers',
                        icon: 'pi pi-truck',
                        routerLink: ['/inventory/suppliers']
                    },
                    {
                        label: 'Warehouses',
                        icon: 'pi pi-building',
                        routerLink: ['/inventory/warehouses']
                    },
                    {
                        label: 'Vehicles',
                        icon: 'pi pi-car',
                        routerLink: ['/inventory/vehicles']
                    },
                    {
                        label: 'Stock',
                        icon: 'pi pi-warehouse',
                        routerLink: ['/inventory/stock']
                    }
                ]
            },
            {
                label: 'Procurement',
                icon: 'pi pi-briefcase',
                items: [
                    {
                        label: 'Purchase Orders',
                        icon: 'pi pi-list',
                        routerLink: ['/procurement/purchase-orders']
                    },
                    {
                        label: 'Purchase Returns',
                        icon: 'pi pi-reply',
                        routerLink: ['/procurement/purchase-returns']
                    },
                    {
                        label: 'Goods Receipts',
                        icon: 'pi pi-inbox',
                        routerLink: ['/procurement/goods-receipts']
                    },
                    {
                        label: 'Payment Providers',
                        icon: 'pi pi-credit-card',
                        routerLink: ['/procurement/payment-providers']
                    },
                    {
                        label: 'Supplier Payments',
                        icon: 'pi pi-money-bill',
                        routerLink: ['/procurement/supplier-payments']
                    }
                ]
            },
            {
                label: 'Sales',
                icon: 'pi pi-shopping-cart',
                items: [
                    {
                        label: 'Sales Orders',
                        icon: 'pi pi-file',
                        routerLink: ['/sales/sales-orders']
                    },
                    {
                        label: 'Invoices',
                        icon: 'pi pi-file-edit',
                        routerLink: ['/sales/invoices']
                    },
                    {
                        label: 'Customers',
                        icon: 'pi pi-users',
                        routerLink: ['/sales/customers']
                    },
                    {
                        label: 'Technicians',
                        icon: 'pi pi-wrench',
                        routerLink: ['/sales/technicians']
                    },
                    {
                        label: 'Payments',
                        icon: 'pi pi-dollar',
                        routerLink: ['/sales/customer-payments']
                    },
                    {
                        label: 'Returns',
                        icon: 'pi pi-replay',
                        routerLink: ['/sales/sales-returns']
                    }
                ]
            },
            {
                label: 'Audit Trail',
                icon: 'pi pi-history',
                items: [
                    {
                        label: 'Dashboard',
                        icon: 'pi pi-chart-bar',
                        routerLink: ['/audit/dashboard']
                    },
                    {
                        label: 'Activity Logs',
                        icon: 'pi pi-list',
                        routerLink: ['/audit/logs']
                    }
                ]
            },
            {
                label: 'Settings',
                icon: 'pi pi-cog',
                items: [
                    {
                        label: 'Admin Settings',
                        icon: 'pi pi-shield',
                        routerLink: ['/admin-settings'],
                        visible: this.hasAdminRole()
                    }
                ]
            },
            // {
            //     label: 'Reports',
            //     icon: 'pi pi-chart-bar',
            //     routerLink: ['/reports']
            // },
            // {
            //     label: 'Settings',
            //     icon: 'pi pi-cog',
            //     routerLink: ['/settings']
            // },
            // {
            //     label: 'Home',
            //     items: [{ label: 'Dashboard', icon: 'pi pi-fw pi-home', routerLink: ['/'] }]
            // },
            // {
            //     label: 'UI Components',
            //     items: [
            //         { label: 'Form Layout', icon: 'pi pi-fw pi-id-card', routerLink: ['/uikit/formlayout'] },
            //         { label: 'Input', icon: 'pi pi-fw pi-check-square', routerLink: ['/uikit/input'] },
            //         { label: 'Button', icon: 'pi pi-fw pi-mobile', class: 'rotated-icon', routerLink: ['/uikit/button'] },
            //         { label: 'Table', icon: 'pi pi-fw pi-table', routerLink: ['/uikit/table'] },
            //         { label: 'List', icon: 'pi pi-fw pi-list', routerLink: ['/uikit/list'] },
            //         { label: 'Tree', icon: 'pi pi-fw pi-share-alt', routerLink: ['/uikit/tree'] },
            //         { label: 'Panel', icon: 'pi pi-fw pi-tablet', routerLink: ['/uikit/panel'] },
            //         { label: 'Overlay', icon: 'pi pi-fw pi-clone', routerLink: ['/uikit/overlay'] },
            //         { label: 'Media', icon: 'pi pi-fw pi-image', routerLink: ['/uikit/media'] },
            //         { label: 'Menu', icon: 'pi pi-fw pi-bars', routerLink: ['/uikit/menu'] },
            //         { label: 'Message', icon: 'pi pi-fw pi-comment', routerLink: ['/uikit/message'] },
            //         { label: 'File', icon: 'pi pi-fw pi-file', routerLink: ['/uikit/file'] },
            //         { label: 'Chart', icon: 'pi pi-fw pi-chart-bar', routerLink: ['/uikit/charts'] },
            //         { label: 'Timeline', icon: 'pi pi-fw pi-calendar', routerLink: ['/uikit/timeline'] },
            //         { label: 'Misc', icon: 'pi pi-fw pi-circle', routerLink: ['/uikit/misc'] }
            //     ]
            // },
            // {
            //     label: 'Pages',
            //     icon: 'pi pi-fw pi-briefcase',
            //     routerLink: ['/pages'],
            //     items: [
            //         {
            //             label: 'Landing',
            //             icon: 'pi pi-fw pi-globe',
            //             routerLink: ['/landing']
            //         },
            //         {
            //             label: 'Auth',
            //             icon: 'pi pi-fw pi-user',
            //             items: [
            //                 {
            //                     label: 'Login',
            //                     icon: 'pi pi-fw pi-sign-in',
            //                     routerLink: ['/auth/login']
            //                 },
            //                 {
            //                     label: 'Error',
            //                     icon: 'pi pi-fw pi-times-circle',
            //                     routerLink: ['/auth/error']
            //                 },
            //                 {
            //                     label: 'Access Denied',
            //                     icon: 'pi pi-fw pi-lock',
            //                     routerLink: ['/auth/access']
            //                 }
            //             ]
            //         },
            //         {
            //             label: 'Crud',
            //             icon: 'pi pi-fw pi-pencil',
            //             routerLink: ['/pages/crud']
            //         },
            //         {
            //             label: 'Not Found',
            //             icon: 'pi pi-fw pi-exclamation-circle',
            //             routerLink: ['/pages/notfound']
            //         },
            //         {
            //             label: 'Empty',
            //             icon: 'pi pi-fw pi-circle-off',
            //             routerLink: ['/pages/empty']
            //         }
            //     ]
            // },
            // {
            //     label: 'Hierarchy',
            //     items: [
            //         {
            //             label: 'Submenu 1',
            //             icon: 'pi pi-fw pi-bookmark',
            //             items: [
            //                 {
            //                     label: 'Submenu 1.1',
            //                     icon: 'pi pi-fw pi-bookmark',
            //                     items: [
            //                         { label: 'Submenu 1.1.1', icon: 'pi pi-fw pi-bookmark' },
            //                         { label: 'Submenu 1.1.2', icon: 'pi pi-fw pi-bookmark' },
            //                         { label: 'Submenu 1.1.3', icon: 'pi pi-fw pi-bookmark' }
            //                     ]
            //                 },
            //                 {
            //                     label: 'Submenu 1.2',
            //                     icon: 'pi pi-fw pi-bookmark',
            //                     items: [{ label: 'Submenu 1.2.1', icon: 'pi pi-fw pi-bookmark' }]
            //                 }
            //             ]
            //         },
            //         {
            //             label: 'Submenu 2',
            //             icon: 'pi pi-fw pi-bookmark',
            //             items: [
            //                 {
            //                     label: 'Submenu 2.1',
            //                     icon: 'pi pi-fw pi-bookmark',
            //                     items: [
            //                         { label: 'Submenu 2.1.1', icon: 'pi pi-fw pi-bookmark' },
            //                         { label: 'Submenu 2.1.2', icon: 'pi pi-fw pi-bookmark' }
            //                     ]
            //                 },
            //                 {
            //                     label: 'Submenu 2.2',
            //                     icon: 'pi pi-fw pi-bookmark',
            //                     items: [{ label: 'Submenu 2.2.1', icon: 'pi pi-fw pi-bookmark' }]
            //                 }
            //             ]
            //         }
            //     ]
            // },
            // {
            //     label: 'Get Started',
            //     items: [
            //         {
            //             label: 'Documentation',
            //             icon: 'pi pi-fw pi-book',
            //             routerLink: ['/documentation']
            //         },
            //         {
            //             label: 'View Source',
            //             icon: 'pi pi-fw pi-github',
            //             url: 'https://github.com/primefaces/sakai-ng',
            //             target: '_blank'
            //         }
            //     ]
            // }
        ];
    }
}
