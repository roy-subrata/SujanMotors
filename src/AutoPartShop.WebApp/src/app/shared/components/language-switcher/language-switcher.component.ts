import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { I18nService } from '../../services/i18n.service';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'app-language-switcher',
    standalone: true,
    imports: [CommonModule, TooltipModule],
    template: `
        <button
            type="button"
            class="topbar-action-btn"
            (click)="toggleLanguage()"
            [pTooltip]="currentLanguageDisplay"
            tooltipPosition="bottom">
            <i class="pi pi-globe"></i>
            <span class="lang-text">{{ currentLanguageCode }}</span>
        </button>
    `,
    styles: [`
        .topbar-action-btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 0.5rem;
            width: auto;
            height: 40px;
            padding: 0 12px;
            border: none;
            background: none;
            color: var(--text-color);
            cursor: pointer;
            border-radius: 6px;
            transition: background-color 0.15s, transform 0.15s;
        }

        .topbar-action-btn:hover {
            background: var(--surface-hover);
        }

        .topbar-action-btn i {
            font-size: 1.125rem;
        }

        .lang-text {
            font-size: 0.875rem;
            font-weight: 600;
        }
    `]
})
export class LanguageSwitcherComponent {
    private i18n = inject(I18nService);

    toggleLanguage(): void {
        const currentLang = this.i18n.getCurrentLanguage();
        const newLang = currentLang === 'en' ? 'bn' : 'en';
        this.i18n.setLanguage(newLang);
    }

    get currentLanguageCode(): string {
        return this.i18n.getCurrentLanguage().toUpperCase();
    }

    get currentLanguageDisplay(): string {
        return this.i18n.getCurrentLanguage() === 'en' ? 'Switch to Bangla' : 'ইংরেজিতে পরিবর্তন করুন';
    }
}
