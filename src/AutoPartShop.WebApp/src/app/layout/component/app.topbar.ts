import { Component, inject, computed, signal, OnInit, OnDestroy } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StyleClassModule } from 'primeng/styleclass';
import { TooltipModule } from 'primeng/tooltip';
import { AvatarModule } from 'primeng/avatar';
import { MenuModule } from 'primeng/menu';
import { BadgeModule } from 'primeng/badge';
import { InputTextModule } from 'primeng/inputtext';
import { LayoutService } from '../service/layout.service';
import { AuthService } from '../../shared/services/auth.service';
import { I18nService } from '../../shared/services/i18n.service';
import { NotificationHubService, SaleNotificationEvent } from '../../shared/services/notification-hub.service';
import { LanguageSwitcherComponent } from '../../shared/components/language-switcher/language-switcher.component';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-topbar',
    standalone: true,
    imports: [RouterModule, CommonModule, FormsModule, StyleClassModule, TooltipModule, AvatarModule, MenuModule, BadgeModule, InputTextModule, LanguageSwitcherComponent, ToastModule],
    providers: [MessageService],
    template: ` <p-toast position="top-right" key="sale-notification"></p-toast>
    <div class="layout-topbar">
        <!-- Mobile Menu Toggle -->
        <button class="layout-menu-button layout-topbar-action" (click)="layoutService.onMenuToggle()">
            <i class="pi pi-bars"></i>
        </button>

        <!-- Page Title -->
        <div class="topbar-title">
            <h1>{{ pageTitle() }}</h1>
        </div>

        <!-- Actions -->
        <div class="topbar-actions">
            <!-- Quick Sale -->
            <button
                type="button"
                class="topbar-action-btn quick-sale-shortcut-btn"
                (click)="navigateToQuickSaleShortcut()"
                pTooltip="Quick Sale (POS)"
                tooltipPosition="bottom">
                <i class="pi pi-bolt"></i>
            </button>

            <!-- Language Switcher -->
            <app-language-switcher></app-language-switcher>

            <!-- Theme Toggle -->
            <button
                type="button"
                class="topbar-action-btn"
                (click)="toggleDarkMode()"
                [pTooltip]="layoutService.isDarkTheme() ? 'Switch to Light Mode' : 'Switch to Dark Mode'"
                tooltipPosition="bottom">
                <i [ngClass]="{ 'pi': true, 'pi-moon': layoutService.isDarkTheme(), 'pi-sun': !layoutService.isDarkTheme() }"></i>
            </button>

            <!-- Notifications -->
            <div class="notification-container">
                <button
                    type="button"
                    class="topbar-action-btn"
                    pBadge
                    [value]="notificationCount().toString()"
                    severity="danger"
                    pTooltip="Notifications"
                    tooltipPosition="bottom"
                    (click)="notificationsMenu.toggle($event)">
                    <i class="pi pi-bell"></i>
                </button>
                <p-menu
                    #notificationsMenu
                    [model]="notificationItems"
                    [popup]="true"
                    [appendTo]="'body'"
                    [style]="{'min-width': '320px', 'max-width': '400px'}">
                    <ng-template pTemplate="start">
                        <div class="notifications-header">
                            <h3>Notifications</h3>
                            <button type="button" class="text-btn">Mark all as read</button>
                        </div>
                    </ng-template>
                    <ng-template pTemplate="item" let-item>
                        <div class="notification-item-custom">
                            <i [class]="'pi ' + item.icon + ' notification-icon ' + (item.severity || '')"></i>
                            <div class="notification-content">
                                <p class="notification-title">{{ item.label }}</p>
                                <p class="notification-time">{{ item.time }}</p>
                            </div>
                        </div>
                    </ng-template>
                </p-menu>
            </div>

            <!-- User Menu -->
            @if (currentUser(); as user) {
                <div class="user-menu-container">
                    <button
                        type="button"
                        class="user-menu-btn"
                        (click)="userMenu.toggle($event)">
                        <p-avatar
                            [label]="getUserInitials()"
                            shape="circle"
                            size="normal"
                            [style]="{'background-color':'#667eea', 'color': '#ffffff'}">
                        </p-avatar>
                    </button>
                    <p-menu
                        #userMenu
                        [model]="userMenuItems"
                        [popup]="true"
                        [appendTo]="'body'"
                        [style]="{'min-width': '200px'}">
                        <ng-template pTemplate="start">
                            <div class="user-menu-header">
                                <p-avatar
                                    [label]="getUserInitials()"
                                    shape="circle"
                                    size="large"
                                    [style]="{'background-color':'#667eea', 'color': '#ffffff'}">
                                </p-avatar>
                                <div class="user-menu-info">
                                    <span class="user-menu-name">{{ user.fullName }}</span>
                                    <span class="user-menu-email">{{ user.email }}</span>
                                </div>
                            </div>
                        </ng-template>
                    </p-menu>
                </div>
            }
        </div>
    </div>`,
    styles: [`
        // Menu and notification styles
        ::ng-deep .user-menu-header {
            padding: 1rem;
            border-bottom: 1px solid var(--surface-border);
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }

        ::ng-deep .user-menu-info {
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
        }

        ::ng-deep .user-menu-name {
            font-weight: 600;
            font-size: 0.875rem;
            color: var(--text-color);
        }

        ::ng-deep .user-menu-email {
            font-size: 0.75rem;
            color: var(--text-color-secondary);
        }

        /* Fix PrimeNG menu items styling */
        ::ng-deep .p-menu {
            background: var(--surface-card);
            border: 1px solid var(--surface-border);
            border-radius: 6px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
            z-index: 9999 !important;
        }

        /* Ensure menu overlay wrapper has high z-index */
        ::ng-deep .p-menu-overlay {
            z-index: 9999 !important;
        }

        ::ng-deep .p-component-overlay {
            z-index: 9998 !important;
        }

        ::ng-deep .p-menu .p-menuitem-link {
            padding: 0.75rem 1rem;
            color: var(--text-color);
            transition: background-color 0.15s;
        }

        ::ng-deep .p-menu .p-menuitem-link:hover {
            background: var(--surface-hover);
        }

        ::ng-deep .p-menu .p-menuitem-link .p-menuitem-icon {
            color: var(--text-color-secondary);
            margin-right: 0.5rem;
        }

        ::ng-deep .p-menu .p-menuitem-link .p-menuitem-text {
            color: var(--text-color);
        }

        ::ng-deep .p-menu .p-menu-separator {
            border-top: 1px solid var(--surface-border);
            margin: 0.25rem 0;
        }

        ::ng-deep .notifications-header {
            padding: 1rem;
            border-bottom: 1px solid var(--surface-border);
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        ::ng-deep .notifications-header h3 {
            margin: 0;
            font-size: 1rem;
            font-weight: 600;
            color: var(--text-color);
        }

        ::ng-deep .text-btn {
            background: none;
            border: none;
            color: var(--primary-color);
            cursor: pointer;
            font-size: 0.875rem;
            padding: 0;
        }

        ::ng-deep .text-btn:hover {
            text-decoration: underline;
        }

        ::ng-deep .notification-item-custom {
            display: flex;
            gap: 0.75rem;
            padding: 0.75rem 1rem;
            cursor: pointer;
            transition: background-color 0.15s;
        }

        ::ng-deep .notification-item-custom:hover {
            background: var(--surface-hover);
        }

        ::ng-deep .notification-icon {
            font-size: 1.25rem;
            color: var(--primary-color);
            margin-top: 0.125rem;
            flex-shrink: 0;
        }

        ::ng-deep .notification-icon.warning {
            color: #f59e0b;
        }

        ::ng-deep .notification-icon.success {
            color: #10b981;
        }

        ::ng-deep .notification-content {
            flex: 1;
            min-width: 0;
        }

        ::ng-deep .notification-title {
            margin: 0 0 0.25rem 0;
            font-size: 0.875rem;
            font-weight: 500;
            color: var(--text-color);
        }

        ::ng-deep .notification-time {
            margin: 0;
            font-size: 0.75rem;
            color: var(--text-color-secondary);
        }

        /* Fix notification menu list styling */
        ::ng-deep .p-menu-list {
            padding: 0;
            margin: 0;
            list-style: none;
        }

        .layout-menu-button {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 40px;
            height: 40px;
            border: none;
            background: none;
            color: var(--text-color);
            cursor: pointer;
            border-radius: 50%;
            transition: background-color 0.15s;
        }

        .layout-menu-button:hover {
            background: var(--surface-hover);
        }

        .layout-menu-button i {
            font-size: 1.25rem;
        }

        .quick-sale-btn {
            background: var(--primary-color) !important;
            color: white !important;
        }

        .quick-sale-btn:hover {
            background: var(--primary-600) !important;
            transform: scale(1.05);
        }

        .quick-sale-btn i {
            color: white !important;
        }

        .quick-sale-shortcut-btn {
            background: #0ea5e9 !important;
            color: white !important;
        }

        .quick-sale-shortcut-btn:hover {
            background: #0284c7 !important;
            transform: scale(1.05);
        }

        .quick-sale-shortcut-btn i {
            color: white !important;
        }
    `]
})
export class AppTopbar implements OnInit, OnDestroy {
    public layoutService = inject(LayoutService);
    private authService = inject(AuthService);
    private router = inject(Router);
    private i18n = inject(I18nService);
    private notificationHub = inject(NotificationHubService);
    private messageService = inject(MessageService);

    currentUser = computed(() => this.authService.currentUser());
    pageTitle = signal('Dashboard');
    notificationCount = signal(0);
    searchQuery = '';

    notificationItems: MenuItem[] = [];
    userMenuItems: MenuItem[] = [];

    private hubSub?: Subscription;

    constructor() {
        this.buildNotificationItems();
        this.buildUserMenuItems();
        this.updatePageTitle();

        this.router.events.pipe(
            filter(event => event instanceof NavigationEnd)
        ).subscribe(() => {
            this.updatePageTitle();
        });

        this.i18n.translationsLoaded$.subscribe(() => {
            this.buildNotificationItems();
            this.buildUserMenuItems();
            this.updatePageTitle();
        });
    }

    ngOnInit(): void {
        this.hubSub = this.notificationHub.saleNotification$.subscribe(evt => {
            this.onSaleNotification(evt);
        });
    }

    ngOnDestroy(): void {
        this.hubSub?.unsubscribe();
    }

    private onSaleNotification(evt: SaleNotificationEvent): void {
        const label = `${evt.soNumber} — ${evt.customerName} · ${evt.currency} ${evt.grandTotal.toFixed(2)}`;
        const time   = new Date(evt.occurredAt).toLocaleTimeString();

        // Prepend to notification dropdown
        this.notificationItems = [
            { label, icon: 'pi-check-circle', time, severity: 'success' },
            ...this.notificationItems
        ].slice(0, 10); // keep last 10

        this.notificationCount.update(n => n + 1);

        // Show a toast popup
        this.messageService.add({
            key:      'sale-notification',
            severity: 'success',
            summary:  '💰 New Sale',
            detail:   `${evt.soNumber} — ${evt.currency} ${evt.grandTotal.toFixed(2)}`,
            life:      6000
        });
    }

    private buildNotificationItems(): void {
        this.notificationItems = [
            {
                label: 'New stock alert',
                icon: 'pi-info-circle',
                time: '5 minutes ago'
            },
            {
                label: 'Low stock warning',
                icon: 'pi-exclamation-triangle',
                time: '1 hour ago',
                severity: 'warning'
            },
            {
                label: 'Order completed',
                icon: 'pi-check-circle',
                time: '2 hours ago',
                severity: 'success'
            }
        ];
    }

    private buildUserMenuItems(): void {
        this.userMenuItems = [
            {
                label: this.i18n.t('topbar.profile'),
                icon: 'pi pi-user',
                command: () => this.router.navigate(['/profile'])
            },
            {
                label: this.i18n.t('topbar.settings'),
                icon: 'pi pi-cog',
                command: () => this.router.navigate(['/settings'])
            },
            {
                separator: true
            },
            {
                label: this.i18n.t('topbar.documentation'),
                icon: 'pi pi-book',
                command: () => window.open('/docs', '_blank')
            },
            {
                label: this.i18n.t('topbar.support'),
                icon: 'pi pi-headphones',
                command: () => window.open('/support', '_blank')
            },
            {
                separator: true
            },
            {
                label: this.i18n.t('topbar.logout'),
                icon: 'pi pi-sign-out',
                command: () => this.logout()
            }
        ];
    }

    updatePageTitle() {
        const url = this.router.url;
        const titleKeyMap: { [key: string]: string } = {
            '/': 'pageTitles.dashboard',
            '/inventory/categories': 'pageTitles.categories',
            '/inventory/brands': 'pageTitles.brands',
            '/inventory/units': 'pageTitles.units',
            '/inventory/parts': 'pageTitles.parts',
            '/inventory/suppliers': 'pageTitles.suppliers',
            '/inventory/warehouses': 'pageTitles.warehouses',
            '/inventory/vehicles': 'pageTitles.vehicles',
            '/inventory/stock': 'pageTitles.stockManagement',
            '/procurement/purchase-orders': 'pageTitles.purchaseOrders',
            '/procurement/purchase-returns': 'pageTitles.purchaseReturns',
            '/procurement/goods-receipts': 'pageTitles.goodsReceipts',
            '/procurement/payment-providers': 'pageTitles.paymentProviders',
            '/procurement/supplier-payments': 'pageTitles.supplierPayments',
            '/sales/sales-orders': 'pageTitles.salesOrders',
            '/sales/invoices': 'pageTitles.invoices',
            '/sales/customers': 'pageTitles.customers',
            '/sales/technicians': 'pageTitles.technicians',
            '/sales/customer-payments': 'pageTitles.customerPayments',
            '/sales/sales-returns': 'pageTitles.salesReturns',
            '/audit/dashboard': 'pageTitles.auditTrail',
            '/audit/logs': 'pageTitles.auditTrail',
            '/admin-settings': 'pageTitles.adminSettings'
        };

        const key = titleKeyMap[url] || 'pageTitles.autoPartShop';
        this.pageTitle.set(this.i18n.t(key));
    }

    toggleDarkMode() {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
    }

    navigateToQuickSale() {
        this.router.navigate(['/quick-sale']);
    }

    navigateToQuickSaleShortcut() {
        this.router.navigate(['/quick-sale-shortcut']);
    }

    getUserInitials(): string {
        const user = this.currentUser();
        if (!user || !user.fullName) return '?';
        const names = user.fullName.split(' ');
        if (names.length >= 2) {
            return `${names[0][0]}${names[names.length - 1][0]}`.toUpperCase();
        }
        return names[0].substring(0, 2).toUpperCase();
    }

    onSearch() {
        if (this.searchQuery.trim()) {
            console.log('Searching for:', this.searchQuery);
            // Implement search functionality
        }
    }

    logout(): void {
        this.authService.logout();
        this.router.navigate(['/login']);
    }
}
