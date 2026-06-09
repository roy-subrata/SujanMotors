import { Component, OnInit, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import QRCode from 'qrcode';
import { BarcodeService, BarcodeType } from '../../services/barcode.service';
import { PartResponse } from '../../services/part.service';
import {
    LABEL_CSS,
    LABEL_SIZE_PRESETS,
    DEFAULT_FIELDS_BY_SIZE,
    LabelSizeKey,
    buildLabelMarkup,
} from './label-template';

/**
 * Dynamic label field definition
 */
export interface LabelField {
    key: string;
    label: string;
    value: string;
    visible: boolean;
}

/** Bar dimensions (jsbarcode units) tuned per label size. */
const LINEAR_DIMS: Record<LabelSizeKey, { width: number; height: number }> = {
    large: { width: 2, height: 60 },
    standard: { width: 1.6, height: 45 },
    compact: { width: 1.2, height: 32 },
    tiny: { width: 1, height: 30 },
    custom: { width: 1.6, height: 45 },
};

const LABEL_STYLE_ELEMENT_ID = 'apl-label-styles';

@Component({
    selector: 'app-barcode-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, CardModule, SelectModule, TooltipModule, ToastModule, InputTextModule, CheckboxModule],
    providers: [MessageService],
    templateUrl: './barcode-dialog.component.html',
    styleUrls: ['./barcode-dialog.component.css']
})
export class BarcodeDialogComponent implements OnInit {
    private readonly barcodeService = inject(BarcodeService);
    private readonly messageService = inject(MessageService);
    private readonly dialogRef = inject(DynamicDialogRef);
    private readonly dialogConfig = inject(DynamicDialogConfig);
    private readonly sanitizer = inject(DomSanitizer);

    public part: PartResponse | null = null;
    selectedBarcodeType: BarcodeType = 'code128';
    barcodeValue: string = '';
    /** Inline SVG (linear or QR) for the current barcode. */
    barcodeSvg: string = '';
    /** Rendered label HTML (single source of truth, shared with print). */
    labelHtmlSafe: SafeHtml | null = null;
    validationMessage: string = '';
    isGenerating = false;
    isDownloading = false;

    // Size management (physical label stock, in mm)
    selectedSize: LabelSizeKey = 'standard';
    widthMm: number = LABEL_SIZE_PRESETS.standard.widthMm;
    heightMm: number = LABEL_SIZE_PRESETS.standard.heightMm;
    customWidthMm: number = 50;
    customHeightMm: number = 40;

    // Quantity management
    quantity: number = 1;
    maxQuantity: number = 100;

    // Barcode value source
    selectedValueSource: string = 'sku';
    customValue: string = '';

    // Label fields
    companyName: string = 'SM Motors';
    labelFields: LabelField[] = [];

    barcodeTypes = [
        { label: 'Code128', value: 'code128' },
        { label: 'Code39', value: 'code39' },
        { label: 'EAN13', value: 'ean13' },
        { label: 'QR Code', value: 'qrcode' }
    ];

    barcodeValueSources = [
        { label: 'SKU', value: 'sku' },
        { label: 'Part Number', value: 'partNumber' },
        { label: 'SKU + Part #', value: 'sku-part' },
        { label: 'Custom', value: 'custom' }
    ];

    barcodeSizeOptions = [
        { label: 'Large — 100 × 50 mm', value: 'large' },
        { label: 'Standard — 50 × 40 mm', value: 'standard' },
        { label: 'Compact — 40 × 25 mm', value: 'compact' },
        { label: 'Tiny — 30 × 15 mm', value: 'tiny' },
        { label: 'Custom (mm)', value: 'custom' }
    ];

    ngOnInit(): void {
        this.part = this.dialogConfig.data.part;
        if (!this.part) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Missing Data',
                detail: 'No part data provided for barcode generation'
            });
            return;
        }

        this.ensureLabelStyles();
        this.initializeLabelFields();
        this.applySizeDefaults('standard');
        void this.generateBarcode(this.selectedBarcodeType);
    }

    /** Inject the canonical label CSS into <head> once (preview = print). */
    private ensureLabelStyles(): void {
        if (document.getElementById(LABEL_STYLE_ELEMENT_ID)) {
            return;
        }
        const style = document.createElement('style');
        style.id = LABEL_STYLE_ELEMENT_ID;
        style.textContent = LABEL_CSS;
        document.head.appendChild(style);
    }

    /**
     * Initialize dynamic label fields from part data
     */
    private initializeLabelFields(): void {
        if (!this.part) return;

        this.labelFields = [
            { key: 'categoryName', label: 'Category', value: this.part.categoryName || '', visible: true },
            { key: 'brandName', label: 'Brand', value: this.part.brandName || '', visible: true },
            { key: 'name', label: 'Name', value: this.part.name || '', visible: true },
            { key: 'sku', label: 'SKU', value: this.part.sku || '', visible: true },
            { key: 'partNumber', label: 'Part #', value: this.part.partNumber || '', visible: true },
            { key: 'oemNumber', label: 'OEM #', value: this.part.oemNumber || '', visible: !!this.part.oemNumber },
            { key: 'unitCode', label: 'Unit', value: this.part.unitCode || this.part.unitName || '', visible: true },
            { key: 'sellingPrice', label: 'M.R.P.', value: this.part.sellingPrice ? `৳ ${this.part.sellingPrice.toLocaleString()}` : '', visible: !!this.part.sellingPrice },
        ];
    }

    /**
     * Get a specific field value (empty when the field is hidden)
     */
    getFieldValue(key: string): string {
        const field = this.labelFields.find(f => f.key === key);
        return field?.visible ? field.value : '';
    }

    /**
     * Generate barcode value + SVG for the selected type, then rebuild the label.
     */
    async generateBarcode(type: BarcodeType): Promise<void> {
        if (!this.part) {
            return;
        }

        this.selectedBarcodeType = type;
        this.isGenerating = true;
        this.validationMessage = '';

        try {
            let value: string;
            if (type === 'ean13') {
                value = this.barcodeService.generateEAN13FromSKU(this.getBaseBarcodeValue());
            } else {
                value = this.getBaseBarcodeValue();
            }

            if (type !== 'qrcode' && !this.validateLinearValue(type, value)) {
                this.barcodeSvg = '';
                this.rebuildLabel();
                return;
            }

            this.barcodeValue = value;
            this.barcodeSvg = await this.generateBarcodeSvg(type, value);
            this.rebuildLabel();
        } catch (error) {
            console.error('Error generating barcode:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to generate barcode'
            });
        } finally {
            this.isGenerating = false;
        }
    }

    /** Produce inline SVG markup for either a QR or a linear barcode. */
    private async generateBarcodeSvg(type: BarcodeType, value: string): Promise<string> {
        if (type === 'qrcode') {
            return await QRCode.toString(value, { type: 'svg', margin: 0, errorCorrectionLevel: 'M' });
        }
        const dims = LINEAR_DIMS[this.selectedSize];
        // displayText: '' suppresses jsbarcode's own text — the label renders it separately.
        return this.barcodeService.generateLinearBarcode({
            type,
            value,
            displayText: '',
            width: dims.width,
            height: dims.height
        });
    }

    /** Rebuild the label HTML from current fields + barcode. */
    private rebuildLabel(): void {
        const html = buildLabelMarkup({
            sizeKey: this.selectedSize,
            widthMm: this.widthMm,
            heightMm: this.heightMm,
            isQr: this.selectedBarcodeType === 'qrcode',
            barcodeSvg: this.barcodeSvg,
            barcodeValue: this.barcodeValue,
            companyName: this.companyName,
            category: this.getFieldValue('categoryName'),
            brand: this.getFieldValue('brandName'),
            name: this.getFieldValue('name'),
            sku: this.getFieldValue('sku'),
            partNumber: this.getFieldValue('partNumber'),
            oemNumber: this.getFieldValue('oemNumber'),
            unit: this.getFieldValue('unitCode'),
            price: this.getFieldValue('sellingPrice'),
        });
        this.labelHtmlSafe = this.sanitizer.bypassSecurityTrustHtml(html);
    }

    /** Called when a label field value or visibility toggle changes. */
    onFieldChange(): void {
        this.rebuildLabel();
    }

    onValueSourceChange(source: string): void {
        this.selectedValueSource = source;
        void this.generateBarcode(this.selectedBarcodeType);
    }

    private getBaseBarcodeValue(): string {
        if (!this.part) {
            return '';
        }

        switch (this.selectedValueSource) {
            case 'partNumber':
                return this.part.partNumber || this.part.sku;
            case 'sku-part':
                return `${this.part.sku}-${this.part.partNumber}`;
            case 'custom':
                return this.customValue?.trim() || this.part.sku;
            case 'sku':
            default:
                return this.part.sku;
        }
    }

    private validateLinearValue(type: BarcodeType, value: string): boolean {
        const isValid = this.barcodeService.validateBarcodeValue(type, value);
        if (isValid) {
            this.validationMessage = '';
            return true;
        }

        this.validationMessage = this.getValidationMessage(type, value);
        this.messageService.add({
            severity: 'warn',
            summary: 'Invalid Barcode Value',
            detail: this.validationMessage
        });
        return false;
    }

    private getValidationMessage(type: BarcodeType, value: string): string {
        switch (type) {
            case 'code39':
                return 'Code39 supports A-Z, 0-9, space, and - . $ / + % (max 80 chars).';
            case 'ean13':
                return 'EAN13 must be exactly 13 digits. Try a different value source or custom value.';
            case 'code128':
                return value.length > 80 ? 'Code128 supports up to 80 characters.' : 'Invalid Code128 value.';
            default:
                return 'Invalid barcode value.';
        }
    }

    /**
     * Handle label size change — applies stock dimensions + default field set.
     */
    onSizeChange(size: LabelSizeKey): void {
        this.applySizeDefaults(size);
        void this.generateBarcode(this.selectedBarcodeType);
    }

    /** Apply mm dimensions + the recommended visible-field set for a size. */
    private applySizeDefaults(size: LabelSizeKey): void {
        this.selectedSize = size;

        if (size === 'custom') {
            this.widthMm = this.customWidthMm;
            this.heightMm = this.customHeightMm;
            return;
        }

        const preset = LABEL_SIZE_PRESETS[size];
        this.widthMm = preset.widthMm;
        this.heightMm = preset.heightMm;

        const defaults = DEFAULT_FIELDS_BY_SIZE[size];
        this.labelFields.forEach(f => {
            // Never show a field that has no value, even if the size would include it.
            f.visible = defaults.includes(f.key) && !!f.value;
        });
    }

    /**
     * Handle custom size change
     */
    onCustomSizeChange(): void {
        if (this.selectedSize === 'custom') {
            this.widthMm = this.customWidthMm;
            this.heightMm = this.customHeightMm;
            void this.generateBarcode(this.selectedBarcodeType);
        }
    }

    /**
     * Validate quantity
     */
    validateQuantity(): void {
        if (this.quantity < 1) {
            this.quantity = 1;
        } else if (this.quantity > this.maxQuantity) {
            this.quantity = this.maxQuantity;
        }
    }

    get valueSourceOptions() {
        return this.barcodeValueSources;
    }

    /**
     * Download barcode as PNG
     */
    async downloadBarcode(): Promise<void> {
        if (!this.part) {
            return;
        }

        this.isDownloading = true;

        try {
            const filename = `barcode-${this.part.sku}-${Date.now()}`;

            if (this.selectedBarcodeType === 'qrcode') {
                const dataUrl = await QRCode.toDataURL(this.barcodeValue, { margin: 1, width: 512 });
                this.triggerDownload(dataUrl, `${filename}.png`);
            } else {
                await this.barcodeService.downloadBarcodeAsPNG(
                    {
                        type: this.selectedBarcodeType,
                        value: this.barcodeValue,
                        displayText: this.barcodeValue
                    },
                    filename,
                    false
                );
            }

            this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Barcode downloaded successfully'
            });
        } catch (error) {
            console.error('Error downloading barcode:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to download barcode'
            });
        } finally {
            this.isDownloading = false;
        }
    }

    private triggerDownload(href: string, filename: string): void {
        const link = document.createElement('a');
        link.href = href;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    /**
     * Download barcode as SVG
     */
    downloadBarcodeSVG(): void {
        if (!this.part) {
            return;
        }

        try {
            const filename = `barcode-${this.part.sku}-${Date.now()}`;
            const blob = new Blob([this.barcodeSvg], { type: 'image/svg+xml' });
            const url = window.URL.createObjectURL(blob);
            this.triggerDownload(url, `${filename}.svg`);
            window.URL.revokeObjectURL(url);

            this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Barcode SVG downloaded successfully'
            });
        } catch (error) {
            console.error('Error downloading SVG:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to download SVG barcode'
            });
        }
    }

    /**
     * Print labels (single or bulk) — reuses the exact preview markup.
     */
    printBarcode(): void {
        if (!this.part) {
            return;
        }

        try {
            const printWindow = window.open('', '', 'width=900,height=650');
            if (!printWindow) {
                throw new Error('Could not open print window');
            }

            const labelHtml = buildLabelMarkup({
                sizeKey: this.selectedSize,
                widthMm: this.widthMm,
                heightMm: this.heightMm,
                isQr: this.selectedBarcodeType === 'qrcode',
                barcodeSvg: this.barcodeSvg,
                barcodeValue: this.barcodeValue,
                companyName: this.companyName,
                category: this.getFieldValue('categoryName'),
                brand: this.getFieldValue('brandName'),
                name: this.getFieldValue('name'),
                sku: this.getFieldValue('sku'),
                partNumber: this.getFieldValue('partNumber'),
                oemNumber: this.getFieldValue('oemNumber'),
                unit: this.getFieldValue('unitCode'),
                price: this.getFieldValue('sellingPrice'),
            });

            const htmlContent = `<!DOCTYPE html>
<html>
<head>
    <title>${this.escapeHtml(this.part.name)} — Labels</title>
    <style>
        @page { size: ${this.widthMm}mm ${this.heightMm}mm; margin: 0; }
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { background: #fff; }
        .apl-sheet { display: flex; flex-wrap: wrap; gap: 2mm; padding: 2mm; }
        .apl-label-slot { page-break-inside: avoid; break-inside: avoid; }
        ${LABEL_CSS}
        @media print {
            body { background: #fff; }
            .apl-sheet { gap: 0; padding: 0; }
            .apl-label-slot { page-break-after: always; }
            .apl-label-slot:last-child { page-break-after: auto; }
        }
    </style>
</head>
<body>
    <div class="apl-sheet">
        ${Array.from({ length: this.quantity }, () => `<div class="apl-label-slot">${labelHtml}</div>`).join('')}
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
            console.error('Error printing labels:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to print labels'
            });
        }
    }

    private escapeHtml(value: string): string {
        return (value ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    /**
     * Copy barcode value to clipboard
     */
    copyBarcodeValue(): void {
        navigator.clipboard.writeText(this.barcodeValue).then(
            () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Barcode value copied to clipboard'
                });
            },
            (error) => {
                console.error('Error copying to clipboard:', error);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to copy barcode value'
                });
            }
        );
    }

    /**
     * Close dialog
     */
    closeDialog(): void {
        this.dialogRef.close();
    }
}
