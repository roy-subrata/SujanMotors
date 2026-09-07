import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { SafeHtml } from '@angular/platform-browser';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

/**
 * On-screen 80mm thermal receipt preview (design_handoff_pos_sale §5a). Renders the exact same
 * HTML document the hidden-iframe printer uses (ThermalReceiptService.buildReceiptHtml()) inside
 * a sandboxed iframe so the preview can never drift from the real print output — see plan
 * §"Print previews (5)". The A4 tax invoice uses the existing (restyled) InvoicePreviewComponent
 * instead of this component — see plan for why.
 */
@Component({
    selector: 'app-pos-print-preview',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './pos-print-preview.component.html',
    styleUrl: './pos-print-preview.component.css'
})
export class PosPrintPreviewComponent {
    open = input<boolean>(false);
    receiptHtml = input<SafeHtml | null>(null);

    close = output<void>();
    switchToInvoice = output<void>();
    sendToPrinter = output<void>();

    onBackdropClick(): void {
        this.close.emit();
    }

    onPanelClick(event: MouseEvent): void {
        event.stopPropagation();
    }
}
