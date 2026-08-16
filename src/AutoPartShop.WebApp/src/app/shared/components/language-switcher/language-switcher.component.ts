import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { I18nService } from '../../services/i18n.service';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'app-language-switcher',
    standalone: true,
    imports: [CommonModule, TooltipModule],
    template: `
        <div class="lang-pill" pTooltip="Change language" tooltipPosition="bottom">
            <button
                type="button"
                class="lang-tab"
                [class.active]="currentLanguageCode === 'en'"
                (click)="setLanguage('en')">
                EN
            </button>
            <button
                type="button"
                class="lang-tab"
                [class.active]="currentLanguageCode === 'bn'"
                (click)="setLanguage('bn')">
                বাংলা
            </button>
        </div>
    `,
    styles: [`
        .lang-pill {
            display: inline-flex;
            align-items: center;
            padding: 2px;
            height: 36px;
            border-radius: 8px;
            background: var(--bg);
            border: 1px solid var(--border);
            gap: 2px;
        }

        .lang-tab {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            height: 100%;
            padding: 0 10px;
            border: none;
            background: transparent;
            color: var(--text-color-secondary);
            font-size: 0.75rem;
            font-weight: 600;
            border-radius: 6px;
            cursor: pointer;
            transition: background-color 0.15s, color 0.15s;
        }

        .lang-tab.active {
            background: var(--text);
            color: var(--surface);
        }
    `]
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
