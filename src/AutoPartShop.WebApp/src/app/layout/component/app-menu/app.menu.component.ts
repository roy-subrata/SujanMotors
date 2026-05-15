import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { I18nService } from '@/shared/services/i18n.service';
import { AuthService } from '@/shared/services/auth.service';
import { AppMenuitem } from '../app.menuitem';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    templateUrl: './app.menu.component.html'
})
export class AppMenuComponent implements OnInit {
    private authService = inject(AuthService);
    private i18n = inject(I18nService);
    model: MenuItem[] = [];

    hasAdminRole(): boolean {
        return this.authService.hasRole('Admin');
    }

    ngOnInit() {
        this.buildMenu();

        // Rebuild menu when language changes
        this.i18n.translationsLoaded$.subscribe(() => {
            this.buildMenu();
        });
    }

    private buildMenu(): void {
        this.model = [
            // ==================== DASHBOARD ====================
            {
                label: 'Dashboard',
                icon: 'pi pi-home',
                routerLink: ['/']
            },

            // ==================== INVENTORY ====================
            {
                label: this.i18n.t('menu.inventory'),
                icon: 'pi pi-box',
                items: [
                    {
                        label: 'Products',
                        icon: 'pi pi-shopping-bag',
                        items: [
                            {
                                label: this.i18n.t('menu.parts'),
                                icon: 'pi pi-shopping-bag',
                                routerLink: ['/inventory/parts']
                            },
                            {
                                label: this.i18n.t('menu.categories'),
                                icon: 'pi pi-list',
                                routerLink: ['/inventory/categories']
                            },
                            {
                                label: this.i18n.t('menu.brands'),
                                icon: 'pi pi-tag',
                                routerLink: ['/inventory/brands']
                            },
                            {
                                label: 'Discounts',
                                icon: 'pi pi-percentage',
                                routerLink: ['/inventory/discounts']
                            },
                            {
                                label: this.i18n.t('menu.units'),
                                icon: 'pi pi-sitemap',
                                routerLink: ['/inventory/units']
                            },
                            {
                                label: 'Attribute Groups',
                                icon: 'pi pi-sliders-h',
                                routerLink: ['/inventory/attribute-groups']
                            }
                        ]
                    },
                    {
                        label: this.i18n.t('menu.stockManagement'),
                        icon: 'pi pi-warehouse',
                        routerLink: ['/inventory/stock']
                    },
                    {
                        label: this.i18n.t('menu.warehouses'),
                        icon: 'pi pi-building',
                        routerLink: ['/inventory/warehouses']
                    }
                ]
            },

            // ==================== PROCUREMENT ====================
            {
                label: this.i18n.t('menu.procurement'),
                icon: 'pi pi-briefcase',
                items: [
                    {
                        label: 'Suppliers',
                        icon: 'pi pi-truck',
                        items: [
                            {
                                label: 'Suppliers',
                                icon: 'pi pi-truck',
                                routerLink: ['/inventory/suppliers']
                            },
                            {
                                label: 'Supplier Payments',
                                icon: 'pi pi-money-bill',
                                routerLink: ['/procurement/supplier-payments']
                            },
                            {
                                label: 'Account Summary',
                                icon: 'pi pi-chart-line',
                                routerLink: ['/procurement/supplier-account-summary']
                            }
                        ]
                    },
                    {
                        label: 'Purchasing',
                        icon: 'pi pi-shopping-cart',
                        items: [
                            {
                                label: this.i18n.t('menu.purchaseOrders'),
                                icon: 'pi pi-file',
                                routerLink: ['/procurement/purchase-orders']
                            },
                            {
                                label: this.i18n.t('menu.goodsReceipts'),
                                icon: 'pi pi-inbox',
                                routerLink: ['/procurement/goods-receipts']
                            },
                            {
                                label: this.i18n.t('menu.purchaseReturns'),
                                icon: 'pi pi-reply',
                                routerLink: ['/procurement/purchase-returns']
                            }
                        ]
                    },
                    {
                        label: 'Expenses',
                        icon: 'pi pi-wallet',
                        items: [
                            {
                                label: this.i18n.t('menu.dailyExpenses'),
                                icon: 'pi pi-receipt',
                                routerLink: ['/procurement/daily-expenses']
                            },
                            {
                                label: this.i18n.t('menu.paymentProviders'),
                                icon: 'pi pi-credit-card',
                                routerLink: ['/procurement/payment-providers']
                            }
                        ]
                    }
                ]
            },

            // ==================== SALES ====================
            {
                label: this.i18n.t('menu.sales'),
                icon: 'pi pi-shopping-cart',
                items: [
                    {
                        label: 'Online Store',
                        icon: 'pi pi-globe',
                        routerLink: ['/shop']
                    },
                    {
                        label: 'Sales Orders',
                        icon: 'pi pi-file',
                        items: [
                            {
                                label: this.i18n.t('menu.salesOrders'),
                                icon: 'pi pi-file',
                                routerLink: ['/sales/sales-orders']
                            },
                            {
                                label: this.i18n.t('menu.salesReturns'),
                                icon: 'pi pi-replay',
                                routerLink: ['/sales/sales-returns']
                            }
                        ]
                    },
                    {
                        label: 'Customers',
                        icon: 'pi pi-users',
                        items: [
                            {
                                label: 'Manage Customers',
                                icon: 'pi pi-users',
                                routerLink: ['/sales/customers']
                            },
                            {
                                label: this.i18n.t('menu.customerPayments'),
                                icon: 'pi pi-dollar',
                                routerLink: ['/sales/customer-payments']
                            },
                            {
                                label: 'Account Summary',
                                icon: 'pi pi-chart-line',
                                routerLink: ['/sales/customer-account-summary']
                            }
                        ]
                    },
                    {
                        label: 'Warranty',
                        icon: 'pi pi-shield',
                        items: [
                            {
                                label: this.i18n.t('menu.warrantyRegistrations'),
                                icon: 'pi pi-check-square',
                                routerLink: ['/warranty/registrations']
                            },
                            {
                                label: this.i18n.t('menu.warrantyClaims'),
                                icon: 'pi pi-exclamation-triangle',
                                routerLink: ['/warranty/claims']
                            }
                        ]
                    }
                ]
            },

            // ==================== SERVICE ====================
            {
                label: 'Service',
                icon: 'pi pi-wrench',
                items: [
                    {
                        label: this.i18n.t('menu.technicians'),
                        icon: 'pi pi-user',
                        routerLink: ['/sales/technicians']
                    },
                    {
                        label: this.i18n.t('menu.vehicles'),
                        icon: 'pi pi-car',
                        routerLink: ['/inventory/vehicles']
                    }
                ]
            },

            // ==================== FINANCE ====================
            {
                label: 'Finance',
                icon: 'pi pi-chart-line',
                items: [
                    {
                        label: this.i18n.t('menu.invoices'),
                        icon: 'pi pi-file-invoice',
                        routerLink: ['/sales/invoices']
                    },
                    {
                        label: this.i18n.t('menu.exchangeRates'),
                        icon: 'pi pi-percentage',
                        routerLink: ['/admin/exchange-rates'],
                        visible: this.hasAdminRole()
                    }
                ]
            },

            // ==================== ADMIN ====================
            {
                label: this.i18n.t('menu.admin'),
                icon: 'pi pi-cog',
                items: [
                    {
                        label: 'Currencies',
                        icon: 'pi pi-dollar',
                        routerLink: ['/admin/currencies'],
                        visible: this.hasAdminRole()
                    },
                    {
                        label: 'Shop Policies',
                        icon: 'pi pi-truck',
                        routerLink: ['/admin/shop-policies'],
                        visible: this.hasAdminRole()
                    },
                    {
                        label: this.i18n.t('menu.settings'),
                        icon: 'pi pi-cog',
                        routerLink: ['/admin-settings'],
                        visible: this.hasAdminRole()
                    },
                    {
                        label: 'Audit',
                        icon: 'pi pi-history',
                        items: [
                            {
                                label: this.i18n.t('menu.dashboard'),
                                icon: 'pi pi-chart-bar',
                                routerLink: ['/audit/dashboard']
                            },
                            {
                                label: this.i18n.t('menu.auditTrail'),
                                icon: 'pi pi-list',
                                routerLink: ['/audit/logs']
                            }
                        ]
                    }
                ]
            }
        ];
    }
}
