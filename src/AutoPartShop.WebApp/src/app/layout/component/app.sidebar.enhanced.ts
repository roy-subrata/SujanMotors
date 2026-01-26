import { Component, ElementRef, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';

import { LayoutService } from '../service/layout.service';
import { AuthService } from '../../shared/services/auth.service';
import { AvatarModule } from 'primeng/avatar';
import { TooltipModule } from 'primeng/tooltip';
import { RippleModule } from 'primeng/ripple';
import { AppMenuComponent } from './app-menu/app.menu.component';

/**
 * Enhanced Sidebar Component
 *
 * Features:
 * - Collapsible sidebar with smooth animations
 * - Icon-only mode with hover expansion
 * - Tooltips for menu items in collapsed state
 * - User profile section at bottom
 * - Mobile-friendly overlay behavior
 * - Responsive design for all screen sizes
 *
 * Usage:
 * Replace 'app-sidebar' selector in app.layout.ts with this enhanced version
 */
@Component({
    selector: 'app-sidebar-enhanced',
    standalone: true,
    imports: [CommonModule, AppMenuComponent, AvatarModule, TooltipModule, RippleModule],
    template: `
        <div class="layout-sidebar" [class.collapsed]="isCollapsed()">
            <!-- Logo and Branding -->
            <div class="sidebar-header">
                <div class="logo-container" [class.collapsed]="isCollapsed()">
                    <!-- Logo Icon -->
                    <div class="logo-icon-wrapper">
                        <i class="pi pi-box logo-icon"></i>
                    </div>

                    <!-- App Name (hidden when collapsed) -->
                    @if (!isCollapsed()) {
                        <div class="logo-content">
                            <span class="logo-text">Auto Part Shop</span>
                            <span class="logo-subtitle">Management System</span>
                        </div>
                    }
                </div>

                <!-- Desktop Toggle Button (hidden - controlled via hamburger) -->
                <button
                    class="sidebar-toggle"
                    (click)="toggleSidebar()"
                    pRipple
                    [pTooltip]="isCollapsed() ? 'Expand Sidebar' : 'Collapse Sidebar'"
                    tooltipPosition="right">
                    <i [class]="isCollapsed() ? 'pi pi-angle-double-right' : 'pi pi-angle-double-left'"></i>
                </button>
            </div>

            <!-- Scrollable Menu Content -->
            <div class="sidebar-content">
                <app-menu></app-menu>
            </div>

            <!-- Bottom Section with User Profile -->
            <div class="sidebar-footer">
                <!-- Help & Support Link -->
                @if (!isCollapsed()) {
                    <div class="footer-links">
                        <a href="#" class="footer-link" pRipple>
                            <i class="pi pi-question-circle"></i>
                            <span>Help & Support</span>
                        </a>
                    </div>
                }

                <!-- User Profile -->
                @if (currentUser(); as user) {
                    <div
                        class="user-profile"
                        [pTooltip]="isCollapsed() ? user.fullName : ''"
                        tooltipPosition="right"
                        pRipple>
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
                            <i class="pi pi-ellipsis-v user-menu-icon"></i>
                        }
                    </div>
                }
            </div>
        </div>
    `,
    styles: [`
        /* Enhanced Avatar component styles */
        ::ng-deep .user-avatar {
            min-width: 40px !important;
            width: 40px !important;
            height: 40px !important;
            font-weight: 600;
            transition: transform 0.2s ease;
        }

        ::ng-deep .user-profile:hover .user-avatar {
            transform: scale(1.05);
        }

        /* Enhanced sidebar header */
        .sidebar-header {
            position: relative;
        }

        .logo-icon-wrapper {
            display: flex;
            align-items: center;
            justify-content: center;
            width: 48px;
            height: 48px;
            border-radius: 12px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
            transition: all 0.3s ease;
        }

        .logo-icon-wrapper .logo-icon {
            color: white;
            font-size: 1.5rem;
        }

        .logo-icon-wrapper:hover {
            transform: scale(1.05);
            box-shadow: 0 6px 16px rgba(102, 126, 234, 0.4);
        }

        .logo-content {
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
        }

        .logo-subtitle {
            font-size: 0.75rem;
            color: var(--text-color-secondary);
            font-weight: 400;
        }

        /* Enhanced footer links */
        .footer-links {
            padding: 0.5rem 0.75rem;
            border-bottom: 1px solid var(--surface-border);
        }

        .footer-link {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            padding: 0.625rem 0.75rem;
            color: var(--text-color-secondary);
            text-decoration: none;
            border-radius: 8px;
            transition: all 0.2s ease;
            font-size: 0.875rem;
        }

        .footer-link:hover {
            background: var(--surface-hover);
            color: var(--primary-color);
        }

        .footer-link i {
            font-size: 1.125rem;
        }

        /* Enhanced user profile */
        .user-profile {
            cursor: pointer;
            transition: background-color 0.2s ease;
        }

        .user-profile:hover {
            background: var(--surface-100) !important;
        }

        .user-menu-icon {
            color: var(--text-color-secondary);
            font-size: 1rem;
        }

        /* Smooth scroll behavior */
        .sidebar-content {
            scroll-behavior: smooth;
        }

        .sidebar-content::-webkit-scrollbar {
            width: 6px;
        }

        .sidebar-content::-webkit-scrollbar-track {
            background: transparent;
        }

        .sidebar-content::-webkit-scrollbar-thumb {
            background: var(--surface-border);
            border-radius: 3px;
            transition: background 0.2s ease;
        }

        .sidebar-content::-webkit-scrollbar-thumb:hover {
            background: var(--surface-300);
        }
    `]
})
export class AppSidebarEnhanced {
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
