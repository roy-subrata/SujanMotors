import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AuthService } from './auth.service';
import { I18nService } from './i18n.service';

/**
 * Builds the Profile/Settings/Logout menu shown from both the topbar avatar
 * and the sidebar user block, so the two stay identical instead of drifting
 * as two separately hand-written arrays.
 */
@Injectable({ providedIn: 'root' })
export class UserMenuService {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);

  buildMenuItems(): MenuItem[] {
    return [
      {
        label: this.i18n.t('topbar.profile'),
        icon: 'pi pi-user',
        command: () => this.router.navigate(['/profile'])
      },
      {
        label: this.i18n.t('topbar.settings'),
        icon: 'pi pi-cog',
        command: () => this.router.navigate(['/settings'])
      },
      {
        separator: true
      },
      {
        label: this.i18n.t('topbar.documentation'),
        icon: 'pi pi-book',
        command: () => window.open('/docs', '_blank')
      },
      {
        label: this.i18n.t('topbar.support'),
        icon: 'pi pi-headphones',
        command: () => window.open('/support', '_blank')
      },
      {
        separator: true
      },
      {
        label: this.i18n.t('topbar.logout'),
        icon: 'pi pi-sign-out',
        command: () => this.logout()
      }
    ];
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
