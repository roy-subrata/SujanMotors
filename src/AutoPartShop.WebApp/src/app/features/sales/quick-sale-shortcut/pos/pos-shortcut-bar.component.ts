import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface PosShortcutKey {
    key: string;
    label: string;
    action: string;
    disabled?: boolean;
}

export interface PosExtraAction {
    id: string;
    label: string;
    disabled?: boolean;
}

/**
 * Dark F-key shortcut strip (design_handoff_pos_sale §1d). The 8 spec F-keys plus a handful of
 * extra utility buttons (no key-cap slot in the design) are appended after them — the strip
 * already scrolls horizontally by design, so this is a non-breaking extension (see plan §1d).
 */
@Component({
    selector: 'app-pos-shortcut-bar',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pos-shortcut-bar.component.html',
    styleUrl: './pos-shortcut-bar.component.css'
})
export class PosShortcutBarComponent {
    shortcuts = input<PosShortcutKey[]>([]);
    extraActions = input<PosExtraAction[]>([]);
    statusLine = input<string>('');
    hideStatus = input<boolean>(false);

    shortcutClick = output<string>();
    extraActionClick = output<string>();
}
