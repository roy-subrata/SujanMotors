import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DrawerService } from '../../services/drawer.service';

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
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent implements OnInit {
  private drawerService = inject(DrawerService);

  isCollapsed = this.drawerService.isCollapsed;

  menuItems: MenuItem[] = [
    {
      label: 'Dashboard',
      icon: 'pi-home',
      path: '/dashboard',
      expanded: false
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
      path: '/settings',
      expanded: false
    }
  ];

  ngOnInit() {
    // Initialize as expanded on desktop
    if (typeof window !== 'undefined' && window.innerWidth >= 1024) {
      this.drawerService.setCollapsed(false);
    } else {
      this.drawerService.setCollapsed(true);
    }
  }

  toggleMenu(item: MenuItem) {
    if (item.children) {
      item.expanded = !item.expanded;
    }
  }

  toggleSidebar() {
    this.drawerService.toggleCollapse();
  }
}
