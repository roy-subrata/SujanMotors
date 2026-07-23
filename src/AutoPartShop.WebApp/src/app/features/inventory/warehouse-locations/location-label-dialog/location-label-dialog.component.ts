import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import QRCode from 'qrcode';
import { BarcodeService } from '../../services/barcode.service';
import { WarehouseLocationResponse } from '../../services/warehouse-location.service';
import { getZoneColor } from '../zone-color';
import { LocationLabelMarkupOptions, LOCATION_LABEL_CSS, buildLocationLabelMarkup } from './location-label-template';

const LABEL_STYLE_ELEMENT_ID = 'wl-location-label-styles';

/**
 * Print dialog for the Warehouse Location barcode label (100mm x 50mm).
 * Opened via `DialogService.open(LocationLabelDialogComponent, { data: { location } })`
 * — same `DynamicDialog` mechanism the product `BarcodeDialogComponent` uses.
 */
@Component({
    selector: 'app-location-label-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, ToastModule],
    providers: [MessageService],
    templateUrl: './location-label-dialog.component.html',
    styleUrls: ['./location-label-dialog.component.css']
})
export class LocationLabelDialogComponent implements OnInit {
    private readonly barcodeService = inject(BarcodeService);
    private readonly messageService = inject(MessageService);
    private readonly dialogRef = inject(DynamicDialogRef);
    private readonly dialogConfig = inject(DynamicDialogConfig);
    private readonly sanitizer = inject(DomSanitizer);

    location: WarehouseLocationResponse | null = null;

    /** Rendered label HTML — single source of truth, shared with print. */
    labelHtmlSafe: SafeHtml | null = null;
    isGenerating = false;

    quantity = 1;
    maxQuantity = 200;

    readonly widthMm = 100;
    readonly heightMm = 50;

    private qrSvg = '';
    private barcodeSvg = '';

    // ── Preview scaling (label renders at exact physical mm; scale to fit the panel) ──
    private readonly MM_TO_PX = 96 / 25.4;
    private readonly PREVIEW_MAX_W = 420;
    private readonly PREVIEW_MAX_H = 230;

    get previewScale(): number {
        const w = this.widthMm * this.MM_TO_PX;
        const h = this.heightMm * this.MM_TO_PX;
        if (!w || !h) return 1;
        return Math.min(1, this.PREVIEW_MAX_W / w, this.PREVIEW_MAX_H / h);
    }

    get previewBox(): { w: number; h: number } {
        const s = this.previewScale;
        return { w: this.widthMm * this.MM_TO_PX * s, h: this.heightMm * this.MM_TO_PX * s };
    }

    ngOnInit(): void {
        this.location = (this.dialogConfig.data?.location as WarehouseLocationResponse) ?? null;
        if (!this.location) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Missing Data',
                detail: 'No warehouse location provided for label generation'
            });
            return;
        }

        this.ensureLabelStyles();
        void this.generateLabel();
    }

    /** Inject the canonical label CSS into <head> once (preview = print). */
    private ensureLabelStyles(): void {
        if (document.getElementById(LABEL_STYLE_ELEMENT_ID)) return;
        const style = document.createElement('style');
        style.id = LABEL_STYLE_ELEMENT_ID;
        style.textContent = LOCATION_LABEL_CSS;
        document.head.appendChild(style);
    }

    private labelOptions(): LocationLabelMarkupOptions {
        const loc = this.location!;
        return {
            zone: loc.zone,
            aisle: loc.aisle,
            rack: loc.rack,
            bin: loc.bin,
            locationCode: loc.locationCode,
            categoryName: loc.categoryName,
            zoneColor: getZoneColor(loc.zone),
            qrSvg: this.qrSvg,
            barcodeSvg: this.barcodeSvg
        };
    }

    private async generateLabel(): Promise<void> {
        if (!this.location) return;
        this.isGenerating = true;

        try {
            const code = this.location.locationCode;

            this.qrSvg = await QRCode.toString(code, { type: 'svg', margin: 0, errorCorrectionLevel: 'M' });
            this.barcodeSvg = this.barcodeService.generateLinearBarcode({
                type: 'code128',
                value: code,
                displayText: '',
                width: 2,
                height: 60
            });

            this.rebuildLabel();
        } catch (error) {
            console.error('Error generating location label:', error);
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to generate label' });
        } finally {
            this.isGenerating = false;
        }
    }

    private rebuildLabel(): void {
        const html = buildLocationLabelMarkup(this.labelOptions());
        this.labelHtmlSafe = this.sanitizer.bypassSecurityTrustHtml(html);
    }

    validateQuantity(): void {
        if (this.quantity < 1) this.quantity = 1;
        else if (this.quantity > this.maxQuantity) this.quantity = this.maxQuantity;
    }

    /** Print (single or bulk) — reuses the exact preview markup, same pattern as BarcodeDialogComponent.printBarcode(). */
    printLabel(): void {
        if (!this.location) return;

        try {
            const printWindow = window.open('', '', 'width=900,height=650');
            if (!printWindow) {
                throw new Error('Could not open print window');
            }

            const labelHtml = buildLocationLabelMarkup(this.labelOptions());

            const htmlContent = `<!DOCTYPE html>
<html>
<head>
    <title>${this.escapeHtml(this.location.locationCode)} — Location Labels</title>
    <style>
        @page { size: ${this.widthMm}mm ${this.heightMm}mm; margin: 0; }
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { background: #fff; }
        .wl-sheet { display: flex; flex-wrap: wrap; gap: 2mm; padding: 2mm; }
        .wl-label-slot { page-break-inside: avoid; break-inside: avoid; }
        ${LOCATION_LABEL_CSS}
        @media print {
            body { background: #fff; }
            .wl-sheet { gap: 0; padding: 0; }
            .wl-label-slot { page-break-after: always; }
            .wl-label-slot:last-child { page-break-after: auto; }
        }
    </style>
</head>
<body>
    <div class="wl-sheet">
        ${Array.from({ length: this.quantity }, () => `<div class="wl-label-slot">${labelHtml}</div>`).join('')}
    </div>
    <script>window.onload = function () { setTimeout(function () { window.print(); }, 200); };</script>
</body>
</html>`;

            printWindow.document.open();
            printWindow.document.write(htmlContent);
            printWindow.document.close();

            this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: `Ready to print ${this.quantity} label(s)`
            });
        } catch (error) {
            console.error('Error printing location label:', error);
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to print label' });
        }
    }

    private escapeHtml(value: string): string {
        return (value ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    closeDialog(): void {
        this.dialogRef.close();
    }
}
