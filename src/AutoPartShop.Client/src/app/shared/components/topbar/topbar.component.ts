import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { AvatarModule } from 'primeng/avatar';
import { MenuItem } from 'primeng/api';
import { RippleModule } from 'primeng/ripple';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, ButtonModule, MenuModule, AvatarModule, RippleModule],
  template: `
    <!-- Header -->
    <header class="sticky top-0 z-40 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 shadow-sm">
      <div class="px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">

          <!-- Left Section: Logo and Brand -->
          <div class="flex items-center gap-3 flex-1">
            <button
              pButton
              type="button"
              icon="pi pi-bars"
              class="p-button-rounded p-button-text p-button-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
              pRipple
              pTooltip="Menu"
              tooltipPosition="bottom"
              tooltipStyleClass="dark:bg-gray-900">
            </button>

            <div class="flex items-center gap-2">
              <div class="flex items-center justify-center w-10 h-10 rounded-lg bg-blue-600 dark:bg-blue-500 shadow-md">
                <i class="pi pi-shopping-bag text-white text-lg"></i>
              </div>
              <div class="hidden sm:block">
                <h1 class="text-lg font-bold text-gray-900 dark:text-white tracking-tight">AutoPartShop</h1>
                <p class="text-xs text-gray-500 dark:text-gray-400">Inventory Management</p>
              </div>
            </div>
          </div>

          <!-- Right Section: User Menu and Profile -->
          <div class="flex items-center gap-2 sm:gap-4">
            <!-- Notification Bell -->
            <button
              pButton
              type="button"
              icon="pi pi-bell"
              class="p-button-rounded p-button-text text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 relative"
              pRipple
              pTooltip="Notifications"
              tooltipPosition="bottom"
              tooltipStyleClass="dark:bg-gray-900">
              <span class="absolute top-2 right-2 inline-flex items-center justify-center px-2 py-1 text-xs font-bold leading-none text-white transform translate-x-1 -translate-y-1 bg-red-600 rounded-full">3</span>
            </button>

            <!-- Divider -->
            <div class="hidden sm:block w-px h-6 bg-gray-200 dark:bg-gray-700"></div>

            <!-- User Profile Dropdown -->
            <button
              #userMenuBtn
              pButton
              type="button"
              class="p-button-rounded p-button-text flex items-center gap-2 px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-700"
              (click)="userMenu.toggle($event)"
              pRipple>
              <p-avatar
                image="https://primefaces.org/cdn/primeng/images/demo/avatar/amyelsner.png"
                shape="circle"
                size="normal"
                styleClass="w-8 h-8">
              </p-avatar>
              <span class="hidden sm:inline text-sm font-medium text-gray-700 dark:text-gray-300">John Doe</span>
              <i class="pi pi-chevron-down text-xs text-gray-500 dark:text-gray-400 hidden sm:inline"></i>
            </button>

            <p-menu
              #userMenu
              [model]="userMenuItems"
              [popup]="true"
              appendTo="body"
              styleClass="dark:bg-gray-800">
            </p-menu>
          </div>
        </div>
      </div>
    </header>
  `
})
export class TopbarComponent {
  userMenuItems: MenuItem[] = [
    {
      label: 'Profile',
      icon: 'pi pi-user',
      command: () => console.log('Profile clicked')
    },
    {
      label: 'Settings',
      icon: 'pi pi-cog',
      command: () => console.log('Settings clicked')
    },
    {
      label: 'Preferences',
      icon: 'pi pi-sliders-h',
      command: () => console.log('Preferences clicked')
    },
    { separator: true },
    {
      label: 'Help & Support',
      icon: 'pi pi-question-circle',
      command: () => console.log('Help clicked')
    },
    { separator: true },
    {
      label: 'Logout',
      icon: 'pi pi-sign-out',
      styleClass: 'text-red-600 dark:text-red-400',
      command: () => console.log('Logout clicked')
    }
  ];
}
