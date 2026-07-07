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
        const adminOnly = this.hasAdminRole();

        this.model = [
            // ==================== DASHBOARD ====================
            {
                label: this.i18n.t('menu.dashboard'),
                icon: 'pi pi-home',
                routerLink: ['/']
            },

            // ==================== CATALOG ====================
            {
                label: this.i18n.t('menu.catalog'),
                icon: 'pi pi-box',
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
                        label: this.i18n.t('menu.units'),
                        icon: 'pi pi-sitemap',
                        routerLink: ['/inventory/units']
                    },
                    {
                        label: this.i18n.t('menu.attributeGroups'),
                        icon: 'pi pi-sliders-h',
                        routerLink: ['/inventory/attribute-groups']
                    },
                    {
                        label: this.i18n.t('menu.discounts'),
                        icon: 'pi pi-percentage',
                        routerLink: ['/inventory/discounts']
                    }
                ]
            },

            // ==================== INVENTORY ====================
            {
                label: this.i18n.t('menu.inventory'),
                icon: 'pi pi-warehouse',
                items: [
                    {
                        label: this.i18n.t('menu.stockManagement'),
                        icon: 'pi pi-chart-bar',
                        routerLink: ['/inventory/stock']
                    },
                    {
                        label: this.i18n.t('menu.stockTake'),
                        icon: 'pi pi-list-check',
                        routerLink: ['/inventory/stock-takes']
                    },
                    {
                        label: this.i18n.t('menu.warehouses'),
                        icon: 'pi pi-building',
                        routerLink: ['/inventory/warehouses']
                    }
                ]
            },

            // ==================== PURCHASING ====================
            {
                label: this.i18n.t('menu.purchasing'),
                icon: 'pi pi-briefcase',
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
                    },
                    {
                        label: this.i18n.t('menu.suppliers'),
                        icon: 'pi pi-truck',
                        routerLink: ['/inventory/suppliers']
                    },
                    {
                        label: this.i18n.t('menu.supplierPayments'),
                        icon: 'pi pi-money-bill',
                        routerLink: ['/procurement/supplier-payments']
                    },
                    {
                        label: this.i18n.t('menu.supplierStatements'),
                        icon: 'pi pi-chart-line',
                        routerLink: ['/procurement/supplier-account-summary']
                    },
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
            },

            // ==================== SALES ====================
            {
                label: this.i18n.t('menu.sales'),
                icon: 'pi pi-shopping-cart',
                items: [
                    {
                        label: this.i18n.t('menu.salesOrders'),
                        icon: 'pi pi-file',
                        routerLink: ['/sales/sales-orders']
                    },
                    {
                        label: this.i18n.t('menu.invoices'),
                        icon: 'pi pi-file-check',
                        routerLink: ['/sales/invoices']
                    },
                    {
                        label: this.i18n.t('menu.pendingDeliveries'),
                        icon: 'pi pi-truck',
                        routerLink: ['/sales/pending-deliveries']
                    },
                    {
                        label: this.i18n.t('menu.salesReturns'),
                        icon: 'pi pi-replay',
                        routerLink: ['/sales/sales-returns']
                    },
                    {
                        label: this.i18n.t('menu.customers'),
                        icon: 'pi pi-users',
                        routerLink: ['/sales/customers']
                    },
                    {
                        label: this.i18n.t('menu.customerPayments'),
                        icon: 'pi pi-dollar',
                        routerLink: ['/sales/customer-payments']
                    },
                    {
                        label: this.i18n.t('menu.customerStatements'),
                        icon: 'pi pi-chart-line',
                        routerLink: ['/sales/customer-account-summary']
                    }
                ]
            },

            // ==================== SERVICE & WARRANTY ====================
            {
                label: this.i18n.t('menu.serviceWarranty'),
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
                    },
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
            },

            // ==================== FINANCE ====================
            {
                label: this.i18n.t('menu.finance'),
                icon: 'pi pi-chart-line',
                items: [
                    {
                        label: this.i18n.t('menu.dailyCashBook'),
                        icon: 'pi pi-book',
                        routerLink: ['/finance/cash-book']
                    },
                    {
                        label: this.i18n.t('menu.exchangeRates'),
                        icon: 'pi pi-percentage',
                        routerLink: ['/admin/exchange-rates'],
                        visible: adminOnly
                    }
                ]
            },

            // ==================== ONLINE STORE ====================
            {
                label: this.i18n.t('menu.onlineStore'),
                icon: 'pi pi-globe',
                routerLink: ['/shop']
            },

            // ==================== ADMINISTRATION (admin only) ====================
            {
                label: this.i18n.t('menu.administration'),
                icon: 'pi pi-cog',
                visible: adminOnly,
                items: [
                    {
                        label: this.i18n.t('menu.companyProfile'),
                        icon: 'pi pi-building',
                        routerLink: ['/admin/company-profile']
                    },
                    {
                        label: this.i18n.t('menu.currencies'),
                        icon: 'pi pi-dollar',
                        routerLink: ['/admin/currencies']
                    },
                    {
                        label: this.i18n.t('menu.shopPolicies'),
                        icon: 'pi pi-shield',
                        routerLink: ['/admin/shop-policies']
                    },
                    {
                        label: this.i18n.t('menu.settings'),
                        icon: 'pi pi-cog',
                        routerLink: ['/admin-settings']
                    },
                    {
                        label: this.i18n.t('menu.auditDashboard'),
                        icon: 'pi pi-chart-bar',
                        routerLink: ['/audit/dashboard']
                    },
                    {
                        label: this.i18n.t('menu.auditTrail'),
                        icon: 'pi pi-history',
                        routerLink: ['/audit/logs']
                    }
                ]
            }
        ];
    }
}
