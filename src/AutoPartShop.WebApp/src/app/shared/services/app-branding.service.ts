import { inject, Injectable, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AppSettingsService, ShopProfile } from './app-settings.service';

/**
 * Holds the white-label application brand (name, logo, tagline) loaded once at
 * startup from the public shop profile, and keeps the browser tab title in sync.
 *
 * Defaults match the shipped fallbacks in index.html / the API, so the UI never
 * renders blank even before the profile request completes (or if it fails).
 */
@Injectable({ providedIn: 'root' })
export class AppBrandingService {
  private readonly settings = inject(AppSettingsService);
  private readonly title = inject(Title);

  readonly appName = signal('Auto Part Shop');
  readonly appLogoUrl = signal('assets/logo.png');
  readonly tagline = signal('');

  /** Full business identity (SHOP_* settings) for document/print headers. Null until loaded. */
  readonly profile = signal<ShopProfile | null>(null);

  /** Called from APP_INITIALIZER — loads the brand and applies the tab title. */
  initialize(): Observable<void> {
    return this.load();
  }

  /** Re-fetch after an admin saves the branding so the chrome updates without a reload. */
  refresh(): void {
    this.load().subscribe();
  }

  private load(): Observable<void> {
    return this.settings.getShopProfile().pipe(
      map((p) => {
        this.profile.set(p);
        if (p.appName) this.appName.set(p.appName);
        if (p.appLogoUrl) this.appLogoUrl.set(p.appLogoUrl);
        this.tagline.set(p.tagline ?? '');
        this.title.setTitle(this.appName());
      }),
      // Never block startup or throw on a branding failure — keep defaults.
      catchError(() => of(void 0)),
    );
  }
}
