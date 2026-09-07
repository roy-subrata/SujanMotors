import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

export interface PosTenderRow {
    label: string;
    amountLabel: string;
}

export interface PosPayType {
    value: string;
    label: string;
    note: string;
    disabled: boolean;
}

export interface PosQuickCash {
    amount: number;
    label: string;
    changeNote: string;
    exact: boolean;
}

/**
 * Tender / payment overlay (design_handoff_pos_sale §3). Wired directly to the orchestrator's
 * existing payment signals — see plan §"Tender dialog (3)". The keypad is a purely presentational
 * cents-first pad; the orchestrator owns the raw digit string and derived amount.
 */
@Component({
    selector: 'app-pos-tender-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, TranslatePipe],
    templateUrl: './pos-tender-dialog.component.html',
    styleUrl: './pos-tender-dialog.component.css'
})
export class PosTenderDialogComponent {
    open = input<boolean>(false);
    customerSubtitle = input<string>('');
    dueLabel = input<string>('');
    totalLabel = input<string>('');
    tenders = input<PosTenderRow[]>([]);
    hasChange = input<boolean>(false);
    changeLabel = input<string>('');

    payTypes = input<PosPayType[]>([]);
    settled = input<boolean>(false);

    /** Optional transaction-reference field — shown only for payment types that need one for
     *  reconciliation (CARD/MOBILE_BANKING), per the orchestrator's requiresReference(). */
    referenceVisible = input<boolean>(false);
    referenceValue = input<string>('');
    referenceValueChange = output<string>();

    amountDisplay = input<string>('');
    amountEmpty = input<boolean>(true);
    keypadKeys: string[] = ['7', '8', '9', '4', '5', '6', '1', '2', '3', '0', '00', '⌫'];

    quickCash = input<PosQuickCash[]>([]);
    amountHint = input<string>('');
    canAddCash = input<boolean>(false);
    canAddCard = input<boolean>(false);

    completeEnabled = input<boolean>(false);
    completeLabel = input<string>('');

    close = output<void>();
    removeTender = output<number>();
    tenderType = output<string>();
    keypadPress = output<string>();
    quickCashTap = output<number>();
    addCash = output<void>();
    addCard = output<void>();
    completeSale = output<void>();

    onBackdropClick(): void {
        this.close.emit();
    }

    onPanelClick(event: MouseEvent): void {
        event.stopPropagation();
    }
}
