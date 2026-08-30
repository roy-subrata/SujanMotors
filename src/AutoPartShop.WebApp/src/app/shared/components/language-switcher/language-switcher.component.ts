import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { I18nService } from '../../services/i18n.service';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'app-language-switcher',
    standalone: true,
    imports: [CommonModule, TooltipModule],
    templateUrl: './language-switcher.component.html',
    styleUrl: './language-switcher.component.scss',
})
export class LanguageSwitcherComponent {
    private i18n = inject(I18nService);

    setLanguage(lang: 'en' | 'bn'): void {
        this.i18n.setLanguage(lang);
    }

    get currentLanguageCode(): string {
        return this.i18n.getCurrentLanguage();
    }
}
