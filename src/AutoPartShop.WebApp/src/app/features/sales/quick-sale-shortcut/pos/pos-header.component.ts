import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

/**
 * POS header band (design_handoff_pos_sale §1a): logo mark, store identity, search trigger
 * (opens the Search list-dialog, F2), a couple of small utility icons (dark-mode toggle, exit),
 * a divider and the operator/context block.
 */
@Component({
    selector: 'app-pos-header',
    standalone: true,
    imports: [CommonModule, RouterLink, TranslatePipe],
    templateUrl: './pos-header.component.html',
    styleUrl: './pos-header.component.css'
})
export class PosHeaderComponent {
    storeName = input<string>('');
    searchPlaceholder = input<string>('');
    operatorLabel = input<string>('');
    contextLine = input<string>('');
    isDarkMode = input<boolean>(false);

    openSearch = output<void>();
    toggleDarkMode = output<void>();
}
