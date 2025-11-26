import { Component, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface MenuItem {
  label: string;
  icon: string;
  path?: string;
  children?: MenuItem[];
  expanded?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="sidebar" [class.collapsed]="isCollapsed()">
      <nav class="sidebar-nav">
        <div *ngFor="let item of menuItems()" class="menu-item">
          <a
            *ngIf="!item.children; else hasChildren"
            [routerLink]="item.path"
            class="menu-link"
            [title]="item.label">
            <i [class]="'pi ' + item.icon"></i>
            <span class="menu-label">{{ item.label }}</span>
          </a>

          <ng-template #hasChildren>
            <button
              class="menu-link menu-toggle"
              (click)="toggleMenu(item)"
              [attr.aria-expanded]="item.expanded"
              [title]="item.label">
              <i [class]="'pi ' + item.icon"></i>
              <span class="menu-label">{{ item.label }}</span>
              <i class="pi pi-chevron-down toggle-icon"></i>
            </button>

            <div class="submenu" *ngIf="item.expanded">
              <a
                *ngFor="let child of item.children"
                [routerLink]="child.path"
                class="submenu-link"
                [title]="child.label">
                <i [class]="'pi ' + child.icon"></i>
                <span class="menu-label">{{ child.label }}</span>
              </a>
            </div>
          </ng-template>
        </div>
      </nav>
    </div>
  `,
  styles: [`
    .sidebar {
      width: 250px;
      background: white;
      border-right: 1px solid #e5e7eb;
      overflow-y: auto;
      transition: width 0.3s ease;
      height: calc(100vh - 64px);
    }

    :host ::ng-deep .dark .sidebar {
      background: #1f2937;
      border-right-color: #374151;
    }

    .sidebar.collapsed {
      width: 80px;
    }

    .sidebar-nav {
      padding: 1rem 0;
    }

    .menu-item {
      margin-bottom: 0.5rem;
      padding: 0 0.5rem;
    }

    .menu-link {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.75rem 1rem;
      color: #6b7280;
      text-decoration: none;
      border: none;
      background: none;
      cursor: pointer;
      font-size: 0.95rem;
      border-radius: 0.375rem;
      transition: all 0.2s ease;
      width: 100%;
      text-align: left;
    }

    .menu-link:hover {
      background: #f3f4f6;
      color: #111827;
    }

    .menu-link.menu-toggle {
      justify-content: space-between;
    }

    .menu-link i {
      flex-shrink: 0;
      font-size: 1.125rem;
      min-width: 1.5rem;
    }

    .menu-label {
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      flex: 1;
    }

    .toggle-icon {
      font-size: 0.875rem;
      transition: transform 0.2s ease;
    }

    .submenu {
      padding-left: 1.5rem;
      background: #fafafa;
    }

    .submenu-link {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.5rem 1rem;
      color: #6b7280;
      text-decoration: none;
      border-radius: 0.375rem;
      font-size: 0.9rem;
      transition: all 0.2s ease;
    }

    .submenu-link:hover {
      background: white;
      color: #2563eb;
    }

    .submenu-link i {
      font-size: 1rem;
    }

    :host ::ng-deep .dark .menu-link:hover {
      background: #374151;
      color: white;
    }

    :host ::ng-deep .dark .submenu {
      background: #111827;
    }

    :host ::ng-deep .dark .submenu-link:hover {
      background: #1f2937;
      color: #60a5fa;
    }

    :host ::ng-deep .dark .menu-link {
      color: #d1d5db;
    }

    :host ::ng-deep .dark .submenu-link {
      color: #d1d5db;
    }

    /* Collapsed state */
    .sidebar.collapsed .menu-label,
    .sidebar.collapsed .toggle-icon {
      display: none;
    }

    .sidebar.collapsed .menu-link {
      justify-content: center;
      padding: 0.75rem;
      width: 80px;
    }

    .sidebar.collapsed .submenu {
      display: none;
    }
  `]
})
export class SidebarComponent {
  isCollapsed = input(false);

  menuItems = signal<MenuItem[]>([
    {
      label: 'Dashboard',
      icon: 'pi-home',
      path: '/dashboard'
    },
    {
      label: 'Inventory',
      icon: 'pi-box',
      expanded: false,
      children: [
        { label: 'Categories', icon: 'pi-list', path: '/inventory/categories' },
        { label: 'Products', icon: 'pi-shopping-bag', path: '/inventory/products' },
        { label: 'Stock', icon: 'pi-warehouse', path: '/inventory/stock' }
      ]
    },
    {
      label: 'Orders',
      icon: 'pi-shopping-cart',
      expanded: false,
      children: [
        { label: 'New Order', icon: 'pi-plus', path: '/orders/new' },
        { label: 'All Orders', icon: 'pi-list', path: '/orders' },
        { label: 'Pending', icon: 'pi-clock', path: '/orders/pending' }
      ]
    },
    {
      label: 'Reports',
      icon: 'pi-chart-bar',
      expanded: false,
      children: [
        { label: 'Sales', icon: 'pi-chart-line', path: '/reports/sales' },
        { label: 'Inventory', icon: 'pi-chart-pie', path: '/reports/inventory' }
      ]
    },
    {
      label: 'Settings',
      icon: 'pi-cog',
      path: '/settings'
    }
  ]);

  toggleMenu(item: MenuItem) {
    item.expanded = !item.expanded;
  }
}
