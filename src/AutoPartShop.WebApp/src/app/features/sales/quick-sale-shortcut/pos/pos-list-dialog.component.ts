import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

/**
 * Generic reusable list-overlay shell (design_handoff_pos_sale §2) — header/subtitle/close,
 * an optional filter input, a scrollable body and an optional footer. Row content is fully
 * projected by the caller (search results, stock rows, customer rows, discount options, the
 * returns table, the price-check card, etc.) rather than data-driven here — see the plan's
 * component table: "content projected/parameterized per use". Row/body visual styling lives in
 * the orchestrator's own (globally scoped, ViewEncapsulation.None) stylesheet since projected
 * content always keeps the *projecting* component's encapsulation, not this shell's.
 */
@Component({
    selector: 'app-pos-list-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './pos-list-dialog.component.html',
    styleUrl: './pos-list-dialog.component.css'
})
export class PosListDialogComponent {
    open = input<boolean>(false);
    title = input<string>('');
    subtitle = input<string>('');
    showFilter = input<boolean>(false);
    filterValue = input<string>('');
    filterPlaceholder = input<string>('');
    hasFooter = input<boolean>(false);
    /** Panel max-width in px — the design's list dialogs are all 720px; a couple of the restyled
     *  legacy dialogs (stock/returns tables) are given more room. */
    maxWidth = input<number>(720);

    filterValueChange = output<string>();
    filterEnter = output<void>();
    closed = output<void>();

    onBackdropClick(): void {
        this.closed.emit();
    }

    onPanelClick(event: MouseEvent): void {
        event.stopPropagation();
    }
}
