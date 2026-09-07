import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

export interface PosReceiptRow {
    label: string;
    value: string;
    /** 'body' | 'green' | 'ink' — mapped to a CSS class. */
    tone: 'body' | 'green' | 'ink';
    bold: boolean;
}

/**
 * Sale-complete confirmation (design_handoff_pos_sale §4). Populated from the existing
 * onSubmit() success payload — see plan §"Receipt dialog (4)". The cart/payments/customer are
 * already reset by the time this is shown (the orchestrator resets synchronously on submit
 * success), so every exit here is a plain close — there's no "finalised" cart to protect.
 */
@Component({
    selector: 'app-pos-receipt-dialog',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './pos-receipt-dialog.component.html',
    styleUrl: './pos-receipt-dialog.component.css'
})
export class PosReceiptDialogComponent {
    open = input<boolean>(false);
    reference = input<string>('');
    rows = input<PosReceiptRow[]>([]);

    printThermal = output<void>();
    printInvoice = output<void>();
    nextCustomer = output<void>();
    close = output<void>();

    onBackdropClick(): void {
        this.close.emit();
    }

    onPanelClick(event: MouseEvent): void {
        event.stopPropagation();
    }
}
