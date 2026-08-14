import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { I18nService } from '@/shared/services/i18n.service';

interface ShortcutEntry {
  keys: string[];
  description: string;
}

interface ShortcutGroup {
  title: string;
  icon: string;
  entries: ShortcutEntry[];
}

/**
 * Static reference page documenting the keyboard behavior that actually exists in the
 * app today. There is no global hotkey system (confirmed: no document/window keydown
 * listeners anywhere in the app) — only these per-field/per-widget behaviors are real.
 */
@Component({
  selector: 'app-keyboard-shortcuts',
  standalone: true,
  imports: [CommonModule, PageContainerComponent, PageHeaderComponent],
  template: `
    <app-page-container>
      <app-page-header
        [title]="i18n.t('shortcuts.title')"
        [subtitle]="i18n.t('shortcuts.subtitle')">
      </app-page-header>

      <div class="shortcuts-page">
        <div class="shortcuts-note">
          <i class="pi pi-info-circle"></i>
          <span>{{ i18n.t('shortcuts.note') }}</span>
        </div>

        <div class="shortcut-groups">
          <div class="shortcut-group" *ngFor="let group of groups">
            <div class="shortcut-group-heading">
              <i [class]="group.icon"></i>
              <h2>{{ i18n.t(group.title) }}</h2>
            </div>
            <div class="shortcut-list">
              <div class="shortcut-row" *ngFor="let entry of group.entries">
                <span class="shortcut-desc">{{ i18n.t(entry.description) }}</span>
                <span class="shortcut-keys">
                  <ng-container *ngFor="let key of entry.keys; let last = last">
                    <kbd>{{ key }}</kbd>
                    <span *ngIf="!last" class="shortcut-plus">+</span>
                  </ng-container>
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </app-page-container>
  `,
  styles: [`
    .shortcuts-page {
      padding: 4px 20px 24px;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .shortcuts-note {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 14px;
      background: var(--color-info-light);
      color: var(--color-info);
      border-radius: var(--radius-md);
      font-size: 13px;
    }

    .shortcut-groups {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 16px;
    }

    .shortcut-group {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-md);
      padding: 16px;
    }

    .shortcut-group-heading {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
    }

    .shortcut-group-heading i {
      color: var(--color-primary);
      font-size: 16px;
    }

    .shortcut-group-heading h2 {
      margin: 0;
      font-size: 14px;
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .shortcut-list {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .shortcut-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 8px 0;
      border-bottom: 1px solid var(--color-border);
    }

    .shortcut-row:last-child {
      border-bottom: none;
    }

    .shortcut-desc {
      font-size: 13px;
      color: var(--color-text-secondary);
    }

    .shortcut-keys {
      display: flex;
      align-items: center;
      gap: 4px;
      flex-shrink: 0;
    }

    .shortcut-plus {
      font-size: 11px;
      color: var(--color-text-muted);
    }

    kbd {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 24px;
      padding: 3px 8px;
      font-family: inherit;
      font-size: 12px;
      font-weight: 600;
      color: var(--color-text-primary);
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border);
      border-radius: 6px;
      box-shadow: 0 1px 0 var(--color-border);
    }
  `]
})
export class KeyboardShortcutsComponent {
  readonly i18n = inject(I18nService);

  // title/description hold i18n keys, resolved at render time via i18n.t() in the
  // template so they react to language switches.
  readonly groups: ShortcutGroup[] = [
    {
      title: 'shortcuts.groups.search.title',
      icon: 'pi pi-search',
      entries: [
        { keys: ['Enter'], description: 'shortcuts.groups.search.submitSearch' },
        { keys: ['Enter'], description: 'shortcuts.groups.search.confirmLogin' },
      ]
    },
    {
      title: 'shortcuts.groups.navigation.title',
      icon: 'pi pi-compass',
      entries: [
        { keys: ['Type'], description: 'shortcuts.groups.navigation.filterSidebar' },
        { keys: ['Enter'], description: 'shortcuts.groups.navigation.goToFirstMatch' },
        { keys: ['Esc'], description: 'shortcuts.groups.navigation.closeSidebarSearch' },
      ]
    }
  ];
}
