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
  templateUrl: './keyboard-shortcuts.component.html',
  styleUrl: './keyboard-shortcuts.component.scss',
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
