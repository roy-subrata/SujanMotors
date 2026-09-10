import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

export interface PosCartLine {
    name: string;
    localName?: string | null;
    unitLabel: string;
    qty: number;
    totalLabel: string;
    discountBadge?: string | null;
    belowCostBadge?: string | null;
    /** Only set when the part has more than one compatible unit of sale (e.g. Piece/Box) —
     *  renders a small unit selector under the unit-price line when present. */
    unitId?: string | null;
    unitOptions?: { id: string; label: string }[] | null;
}

/**
 * Cart panel (design_handoff_pos_sale §1c): customer bar, ticket header, the scrollable
 * line-item list with a stepper per row, the totals footer and the Charge button. The
 * `compact`/`veryCompact` inputs trim the customer bar, ticket header and footer to reclaim
 * vertical space for line items on a short (laptop-height) viewport — see
 * quick-sale-shortcut.component.ts's `compact`/`veryCompact` signals for the threshold
 * rationale (this used to be a footer-only `short` input; extended for laptop screens that
 * otherwise only show ~1 line item before scrolling).
 */
@Component({
    selector: 'app-pos-cart-panel',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './pos-cart-panel.component.html',
    styleUrl: './pos-cart-panel.component.css'
})
export class PosCartPanelComponent {
    customerAttached = input<boolean>(false);
    customerName = input<string>('');
    customerMeta = input<string>('');
    customerInitials = input<string>('+');
    ticketLabel = input<string>('');
    /** Shown appended to ticketLabel (" · Tech: <name>") only when a technician is assigned. */
    technicianLabel = input<string | null>(null);

    lines = input<PosCartLine[]>([]);

    subtotalLabel = input<string>('');
    itemCountLabel = input<string>('');
    hasDiscount = input<boolean>(false);
    discountLabel = input<string>('');
    discountAmountLabel = input<string>('');
    hasVat = input<boolean>(false);
    vatLabel = input<string>('');
    vatAmountLabel = input<string>('');
    totalLabel = input<string>('');

    chargeEnabled = input<boolean>(false);
    compact = input<boolean>(false);
    veryCompact = input<boolean>(false);

    stockCheckDisabled = input<boolean>(false);
    priceCheckDisabled = input<boolean>(false);
    quotationDisabled = input<boolean>(false);
    draftDisabled = input<boolean>(false);

    openCustomer = output<void>();
    clearCart = output<void>();
    decrementLine = output<number>();
    incrementLine = output<number>();
    removeLine = output<number>();
    unitChange = output<{ index: number; unitId: string }>();
    stockCheck = output<void>();
    priceCheck = output<void>();
    saveQuotation = output<void>();
    saveDraft = output<void>();
    charge = output<void>();

    trackLine(index: number): number {
        return index;
    }
}
