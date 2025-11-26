import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, ButtonModule, MenuModule],
  template: `
    <div class="topbar">
      <div class="topbar-left">
        <button
          pButton
          type="button"
          icon="pi pi-bars"
          class="p-button-rounded p-button-text p-button-lg"
          [style]="{'color': '#6b7280'}"
          pRipple
          pTooltip="Menu"
          tooltipPosition="bottom">
        </button>
        <div class="logo-section">
          <i class="pi pi-shopping-bag logo-icon"></i>
          <h1 class="logo-text">AutoPartShop</h1>
        </div>
      </div>

      <div class="topbar-right">
        <p-menu
          #userMenu
          [model]="userMenuItems"
          [popup]="true"
          appendTo="body">
        </p-menu>
        <button
          pButton
          type="button"
          icon="pi pi-user"
          class="p-button-rounded p-button-text p-button-lg"
          [style]="{'color': '#6b7280'}"
          (click)="userMenu.toggle($event)"
          pRipple
          pTooltip="Account"
          tooltipPosition="bottom">
        </button>
      </div>
    </div>
  `,
  styles: [`
    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.75rem 1.5rem;
      background: white;
      border-bottom: 2px solid #e5e7eb;
      height: 64px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    }

    :host ::ng-deep .dark .topbar {
      background: #1f2937;
      border-bottom-color: #374151;
    }

    .topbar-left,
    .topbar-right {
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }

    .logo-section {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-weight: 700;
      color: #111827;
    }

    .logo-icon {
      font-size: 1.75rem;
      color: #2563eb;
    }

    .logo-text {
      margin: 0;
      font-size: 1.125rem;
      letter-spacing: -0.5px;
      display: none;
    }

    @media (min-width: 640px) {
      .logo-text {
        display: block;
      }
    }

    :host ::ng-deep .dark .logo-section {
      color: white;
    }

    :host ::ng-deep .p-button-rounded.p-button-text:hover {
      background: #f3f4f6;
    }

    :host ::ng-deep .dark .p-button-rounded.p-button-text:hover {
      background: #374151;
    }
  `]
})
export class TopbarComponent {
  userMenuItems: MenuItem[] = [
    { label: 'Profile', icon: 'pi pi-user', command: () => {} },
    { label: 'Settings', icon: 'pi pi-cog', command: () => {} },
    { separator: true },
    { label: 'Logout', icon: 'pi pi-sign-out', command: () => {} }
  ];
}
