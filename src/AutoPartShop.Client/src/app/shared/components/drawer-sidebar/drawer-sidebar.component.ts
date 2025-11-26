import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DrawerModule } from 'primeng/drawer';
import { MenuModule } from 'primeng/menu';
import { ButtonModule } from 'primeng/button';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-drawer',
  standalone: true,
  imports: [CommonModule, DrawerModule, MenuModule, ButtonModule],
  templateUrl: './drawer-sidebar.component.html',
  styleUrl: './drawer-sidebar.component.css'
})
export class DrawerSidebarComponent {
  sidebarVisible = signal(false);

  menuItems = signal<MenuItem[]>([
    {
      label: 'Dashboard',
      icon: 'pi pi-home',
      routerLink: ['/dashboard'],
      routerLinkActiveOptions: { exact: true }
    },
    {
      label: 'Inventory Management',
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
        },
        {
          label: 'Low Stock Alert',
          icon: 'pi pi-exclamation-circle',
          routerLink: ['/inventory/alerts']
        }
      ]
    },
    {
      label: 'Orders',
      icon: 'pi pi-shopping-cart',
      items: [
        {
          label: 'Create Order',
          icon: 'pi pi-plus',
          routerLink: ['/orders/new']
        },
        {
          label: 'All Orders',
          icon: 'pi pi-list',
          routerLink: ['/orders']
        },
        {
          label: 'Pending',
          icon: 'pi pi-clock',
          routerLink: ['/orders/pending']
        },
        {
          label: 'Completed',
          icon: 'pi pi-check-circle',
          routerLink: ['/orders/completed']
        }
      ]
    },
    {
      label: 'Suppliers',
      icon: 'pi pi-truck',
      items: [
        {
          label: 'All Suppliers',
          icon: 'pi pi-list',
          routerLink: ['/suppliers']
        },
        {
          label: 'Add Supplier',
          icon: 'pi pi-plus',
          routerLink: ['/suppliers/new']
        }
      ]
    },
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
          label: 'Analytics',
          icon: 'pi pi-chart-area',
          routerLink: ['/reports/analytics']
        }
      ]
    },
    { separator: true },
    {
      label: 'Settings',
      icon: 'pi pi-cog',
      items: [
        {
          label: 'Company Info',
          icon: 'pi pi-building',
          routerLink: ['/settings/company']
        },
        {
          label: 'Users',
          icon: 'pi pi-users',
          routerLink: ['/settings/users']
        },
        {
          label: 'Preferences',
          icon: 'pi pi-sliders-h',
          routerLink: ['/settings/preferences']
        }
      ]
    },
    {
      label: 'Help & Support',
      icon: 'pi pi-question-circle',
      routerLink: ['/help']
    }
  ]);

  toggleSidebar(): void {
    this.sidebarVisible.update(val => !val);
  }
}
