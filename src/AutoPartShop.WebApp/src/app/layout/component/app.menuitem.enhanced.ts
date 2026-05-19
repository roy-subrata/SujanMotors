import { Component, HostBinding, Input, computed, inject } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';
import { MenuItem } from 'primeng/api';
import { LayoutService } from '../service/layout.service';

/**
 * Enhanced Menu Item Component
 *
 * Features:
 * - Tooltips in collapsed sidebar mode
 * - Smooth expand/collapse animations
 * - Active route highlighting
 * - Nested menu support (unlimited depth)
 * - Icon badge support
 * - Hover effects
 * - Touch-friendly on mobile
 *
 * Usage:
 * Replace '[app-menuitem]' selector in app.menu.ts with this enhanced version
 */
@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: '[app-menuitem-enhanced]',
    standalone: true,
    imports: [CommonModule, RouterModule, RippleModule, TooltipModule],
    template: `
        <ng-container>
            <!-- Category Label (Root Menu Items) -->
            <div *ngIf="root && item.visible !== false" class="layout-menuitem-root-text">
                {{ item.label }}
            </div>

            <!-- Menu Item with Sub-items (No Router Link) -->
            <a
                *ngIf="(!item.routerLink || item.items) && item.visible !== false"
                [attr.href]="item.url"
                (click)="itemClick($event)"
                [ngClass]="item.styleClass"
                [class.disabled]="item.disabled"
                [attr.target]="item.target"
                [pTooltip]="showTooltip ? item.label : null"
                tooltipPosition="right"
                [tooltipOptions]="{ showDelay: 300 }"
                tabindex="0"
                pRipple>
                <!-- Icon with Badge Support -->
                <span class="menu-icon-wrapper">
                    <i [ngClass]="item.icon" class="layout-menuitem-icon"></i>
                    @if (item.badge) {
                        <span class="menu-badge" [ngClass]="'badge-' + item.badge.severity">
                            {{ item.badge.value }}
                        </span>
                    }
                </span>

                <!-- Menu Label -->
                <span class="layout-menuitem-text">{{ item.label }}</span>

                <!-- Submenu Toggle Arrow -->
                <i class="pi pi-fw pi-angle-down layout-submenu-toggler" *ngIf="item.items"></i>
            </a>

            <!-- Menu Item with Router Link (No Sub-items) -->
            <a
                *ngIf="item.routerLink && !item.items && item.visible !== false"
                (click)="itemClick($event)"
                [ngClass]="item.styleClass"
                [class.disabled]="item.disabled"
                [routerLink]="item.routerLink"
                routerLinkActive="active-route"
                [routerLinkActiveOptions]="item.routerLinkActiveOptions || {
                    paths: 'exact',
                    queryParams: 'ignored',
                    matrixParams: 'ignored',
                    fragment: 'ignored'
                }"
                [fragment]="item.fragment"
                [queryParamsHandling]="item.queryParamsHandling"
                [preserveFragment]="item.preserveFragment"
                [skipLocationChange]="item.skipLocationChange"
                [replaceUrl]="item.replaceUrl"
                [state]="item.state"
                [queryParams]="item.queryParams"
                [attr.target]="item.target"
                [pTooltip]="showTooltip ? item.label : null"
                tooltipPosition="right"
                [tooltipOptions]="{ showDelay: 300 }"
                tabindex="0"
                pRipple>
                <!-- Icon with Badge Support -->
                <span class="menu-icon-wrapper">
                    <i [ngClass]="item.icon" class="layout-menuitem-icon"></i>
                    @if (item.badge) {
                        <span class="menu-badge" [ngClass]="'badge-' + item.badge.severity">
                            {{ item.badge.value }}
                        </span>
                    }
                </span>

                <!-- Menu Label -->
                <span class="layout-menuitem-text">{{ item.label }}</span>
            </a>

            <!-- Nested Submenu -->
            <ul *ngIf="item.items && item.visible !== false" [@children]="submenuAnimation">
                <ng-template ngFor let-child let-i="index" [ngForOf]="item.items">
                    <li app-menuitem-enhanced [item]="child" [index]="i" [parentKey]="key" [class]="child.badgeClass"></li>
                </ng-template>
            </ul>
        </ng-container>
    `,
    styles: [`
        /* Enhanced menu icon wrapper for badge support */
        .menu-icon-wrapper {
            position: relative;
            display: inline-flex;
            align-items: center;
            justify-content: center;
        }

        /* Menu badge styling */
        .menu-badge {
            position: absolute;
            top: -4px;
            right: -4px;
            min-width: 16px;
            height: 16px;
            border-radius: 8px;
            background: var(--red-500);
            color: white;
            font-size: 0.625rem;
            font-weight: 700;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 0 4px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
            animation: badge-pulse 2s infinite;
        }

        @keyframes badge-pulse {
            0%, 100% {
                transform: scale(1);
            }
            50% {
                transform: scale(1.1);
            }
        }

        .menu-badge.badge-success {
            background: var(--green-500);
        }

        .menu-badge.badge-warning {
            background: var(--orange-500);
        }

        .menu-badge.badge-info {
            background: var(--blue-500);
        }

        .menu-badge.badge-danger {
            background: var(--red-500);
        }

        /* Disabled menu item styling */
        a.disabled {
            opacity: 0.5;
            cursor: not-allowed;
            pointer-events: none;
        }

        /* Enhanced hover effects */
        a:not(.disabled):hover {
            transform: translateX(2px);
        }

        /* Enhanced active state with animation */
        a.active-route {
            animation: menu-item-activate 0.3s ease;
        }

        @keyframes menu-item-activate {
            0% {
                transform: translateX(0);
            }
            50% {
                transform: translateX(4px);
            }
            100% {
                transform: translateX(0);
            }
        }

        /* Smooth transitions for all interactive elements */
        a,
        .layout-menuitem-icon,
        .layout-submenu-toggler {
            transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
        }

        /* Enhanced tooltip appearance */
        ::ng-deep .p-tooltip {
            backdrop-filter: blur(10px);
        }
    `],
    animations: [
        trigger('children', [
            state(
                'collapsed',
                style({
                    height: '0',
                    opacity: 0
                })
            ),
            state(
                'expanded',
                style({
                    height: '*',
                    opacity: 1
                })
            ),
            transition('collapsed <=> expanded', animate('400ms cubic-bezier(0.86, 0, 0.07, 1)'))
        ])
    ]
})
export class AppMenuitemEnhanced {
    @Input() item!: MenuItem;
    @Input() index!: number;
    @Input() @HostBinding('class.layout-root-menuitem') root!: boolean;
    @Input() parentKey!: string;

    private layoutService = inject(LayoutService);
    private router = inject(Router);

    active = false;
    menuSourceSubscription: Subscription;
    menuResetSubscription: Subscription;
    key: string = '';

    // Computed property to show tooltip only when sidebar is collapsed and not on hover
    showTooltip = computed(() => {
        return this.layoutService.layoutState().staticMenuDesktopInactive &&
               this.layoutService.isDesktop();
    });

    constructor() {
        this.menuSourceSubscription = this.layoutService.menuSource$.subscribe((value) => {
            Promise.resolve(null).then(() => {
                if (value.routeEvent) {
                    this.active = value.key === this.key || value.key.startsWith(this.key + '-') ? true : false;
                } else {
                    if (value.key !== this.key && !value.key.startsWith(this.key + '-')) {
                        this.active = false;
                    }
                }
            });
        });

        this.menuResetSubscription = this.layoutService.resetSource$.subscribe(() => {
            this.active = false;
        });

        this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe((params) => {
            if (this.item.routerLink) {
                this.updateActiveStateFromRoute();
            }
        });
    }

    ngOnInit() {
        this.key = this.parentKey ? this.parentKey + '-' + this.index : String(this.index);

        if (this.item.routerLink) {
            this.updateActiveStateFromRoute();
        }
    }

    updateActiveStateFromRoute() {
        let activeRoute = this.router.isActive(
            this.item.routerLink[0],
            { paths: 'exact', queryParams: 'ignored', matrixParams: 'ignored', fragment: 'ignored' }
        );

        if (activeRoute) {
            this.layoutService.onMenuStateChange({ key: this.key, routeEvent: true });
        }
    }

    itemClick(event: Event) {
        // Avoid processing disabled items
        if (this.item.disabled) {
            event.preventDefault();
            return;
        }

        // Execute command
        if (this.item.command) {
            this.item.command({ originalEvent: event, item: this.item });
        }

        // Toggle active state for items with sub-items
        if (this.item.items) {
            this.active = !this.active;
        }

        this.layoutService.onMenuStateChange({ key: this.key });
    }

    get submenuAnimation() {
        return this.root ? 'expanded' : this.active ? 'expanded' : 'collapsed';
    }

    @HostBinding('class.active-menuitem')
    get activeClass() {
        return this.active && !this.root;
    }

    ngOnDestroy() {
        if (this.menuSourceSubscription) {
            this.menuSourceSubscription.unsubscribe();
        }

        if (this.menuResetSubscription) {
            this.menuResetSubscription.unsubscribe();
        }
    }
}
