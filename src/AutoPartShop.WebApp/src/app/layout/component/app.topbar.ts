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
import { InboxNotificationService, InboxNotification } from '../../features/notifications/inbox-notifications.service';
import { LanguageSwitcherComponent } from '../../shared/components/language-switcher/language-switcher.component';
import { UserMenuService } from '../../shared/services/user-menu.service';
import { getUserInitials } from '../../shared/utils/user-display.util';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';

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
    templateUrl: './app.topbar.html',
    styleUrl: './app.topbar.scss',
})
export class AppTopbar implements OnInit, OnDestroy {
    public layoutService = inject(LayoutService);
    private authService = inject(AuthService);
    private router = inject(Router);
    private i18n = inject(I18nService);
    private branding = inject(AppBrandingService);
    private notificationHub = inject(NotificationHubService);
    private inboxService = inject(InboxNotificationService);
    private messageService = inject(MessageService);
    private userMenuService = inject(UserMenuService);

    @ViewChild('notifPanel') notifPanel!: Popover;

    currentUser = computed(() => this.authService.currentUser());
    // Test and prod are built from otherwise-identical bundles (both `production: true`) —
    // this is the only visual cue distinguishing them, so staff don't mistake one for the other.
    isTestEnv = environment.envName === 'staging';
    pageTitle = signal('Dashboard');
    notifications = signal<StaffNotification[]>([]);
    inboxUnread = signal(0);
    // The inbox is the single source of truth for the badge: every broadcast (sale or reorder)
    // is persisted server-side, so counting the transient SignalR entries here too would double-count.
    unreadCount = computed(() => this.inboxUnread());
    searchQuery = '';

    userMenuItems: MenuItem[] = [];

    private hubSub?: Subscription;
    private reorderSub?: Subscription;
    private inboxSub?: Subscription;
    private routerEventsSub?: Subscription;
    private translationsSub?: Subscription;

    constructor() {
        this.buildUserMenuItems();
        this.updatePageTitle();

        this.routerEventsSub = this.router.events.pipe(
            filter(event => event instanceof NavigationEnd)
        ).subscribe(() => {
            this.updatePageTitle();
        });

        this.translationsSub = this.i18n.translationsLoaded$.subscribe(() => {
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
        this.inboxSub = this.inboxService.unreadCount$.subscribe(count => {
            this.inboxUnread.set(count);
        });
        this.reloadInbox();
    }

    ngOnDestroy(): void {
        this.hubSub?.unsubscribe();
        this.reorderSub?.unsubscribe();
        this.inboxSub?.unsubscribe();
        this.routerEventsSub?.unsubscribe();
        this.translationsSub?.unsubscribe();
    }

    private onSaleNotification(evt: SaleNotificationEvent): void {
        this.reloadInbox();

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

        this.reloadInbox();

        this.messageService.add({
            key:      'sale-notification',
            severity: 'warn',
            summary:  'Low Stock',
            detail:   `${evt.itemCount} ${plural} at or below reorder level`,
            life:      8000
        });
    }

    onNotifClick(n: StaffNotification): void {
        if (!n.isRead) {
            this.inboxService.markRead(n.id, true).subscribe({
                next: () => {
                    this.notifications.update(ns => ns.map(x => x.id === n.id ? { ...x, isRead: true } : x));
                    this.inboxService.refreshUnreadCount();
                },
                error: () => { /* non-critical */ }
            });
        }
        this.notifPanel.hide();
        this.router.navigate([n.routerLink], n.queryParams ? { queryParams: n.queryParams } : {});
    }

    markAllAsRead(): void {
        this.inboxService.markAllRead().subscribe({
            next: () => this.reloadInbox(),
            error: () => this.reloadInbox()
        });
    }

    openInbox(): void {
        this.notifPanel.hide();
        this.router.navigate(['/notifications']);
    }

    /** Pull the latest persisted inbox notifications into the bell dropdown. */
    private reloadInbox(): void {
        this.inboxService.getNotifications({ page: 1, pageSize: 20 }).subscribe({
            next: res => this.notifications.set(res.data.map(n => this.mapInboxToStaff(n))),
            error: () => { /* keep the current list */ }
        });
        this.inboxService.refreshUnreadCount();
    }

    private mapInboxToStaff(n: InboxNotification): StaffNotification {
        const reorder = n.type === 'REORDER_ALERT';
        return {
            id: n.id,
            type: reorder ? 'reorder' : 'sale',
            title: n.title,
            description: n.message,
            icon: reorder ? 'pi-exclamation-triangle' : 'pi-shopping-cart',
            occurredAt: new Date(n.createdDate),
            isRead: n.isRead,
            routerLink: n.routerLink || '/notifications',
            queryParams: n.queryParamsJson ? JSON.parse(n.queryParamsJson) : undefined
        };
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
        this.userMenuItems = this.userMenuService.buildMenuItems();
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
            '/inventory/stock-takes': 'menu.stockTake',
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
            '/admin-settings': 'menu.settings',
            '/audit/dashboard': 'menu.auditDashboard',
            '/audit/logs': 'menu.auditTrail',
            '/shortcuts': 'menu.shortcuts',
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
        return getUserInitials(this.currentUser()?.fullName);
    }

    onSearch() {
        if (this.searchQuery.trim()) {
            // TODO: implement global search
        }
    }
}
