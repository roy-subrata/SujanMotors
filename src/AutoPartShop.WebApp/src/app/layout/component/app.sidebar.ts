import { Component, DestroyRef, ElementRef, computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';

import { LayoutService } from '../service/layout.service';
import { AuthService } from '../../shared/services/auth.service';
import { AppBrandingService } from '../../shared/services/app-branding.service';
import { UserMenuService } from '../../shared/services/user-menu.service';
import { getUserInitials } from '../../shared/utils/user-display.util';
import { I18nService } from '../../shared/services/i18n.service';
import { AvatarModule } from 'primeng/avatar';
import { TooltipModule } from 'primeng/tooltip';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { AppMenuComponent } from './app-menu/app.menu.component';

@Component({
    selector: 'app-sidebar',
    standalone: true,
    imports: [CommonModule, AppMenuComponent, AvatarModule, TooltipModule, MenuModule],
    template: `
        <div class="layout-sidebar" [class.collapsed]="isCollapsed()">
            <!-- Logo and Toggle -->
            <div class="sidebar-header">
                <div class="logo-container" [class.collapsed]="isCollapsed()">
                    @if (branding.appLogoUrl(); as logo) {
                        @if (!logo.startsWith('assets')) {
                            <img [src]="logo" alt="" class="logo-icon" style="object-fit: contain;" (error)="onLogoError($event)" />
                        } @else {
                            <i class="pi pi-box logo-icon"></i>
                        }
                    } @else {
                        <i class="pi pi-box logo-icon"></i>
                    }
                    <div class="logo-text-group">
                        <span class="logo-text">{{ branding.appName() }}</span>
                        <span class="logo-subtitle">{{ branding.tagline() || 'Auto Parts POS' }}</span>
                    </div>
                </div>
                <button
                    class="sidebar-toggle"
                    (click)="toggleSidebar()"
                    [pTooltip]="isCollapsed() ? 'Expand' : 'Collapse'"
                    tooltipPosition="right">
                    <i [class]="isCollapsed() ? 'pi pi-angle-right' : 'pi pi-angle-left'"></i>
                </button>
            </div>

            <!-- Menu -->
            <div class="sidebar-content">
                <app-menu></app-menu>
            </div>

            <!-- Bottom Section -->
            <div class="sidebar-footer">
                <!-- User Profile -->
                @if (currentUser(); as user) {
                    <div
                        class="user-profile"
                        [pTooltip]="isCollapsed() ? user.fullName : ''"
                        tooltipPosition="right"
                        (click)="userMenu.toggle($event)">
                        <p-avatar
                            [label]="getUserInitials()"
                            styleClass="user-avatar"
                            shape="circle"
                            [style]="{'background-color':'var(--surface2)', 'color': 'var(--text2)', 'border': '1px solid var(--border)'}">
                        </p-avatar>
                        @if (!isCollapsed()) {
                            <div class="user-info">
                                <span class="user-name">{{ user.fullName }}</span>
                                <span class="user-email">{{ user.email }}</span>
                            </div>
                        }
                    </div>
                    <p-menu #userMenu [model]="userMenuItems" [popup]="true" [appendTo]="'body'" [style]="{'min-width': '200px'}"></p-menu>
                }
            </div>
        </div>
    `,
    styles: [`
        /* Avatar component specific styles */
        ::ng-deep .user-avatar {
            min-width: 30px !important;
            width: 30px !important;
            height: 30px !important;
            font-size: 11px !important;
            font-weight: 600 !important;
        }
    `]
})
export class AppSidebar {
    private layoutService = inject(LayoutService);
    private authService = inject(AuthService);
    private userMenuService = inject(UserMenuService);
    private i18n = inject(I18nService);
    private destroyRef = inject(DestroyRef);
    protected branding = inject(AppBrandingService);

    isCollapsed = computed(() => this.layoutService.layoutState().staticMenuDesktopInactive);
    currentUser = computed(() => this.authService.currentUser());

    userMenuItems: MenuItem[] = [];

    constructor(public el: ElementRef) {
        this.buildUserMenuItems();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildUserMenuItems();
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
}
