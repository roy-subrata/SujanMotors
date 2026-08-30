import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideAppInitializer, inject, isDevMode } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withEnabledBlockingInitialNavigation, withInMemoryScrolling } from '@angular/router';
import { provideServiceWorker, SwUpdate } from '@angular/service-worker';
import Aura from '@primeuix/themes/aura';
import { definePreset } from '@primeuix/themes';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService } from 'primeng/api';
import { appRoutes } from './app.routes';
import { authInterceptor } from './app/shared/interceptors/auth.interceptor';
import { languageInterceptor } from './app/shared/interceptors/language.interceptor';
import { I18nService } from './app/shared/services/i18n.service';
import { AppBrandingService } from './app/shared/services/app-branding.service';
import { firstValueFrom } from 'rxjs';

/**
 * Maps the Sujan Motors theme design tokens (see
 * docs/design_handoff_sujan_motors_theme/README.md, assets/layout/_tokens.scss,
 * assets/_data-page.scss) onto PrimeNG's own semantic design tokens, so stock
 * PrimeNG components (buttons, tags, selects, inputs, cards…) pick up the same
 * orange accent palette everywhere — not just the hand-styled shell/dashboard
 * markup, but also pages like the Parts create/edit form and detail
 * view that lean on PrimeNG's own --p-* vars directly.
 * Dark mode is driven by the existing `.app-dark` class (darkModeSelector
 * below) — the same mechanism LayoutService already toggles.
 */
const AppPreset = definePreset(Aura, {
    semantic: {
        colorScheme: {
            light: {
                primary: { color: '#ea580c', contrastColor: '#ffffff', hoverColor: '#c2410c', activeColor: '#9a3412' },
                highlight: { background: '#ea580c', focusBackground: '#c2410c', color: '#ffffff', focusColor: '#ffffff' },
                text: { color: '#1c1f26', hoverColor: '#1c1f26', mutedColor: '#5b6472', hoverMutedColor: '#1c1f26' },
                content: { background: '#ffffff', hoverBackground: '#fafafb', borderColor: '#e7e9ee', color: '{text.color}', hoverColor: '{text.hover.color}' }
            },
            dark: {
                primary: { color: '#fb923c', contrastColor: '#1c1108', hoverColor: '#fdba74', activeColor: '#f97316' },
                highlight: { background: '#fb923c', focusBackground: '#fdba74', color: '#1c1108', focusColor: '#1c1108' },
                text: { color: '#eef1f6', hoverColor: '#eef1f6', mutedColor: '#9aa4b5', hoverMutedColor: '#eef1f6' },
                content: { background: '#151922', hoverBackground: '#1a1f2a', borderColor: '#252b38', color: '{text.color}', hoverColor: '{text.hover.color}' }
            }
        }
    }
});

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(appRoutes, withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' }), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch(), withInterceptors([authInterceptor, languageInterceptor])),
        provideAnimationsAsync(),
        // Root-level ConfirmationService so the auth interceptor can prompt a reload on
        // optimistic-concurrency (409 CONCURRENCY_CONFLICT) responses from any page.
        ConfirmationService,
        providePrimeNG({
            theme: { preset: AppPreset, options: { darkModeSelector: '.app-dark' } },
            zIndex: { modal: 1100, overlay: 1200, menu: 1200, tooltip: 1300 }
        }),
        provideAppInitializer(() => {
            const i18nService = inject(I18nService);
            return firstValueFrom(i18nService.initialize());
        }),
        provideAppInitializer(() => {
            const branding = inject(AppBrandingService);
            return firstValueFrom(branding.initialize());
        }),
        provideServiceWorker('ngsw-worker.js', {
            enabled: !isDevMode(),
            registrationStrategy: 'registerWhenStable:30000'
        }),
        // Service-worker update handling: without this, clients keep running the
        // previously-installed app version until every tab is closed. Poll for new
        // versions; when one is ready, ask before reloading so an in-progress sale
        // is never interrupted mid-entry.
        provideAppInitializer(() => {
            const updates = inject(SwUpdate);
            if (!updates.isEnabled) return;

            updates.versionUpdates.subscribe(evt => {
                if (evt.type === 'VERSION_READY') {
                    const activate = window.confirm(
                        'A new version of the app is available. Reload to update?\n\n' +
                        'অ্যাপের নতুন সংস্করণ এসেছে। আপডেট করতে রিলোড করবেন?'
                    );
                    if (activate) document.location.reload();
                }
            });

            setInterval(() => { updates.checkForUpdate().catch(() => { /* offline: retry next tick */ }); }, 5 * 60 * 1000);
        })
    ]
};
