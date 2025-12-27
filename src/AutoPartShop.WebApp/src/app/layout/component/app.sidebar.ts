import { Component, ElementRef, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppMenu } from './app.menu';
import { LayoutService } from '../service/layout.service';
import { AuthService } from '../../shared/services/auth.service';
import { AvatarModule } from 'primeng/avatar';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'app-sidebar',
    standalone: true,
    imports: [CommonModule, AppMenu, AvatarModule, TooltipModule],
    template: `
        <div class="layout-sidebar" [class.collapsed]="isCollapsed()">
            <!-- Logo and Toggle -->
            <div class="sidebar-header">
                <div class="logo-container" [class.collapsed]="isCollapsed()">
                    <i class="pi pi-box logo-icon"></i>
                    @if (!isCollapsed()) {
                        
                    }
                    <span class="logo-text">Auto Part Shop</span>
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
                            [style]="{'background-color':'#667eea', 'color': '#ffffff'}">
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
            min-width: 36px !important;
            width: 36px !important;
            height: 36px !important;
        }
    `]
})
export class AppSidebar {
    private layoutService = inject(LayoutService);
    private authService = inject(AuthService);

    isCollapsed = computed(() => this.layoutService.layoutState().staticMenuDesktopInactive);
    currentUser = computed(() => this.authService.currentUser());

    constructor(public el: ElementRef) {}

    toggleSidebar() {
        this.layoutService.onMenuToggle();
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
