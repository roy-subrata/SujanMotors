import { Component, inject, computed, signal, OnInit, OnDestroy, ViewChild } from '@angular/core';
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
import { Popover } from 'primeng/popover';
import { LayoutService } from '../service/layout.service';
import { AuthService } from '../../shared/services/auth.service';
import { I18nService } from '../../shared/services/i18n.service';
import { AppBrandingService } from '../../shared/services/app-branding.service';
import { NotificationHubService, SaleNotificationEvent, ReorderAlertEvent } from '../../shared/services/notification-hub.service';
import { LanguageSwitcherComponent } from '../../shared/components/language-switcher/language-switcher.component';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';

interface StaffNotification {
    id: string;
    type: 'sale' | 'reorder';
    title: string;
    description: string;
    icon: string;
    occurredAt: Date;
    isRead: boolean;
    routerLink: string;
    queryParams?: Record<string, string>;
}

@Component({
    selector: 'app-topbar',
    standalone: true,
    imports: [RouterModule, CommonModule, FormsModule, StyleClassModule, TooltipModule, AvatarModule, MenuModule, BadgeModule, InputTextModule, LanguageSwitcherComponent, ToastModule, Popover],
    providers: [MessageService],
    template: `
    <p-toast position="top-right" key="sale-notification"></p-toast>
    <div class="layout-topbar">
        <!-- Mobile Menu Toggle -->
        <button class="layout-menu-button layout-topbar-action" (click)="layoutService.onMenuToggle()">
            <i class="pi pi-bars"></i>
        </button>

        <!-- Page Title -->
        <div class="topbar-title">
            <h1>{{ pageTitle() }}</h1>
        </div>

        <!-- Presentational global search (no search backend wired up yet — visual only,
             matches the design reference which is likewise a static, non-functional box) -->
        <div class="topbar-search" pTooltip="Search coming soon" tooltipPosition="bottom">
            <i class="pi pi-search"></i>
            <span class="topbar-search-placeholder">Search parts, invoices, customers…</span>
            <span class="topbar-search-kbd">⌘K</span>
        </div>

        <!-- Actions -->
        <div class="topbar-actions">
            <!-- Quick Sale -->
            <button
                type="button"
                class="quick-sale-btn"
                (click)="navigateToQuickSaleShortcut()"
                pTooltip="Quick Sale (POS)"
                tooltipPosition="bottom">
                <i class="pi pi-bolt"></i>
                <span>New Sale</span>
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
            <div class="notif-btn-wrap">
                <button
                    type="button"
                    class="topbar-action-btn"
                    pTooltip="Notifications"
                    tooltipPosition="bottom"
                    (click)="notifPanel.toggle($event)">
                    <i class="pi pi-bell"></i>
                </button>
                @if (unreadCount() > 0) {
                    <span class="notif-badge">{{ unreadCount() }}</span>
                }
            </div>

            <p-popover #notifPanel [style]="{'width': '380px'}">
                <div class="notif-panel">
                    <div class="notif-panel-header">
                        <span class="notif-panel-title">Notifications</span>
                        @if (unreadCount() > 0) {
                            <button type="button" class="text-btn" (click)="markAllAsRead()">Mark all read</button>
                        }
                    </div>

                    @if (notifications().length === 0) {
                        <div class="notif-empty">
                            <i class="pi pi-bell-slash"></i>
                            <p>No notifications yet</p>
                        </div>
                    } @else {
                        <div class="notif-list">
                            @for (n of notifications(); track n.id) {
                                <div class="notif-item" [class.unread]="!n.isRead" (click)="onNotifClick(n)">
                                    <div class="notif-icon-wrap">
                                        <i [class]="'pi ' + n.icon"></i>
                                    </div>
                                    <div class="notif-body">
                                        <p class="notif-title">{{ n.title }}</p>
                                        <p class="notif-desc">{{ n.description }}</p>
                                        <p class="notif-time">{{ timeAgo(n.occurredAt) }}</p>
                                    </div>
                                    @if (!n.isRead) {
                                        <span class="notif-dot"></span>
                                    }
                                </div>
                            }
                        </div>
                        <div class="notif-panel-footer">
                            <button type="button" class="text-btn small" (click)="clearAll()">Clear all</button>
                        </div>
                    }
                </div>
            </p-popover>

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

        ::ng-deep .p-menu {
            background: var(--surface-card);
            border: 1px solid var(--surface-border);
            border-radius: 6px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
            z-index: 9999 !important;
        }

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

        /* Notification button wrapper */
        .notif-btn-wrap {
            position: relative;
            display: inline-flex;
        }

        .notif-badge {
            position: absolute;
            top: 2px;
            right: 2px;
            background: var(--red-500, #ef4444);
            color: #fff;
            border-radius: 10px;
            min-width: 18px;
            height: 18px;
            font-size: 10px;
            font-weight: 700;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 0 4px;
            pointer-events: none;
            line-height: 1;
        }

        /* Notification popover panel */
        ::ng-deep .p-popover {
            border-radius: 8px;
            border: 1px solid var(--surface-border);
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
            overflow: hidden;
        }

        ::ng-deep .p-popover-content {
            padding: 0 !important;
        }

        .notif-panel {
            display: flex;
            flex-direction: column;
        }

        .notif-panel-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 0.875rem 1rem;
            border-bottom: 1px solid var(--surface-border);
        }

        .notif-panel-title {
            font-weight: 600;
            font-size: 0.9375rem;
            color: var(--text-color);
        }

        .text-btn {
            background: none;
            border: none;
            color: var(--primary-color);
            cursor: pointer;
            font-size: 0.8125rem;
            padding: 0;
        }

        .text-btn:hover {
            text-decoration: underline;
        }

        .text-btn.small {
            color: var(--text-color-secondary);
            font-size: 0.8125rem;
        }

        /* Empty state */
        .notif-empty {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: 2.5rem 1rem;
            gap: 0.5rem;
            color: var(--text-color-secondary);
        }

        .notif-empty i {
            font-size: 2rem;
            opacity: 0.4;
        }

        .notif-empty p {
            margin: 0;
            font-size: 0.875rem;
        }

        /* Notification list */
        .notif-list {
            max-height: 340px;
            overflow-y: auto;
        }

        .notif-item {
            display: flex;
            align-items: flex-start;
            gap: 0.75rem;
            padding: 0.875rem 1rem;
            cursor: pointer;
            transition: background-color 0.15s;
            position: relative;
        }

        .notif-item:not(:last-child) {
            border-bottom: 1px solid var(--surface-border);
        }

        .notif-item:hover {
            background: var(--surface-hover);
        }

        .notif-item.unread {
            background: color-mix(in srgb, var(--primary-color) 5%, transparent);
        }

        .notif-item.unread:hover {
            background: color-mix(in srgb, var(--primary-color) 10%, transparent);
        }

        .notif-icon-wrap {
            width: 36px;
            height: 36px;
            border-radius: 50%;
            background: color-mix(in srgb, var(--green-500, #22c55e) 15%, transparent);
            display: flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
        }

        .notif-icon-wrap i {
            font-size: 1rem;
            color: var(--green-600, #16a34a);
        }

        .notif-body {
            flex: 1;
            min-width: 0;
        }

        .notif-title {
            margin: 0 0 0.2rem;
            font-size: 0.875rem;
            font-weight: 600;
            color: var(--text-color);
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .notif-desc {
            margin: 0 0 0.2rem;
            font-size: 0.8125rem;
            color: var(--text-color-secondary);
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .notif-time {
            margin: 0;
            font-size: 0.75rem;
            color: var(--text-color-secondary);
            opacity: 0.7;
        }

        .notif-dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: var(--primary-color);
            flex-shrink: 0;
            margin-top: 4px;
        }

        .notif-panel-footer {
            display: flex;
            justify-content: center;
            padding: 0.625rem 1rem;
            border-top: 1px solid var(--surface-border);
        }

        /* Common topbar styles */
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
    private branding = inject(AppBrandingService);
    private notificationHub = inject(NotificationHubService);
    private messageService = inject(MessageService);

    @ViewChild('notifPanel') notifPanel!: Popover;

    currentUser = computed(() => this.authService.currentUser());
    pageTitle = signal('Dashboard');
    notifications = signal<StaffNotification[]>([]);
    unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);
    searchQuery = '';

    userMenuItems: MenuItem[] = [];

    private hubSub?: Subscription;
    private reorderSub?: Subscription;

    constructor() {
        this.buildUserMenuItems();
        this.updatePageTitle();

        this.router.events.pipe(
            filter(event => event instanceof NavigationEnd)
        ).subscribe(() => {
            this.updatePageTitle();
        });

        this.i18n.translationsLoaded$.subscribe(() => {
            this.buildUserMenuItems();
            this.updatePageTitle();
        });
    }

    ngOnInit(): void {
        this.hubSub = this.notificationHub.saleNotification$.subscribe(evt => {
            this.onSaleNotification(evt);
        });
        this.reorderSub = this.notificationHub.reorderAlert$.subscribe(evt => {
            this.onReorderAlert(evt);
        });
    }

    ngOnDestroy(): void {
        this.hubSub?.unsubscribe();
        this.reorderSub?.unsubscribe();
    }

    private onSaleNotification(evt: SaleNotificationEvent): void {
        const notif: StaffNotification = {
            id: crypto.randomUUID(),
            type: 'sale',
            title: `New Sale — ${evt.soNumber}`,
            description: `${evt.customerName} · ${evt.saleChannel} · ${evt.currency} ${evt.grandTotal.toFixed(2)}`,
            icon: 'pi-shopping-cart',
            occurredAt: new Date(evt.occurredAt),
            isRead: false,
            routerLink: '/sales/sales-orders/view',
            queryParams: { id: evt.salesOrderId }
        };

        this.notifications.update(ns => [notif, ...ns].slice(0, 20));

        this.messageService.add({
            key:      'sale-notification',
            severity: 'success',
            summary:  'New Sale',
            detail:   `${evt.soNumber} — ${evt.customerName} · ${evt.currency} ${evt.grandTotal.toFixed(2)}`,
            life:      6000
        });
    }

    private onReorderAlert(evt: ReorderAlertEvent): void {
        const plural = evt.itemCount === 1 ? 'item' : 'items';
        const topNames = evt.items.slice(0, 3).map(i => i.partName).join(', ');
        const more = evt.itemCount > 3 ? ` +${evt.itemCount - 3} more` : '';

        const notif: StaffNotification = {
            id: crypto.randomUUID(),
            type: 'reorder',
            title: `Low Stock — ${evt.itemCount} ${plural} to reorder`,
            description: `${topNames}${more}`,
            icon: 'pi-exclamation-triangle',
            occurredAt: new Date(evt.occurredAt),
            isRead: false,
            routerLink: '/inventory/stock',
            queryParams: { tab: 'low' }
        };

        this.notifications.update(ns => [notif, ...ns].slice(0, 20));

        this.messageService.add({
            key:      'sale-notification',
            severity: 'warn',
            summary:  'Low Stock',
            detail:   `${evt.itemCount} ${plural} at or below reorder level`,
            life:      8000
        });
    }

    onNotifClick(n: StaffNotification): void {
        this.notifications.update(ns =>
            ns.map(x => x.id === n.id ? { ...x, isRead: true } : x)
        );
        this.notifPanel.hide();
        this.router.navigate([n.routerLink], n.queryParams ? { queryParams: n.queryParams } : {});
    }

    markAllAsRead(): void {
        this.notifications.update(ns => ns.map(n => ({ ...n, isRead: true })));
    }

    clearAll(): void {
        this.notifications.set([]);
        this.notifPanel.hide();
    }

    timeAgo(date: Date): string {
        const seconds = Math.floor((Date.now() - date.getTime()) / 1000);
        if (seconds < 60) return 'just now';
        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) return `${minutes}m ago`;
        const hours = Math.floor(minutes / 60);
        if (hours < 24) return `${hours}h ago`;
        return `${Math.floor(hours / 24)}d ago`;
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
        const url = this.router.url.split('?')[0];

        // Leaf route -> menu translation key, kept in sync with app.menu.component.ts
        // so the topbar shows the same label as the navigation menu.
        const titleKeyMap: { [key: string]: string } = {
            '/': 'menu.dashboard',
            '/inventory/parts': 'menu.parts',
            '/inventory/categories': 'menu.categories',
            '/inventory/brands': 'menu.brands',
            '/inventory/units': 'menu.units',
            '/inventory/attribute-groups': 'menu.attributeGroups',
            '/inventory/discounts': 'menu.discounts',
            '/inventory/stock': 'menu.stockManagement',
            '/inventory/warehouses': 'menu.warehouses',
            '/inventory/suppliers': 'menu.suppliers',
            '/inventory/vehicles': 'menu.vehicles',
            '/procurement/purchase-orders': 'menu.purchaseOrders',
            '/procurement/goods-receipts': 'menu.goodsReceipts',
            '/procurement/purchase-returns': 'menu.purchaseReturns',
            '/procurement/supplier-payments': 'menu.supplierPayments',
            '/procurement/supplier-account-summary': 'menu.supplierStatements',
            '/procurement/daily-expenses': 'menu.dailyExpenses',
            '/procurement/payment-providers': 'menu.paymentProviders',
            '/sales/sales-orders': 'menu.salesOrders',
            '/sales/invoices': 'menu.invoices',
            '/sales/pending-deliveries': 'menu.pendingDeliveries',
            '/sales/sales-returns': 'menu.salesReturns',
            '/sales/customers': 'menu.customers',
            '/sales/customer-payments': 'menu.customerPayments',
            '/sales/customer-account-summary': 'menu.customerStatements',
            '/sales/technicians': 'menu.technicians',
            '/warranty/registrations': 'menu.warrantyRegistrations',
            '/warranty/claims': 'menu.warrantyClaims',
            '/finance/cash-book': 'menu.dailyCashBook',
            '/admin/company-profile': 'menu.companyProfile',
            '/admin/currencies': 'menu.currencies',
            '/admin/exchange-rates': 'menu.exchangeRates',
            '/admin/shop-policies': 'menu.shopPolicies',
            '/admin-settings': 'menu.settings',
            '/audit/dashboard': 'menu.auditDashboard',
            '/audit/logs': 'menu.auditTrail',
        };

        // Exact match, else inherit the nearest parent list page (e.g. /sales/sales-orders/create).
        let key = titleKeyMap[url];
        if (!key) {
            const prefix = Object.keys(titleKeyMap)
                .filter(k => k !== '/' && url.startsWith(k + '/'))
                .sort((a, b) => b.length - a.length)[0];
            if (prefix) key = titleKeyMap[prefix];
        }
        if (key) {
            this.pageTitle.set(this.i18n.t(key));
            return;
        }

        // Unmapped: humanize the last URL segment (e.g. /admin/foo-bar -> "Foo Bar").
        const segment = url.split('/').filter(Boolean).pop() ?? '';
        const humanized = segment.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
        this.pageTitle.set(humanized || this.branding.appName());
    }

    toggleDarkMode() {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
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
            // TODO: implement global search
        }
    }

    logout(): void {
        this.authService.logout();
        this.router.navigate(['/login']);
    }
}
