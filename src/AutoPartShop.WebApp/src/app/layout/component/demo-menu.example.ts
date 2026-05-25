/**
 * Demo Menu Configuration
 *
 * This file demonstrates all the features of the enhanced menu system
 * including icons, badges, nested menus, and routing.
 *
 * Copy and modify this in your app.menu.ts to use these features.
 */

import { MenuItem } from 'primeng/api';

export const DEMO_MENU_ITEMS: MenuItem[] = [
    // Dashboard - Simple menu item
    {
        label: 'Dashboard',
        icon: 'pi pi-home',
        routerLink: ['/'],
        styleClass: 'menu-item-dashboard'
    },

    // Inventory - Menu with sub-items
    {
        label: 'Inventory',
        icon: 'pi pi-box',
        items: [
            {
                label: 'Categories',
                icon: 'pi pi-list',
                routerLink: ['/inventory/categories']
            },
            {
                label: 'Products',
                icon: 'pi pi-shopping-bag',
                routerLink: ['/inventory/products']
            },
            {
                label: 'Stock Management',
                icon: 'pi pi-warehouse',
                routerLink: ['/inventory/stock']
            }
        ]
    },

    // Sales - Menu with badge on parent
    {
        label: 'Sales',
        icon: 'pi pi-shopping-cart',
        badge: '12',
        items: [
            {
                label: 'New Orders',
                icon: 'pi pi-plus-circle',
                routerLink: ['/sales/orders/new'],
                badge: '5'
            },
            {
                label: 'Orders',
                icon: 'pi pi-file',
                routerLink: ['/sales/orders']
            },
            {
                label: 'Customers',
                icon: 'pi pi-users',
                routerLink: ['/sales/customers']
            },
            {
                label: 'Invoices',
                icon: 'pi pi-file-check',
                routerLink: ['/sales/invoices']
            }
        ]
    },

    // Procurement - Nested menus (3 levels)
    {
        label: 'Procurement',
        icon: 'pi pi-briefcase',
        items: [
            {
                label: 'Purchase Orders',
                icon: 'pi pi-list',
                items: [
                    {
                        label: 'Create New',
                        icon: 'pi pi-plus',
                        routerLink: ['/procurement/purchase-orders/new']
                    },
                    {
                        label: 'View All',
                        icon: 'pi pi-eye',
                        routerLink: ['/procurement/purchase-orders']
                    },
                    {
                        label: 'Pending',
                        icon: 'pi pi-clock',
                        routerLink: ['/procurement/purchase-orders/pending'],
                        badge: '3'
                    }
                ]
            },
            {
                label: 'Suppliers',
                icon: 'pi pi-truck',
                routerLink: ['/procurement/suppliers']
            },
            {
                label: 'Payments',
                icon: 'pi pi-money-bill',
                routerLink: ['/procurement/payments']
            }
        ]
    },

    // Separator
    {
        separator: true
    },

    // Reports - With multiple badges
    {
        label: 'Reports',
        icon: 'pi pi-chart-bar',
        items: [
            {
                label: 'Sales Report',
                icon: 'pi pi-chart-line',
                routerLink: ['/reports/sales']
            },
            {
                label: 'Inventory Report',
                icon: 'pi pi-chart-pie',
                routerLink: ['/reports/inventory']
            },
            {
                label: 'Financial Report',
                icon: 'pi pi-dollar',
                routerLink: ['/reports/financial'],
                badge: 'NEW'
            }
        ]
    },

    // Settings - Restricted menu item
    {
        label: 'Settings',
        icon: 'pi pi-cog',
        visible: true, // Set to false to hide, or use: this.hasAdminRole()
        items: [
            {
                label: 'General',
                icon: 'pi pi-sliders-h',
                routerLink: ['/settings/general']
            },
            {
                label: 'Users & Roles',
                icon: 'pi pi-users',
                routerLink: ['/settings/users']
            },
            {
                label: 'Security',
                icon: 'pi pi-shield',
                routerLink: ['/settings/security']
            }
        ]
    },

    // External link example
    {
        label: 'Help & Support',
        icon: 'pi pi-question-circle',
        items: [
            {
                label: 'Documentation',
                icon: 'pi pi-book',
                url: 'https://docs.example.com',
                target: '_blank'
            },
            {
                label: 'Contact Support',
                icon: 'pi pi-headphones',
                command: () => {
                    // Open support modal or navigate to support page
                    console.log('Opening support...');
                }
            },
            {
                label: 'About',
                icon: 'pi pi-info-circle',
                routerLink: ['/about']
            }
        ]
    }
];

/**
 * Example: Using Dynamic Badge Values
 *
 * Update badge values based on real-time data:
 */
export class DynamicMenuExample {
    private notificationCount = 0;
    private pendingOrders = 0;

    updateMenuBadges(menu: MenuItem[]): MenuItem[] {
        return menu.map(item => {
            // Update notification badge
            if (item.label === 'Notifications') {
                return {
                    ...item,
                    badge: this.notificationCount.toString()
                };
            }

            // Update pending orders badge
            if (item.label === 'Pending') {
                return {
                    ...item,
                    badge: this.pendingOrders.toString()
                };
            }

            // Recursively update nested items
            if (item.items) {
                return {
                    ...item,
                    items: this.updateMenuBadges(item.items)
                };
            }

            return item;
        });
    }
}

/**
 * Example: Conditional Menu Items Based on User Role
 */
export class RoleBasedMenuExample {
    constructor(private userRole: string) {}

    buildMenu(): MenuItem[] {
        const baseMenu: MenuItem[] = [
            {
                label: 'Dashboard',
                icon: 'pi pi-home',
                routerLink: ['/']
            },
            {
                label: 'My Tasks',
                icon: 'pi pi-check-square',
                routerLink: ['/tasks']
            }
        ];

        // Add admin-only items
        if (this.userRole === 'admin') {
            baseMenu.push({
                label: 'Administration',
                icon: 'pi pi-shield',
                items: [
                    {
                        label: 'User Management',
                        icon: 'pi pi-users',
                        routerLink: ['/admin/users']
                    },
                    {
                        label: 'System Settings',
                        icon: 'pi pi-cog',
                        routerLink: ['/admin/settings']
                    }
                ]
            });
        }

        // Add manager-only items
        if (this.userRole === 'manager' || this.userRole === 'admin') {
            baseMenu.push({
                label: 'Reports',
                icon: 'pi pi-chart-bar',
                routerLink: ['/reports']
            });
        }

        return baseMenu;
    }
}

/**
 * Example: Menu with Custom Commands
 */
export const MENU_WITH_COMMANDS: MenuItem[] = [
    {
        label: 'Quick Actions',
        icon: 'pi pi-bolt',
        items: [
            {
                label: 'Quick Sale',
                icon: 'pi pi-shopping-cart',
                command: (event) => {
                    // Open quick sale modal
                    console.log('Opening quick sale...', event);
                }
            },
            {
                label: 'New Customer',
                icon: 'pi pi-user-plus',
                command: (event) => {
                    // Open new customer form
                    console.log('Opening customer form...', event);
                }
            },
            {
                label: 'Export Data',
                icon: 'pi pi-download',
                command: (event) => {
                    // Trigger data export
                    console.log('Exporting data...', event);
                }
            }
        ]
    }
];

/**
 * Example: Menu Item Styles
 */
export const STYLED_MENU_ITEMS: MenuItem[] = [
    {
        label: 'Urgent',
        icon: 'pi pi-exclamation-triangle',
        styleClass: 'menu-item-urgent',
        routerLink: ['/urgent']
        // Add custom CSS class:
        // .menu-item-urgent { background: #fee; color: #c00; }
    },
    {
        label: 'Important',
        icon: 'pi pi-star',
        styleClass: 'menu-item-important',
        routerLink: ['/important']
        // Add custom CSS class:
        // .menu-item-important { background: #ffc; color: #860; }
    }
];

/**
 * Example: Disabled Menu Items
 */
export const MENU_WITH_DISABLED_ITEMS: MenuItem[] = [
    {
        label: 'Coming Soon',
        icon: 'pi pi-sparkles',
        disabled: true,
        items: [
            {
                label: 'Feature A',
                icon: 'pi pi-star',
                disabled: true
            },
            {
                label: 'Feature B',
                icon: 'pi pi-star',
                disabled: true
            }
        ]
    }
];
