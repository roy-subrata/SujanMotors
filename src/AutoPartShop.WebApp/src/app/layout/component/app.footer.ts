import { Component, inject } from '@angular/core';
import { AppBrandingService } from '../../shared/services/app-branding.service';

@Component({
        standalone: true,
        selector: 'app-footer',
        template: `
            <div class="layout-footer">
                <div class="footer-content">© {{ year }} {{ branding.appName() }}. All rights reserved.</div>
            </div>
        `
})
export class AppFooter {
    protected branding = inject(AppBrandingService);
    protected readonly year = new Date().getFullYear();
}
