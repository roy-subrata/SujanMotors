import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideAppInitializer, inject, isDevMode } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withEnabledBlockingInitialNavigation, withInMemoryScrolling } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import Aura from '@primeuix/themes/aura';
import { definePreset } from '@primeuix/themes';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService } from 'primeng/api';
import { appRoutes } from './app.routes';
import { authInterceptor } from './app/shared/interceptors/auth.interceptor';
import { I18nService } from './app/shared/services/i18n.service';
import { AppBrandingService } from './app/shared/services/app-branding.service';
import { firstValueFrom } from 'rxjs';

/**
 * Maps the "modern SaaS" data-page design tokens (see
 * design_handoff_pos_dashboard/README.md, assets/_data-page.scss) onto
 * PrimeNG's own semantic design tokens, so stock PrimeNG components
 * (buttons, tags, selects, inputs, cards…) pick up the same near-black/
 * slate palette everywhere — not just the hand-styled shell/dashboard
 * markup, but also pages like the Parts create/edit form and detail
 * view that lean on PrimeNG's own --p-* vars directly.
 * Dark mode is driven by the existing `.app-dark` class (darkModeSelector
 * below) — the same mechanism LayoutService already toggles.
 */
const AppPreset = definePreset(Aura, {
    semantic: {
        colorScheme: {
            light: {
                primary: { color: '#0f172a', contrastColor: '#ffffff', hoverColor: '#1e293b', activeColor: '#334155' },
                highlight: { background: '#0f172a', focusBackground: '#1e293b', color: '#ffffff', focusColor: '#ffffff' },
                text: { color: '#0f172a', hoverColor: '#0f172a', mutedColor: '#5b6472', hoverMutedColor: '#0f172a' },
                content: { background: '#ffffff', hoverBackground: '#fafafb', borderColor: '#e6e8ec', color: '{text.color}', hoverColor: '{text.hover.color}' }
            },
            dark: {
                primary: { color: '#e2e8f0', contrastColor: '#0f172a', hoverColor: '#cbd5e1', activeColor: '#94a3b8' },
                highlight: { background: '#e2e8f0', focusBackground: '#cbd5e1', color: '#0f172a', focusColor: '#0f172a' },
                text: { color: '#eef1f6', hoverColor: '#eef1f6', mutedColor: '#9aa4b5', hoverMutedColor: '#eef1f6' },
                content: { background: '#151922', hoverBackground: '#1a1f2a', borderColor: '#252b38', color: '{text.color}', hoverColor: '{text.hover.color}' }
            }
        }
    }
});

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(appRoutes, withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' }), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
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
        })
    ]
};
