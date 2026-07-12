import { Component, ElementRef, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';

import { LayoutService } from '../service/layout.service';
import { AuthService } from '../../shared/services/auth.service';
import { AppBrandingService } from '../../shared/services/app-branding.service';
import { AvatarModule } from 'primeng/avatar';
import { TooltipModule } from 'primeng/tooltip';
import { AppMenuComponent } from './app-menu/app.menu.component';

@Component({
    selector: 'app-sidebar',
    standalone: true,
    imports: [CommonModule, AppMenuComponent, AvatarModule, TooltipModule],
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
                    <div class="user-profile" [pTooltip]="isCollapsed() ? user.fullName : ''" tooltipPosition="right">
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
    protected branding = inject(AppBrandingService);

    isCollapsed = computed(() => this.layoutService.layoutState().staticMenuDesktopInactive);
    currentUser = computed(() => this.authService.currentUser());

    constructor(public el: ElementRef) {}

    toggleSidebar() {
        this.layoutService.onMenuToggle();
    }

    onLogoError(event: Event) {
        (event.target as HTMLImageElement).style.display = 'none';
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
}
