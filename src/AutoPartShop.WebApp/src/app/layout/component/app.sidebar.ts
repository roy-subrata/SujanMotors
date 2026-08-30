import { Component, DestroyRef, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { LayoutService } from '../service/layout.service';
import { AuthService } from '../../shared/services/auth.service';
import { AppBrandingService } from '../../shared/services/app-branding.service';
import { UserMenuService } from '../../shared/services/user-menu.service';
import { getUserInitials } from '../../shared/utils/user-display.util';
import { I18nService } from '../../shared/services/i18n.service';
import { CurrencyService } from '../../shared/services/currency.service';
import { TillSessionService, TillSessionResponse } from '../../features/sales/services/till-session.service';
import { AvatarModule } from 'primeng/avatar';
import { TooltipModule } from 'primeng/tooltip';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { AppMenuComponent } from './app-menu/app.menu.component';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

interface NavSearchResult {
    label: string;
    icon: string;
    routerLink: string[];
}

@Component({
    selector: 'app-sidebar',
    standalone: true,
    imports: [CommonModule, FormsModule, AppMenuComponent, AvatarModule, TooltipModule, MenuModule, TranslatePipe],
    templateUrl: './app.sidebar.html',
    styleUrl: './app.sidebar.scss',
})
export class AppSidebar implements OnInit {
    private layoutService = inject(LayoutService);
    private authService = inject(AuthService);
    private userMenuService = inject(UserMenuService);
    private i18n = inject(I18nService);
    private destroyRef = inject(DestroyRef);
    private router = inject(Router);
    private tillSessionService = inject(TillSessionService);
    private currencyService = inject(CurrencyService);
    protected branding = inject(AppBrandingService);

    @ViewChild(AppMenuComponent) appMenu?: AppMenuComponent;

    isCollapsed = computed(() => this.layoutService.layoutState().staticMenuDesktopInactive);
    currentUser = computed(() => this.authService.currentUser());

    userMenuItems: MenuItem[] = [];

    navQuery = signal('');
    navSearchOpen = signal(false);
    navResults = computed<NavSearchResult[]>(() => {
        const query = this.navQuery().trim().toLowerCase();
        if (!query) return [];
        return this.flattenNavItems()
            .filter(item => item.label.toLowerCase().includes(query))
            .slice(0, 8);
    });

    tillSession = signal<TillSessionResponse | null>(null);

    constructor(public el: ElementRef) {
        this.buildUserMenuItems();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildUserMenuItems();
        });
    }

    ngOnInit(): void {
        this.tillSessionService.getCurrent().subscribe({
            next: session => this.tillSession.set(session),
            error: () => this.tillSession.set(null)
        });
    }

    private buildUserMenuItems(): void {
        this.userMenuItems = this.userMenuService.buildMenuItems();
    }

    toggleSidebar() {
        this.layoutService.onMenuToggle();
    }

    onLogoError(event: Event) {
        (event.target as HTMLImageElement).style.display = 'none';
    }

    getUserInitials(): string {
        return getUserInitials(this.currentUser()?.fullName);
    }

    formatCurrency(value: number): string {
        return this.currencyService.formatCurrency(value, this.currencyService.selectedCurrency());
    }

    onNavSearchFocus(): void {
        this.navSearchOpen.set(true);
    }

    onNavSearchBlur(): void {
        // Delay so a click/mousedown on a result can still register before we close.
        setTimeout(() => this.navSearchOpen.set(false), 150);
    }

    closeNavSearch(): void {
        this.navQuery.set('');
        this.navSearchOpen.set(false);
    }

    onNavSearchEnter(): void {
        const first = this.navResults()[0];
        if (first) this.goToResult(first);
    }

    goToResult(result: NavSearchResult): void {
        this.router.navigate(result.routerLink);
        this.closeNavSearch();
    }

    private flattenNavItems(): NavSearchResult[] {
        const model = this.appMenu?.model ?? [];
        const results: NavSearchResult[] = [];
        for (const group of model) {
            if (group.visible === false) continue;
            if (group.routerLink) {
                results.push({ label: group.label ?? '', icon: group.icon ?? 'pi pi-circle', routerLink: group.routerLink as string[] });
            }
            for (const item of group.items ?? []) {
                if (item.visible === false) continue;
                if (item.routerLink) {
                    results.push({ label: item.label ?? '', icon: item.icon ?? 'pi pi-circle', routerLink: item.routerLink as string[] });
                }
            }
        }
        return results;
    }
}
