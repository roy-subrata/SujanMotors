import { Component, OnInit, inject, ViewChild, ElementRef } from '@angular/core';
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
import { QRCodeComponent } from 'angularx-qrcode';
import { BarcodeService, BarcodeType, BarcodeConfig } from '../../services/barcode.service';
import { PartResponse } from '../../services/part.service';

/**
 * Barcode item for bulk printing
 */
export interface BarcodeItem {
    index: number;
    barcodeHTML: string;
    barcodeValue: string;
    barcodeType: BarcodeType;
}

@Component({
    selector: 'app-barcode-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, CardModule, SelectModule, TooltipModule, ToastModule, QRCodeComponent],
    providers: [MessageService],
    templateUrl: './barcode-dialog.component.html',
    styleUrls: ['./barcode-dialog.component.css']
})
export class BarcodeDialogComponent implements OnInit {
    @ViewChild('barcodeContainer') barcodeContainer: ElementRef | undefined;
    @ViewChild('printContainer') printContainer: ElementRef | undefined;

    private readonly barcodeService = inject(BarcodeService);
    private readonly messageService = inject(MessageService);
    private readonly dialogRef = inject(DynamicDialogRef);
    private readonly dialogConfig = inject(DynamicDialogConfig);
    private readonly sanitizer = inject(DomSanitizer);

    public part: PartResponse | null = null;
    selectedBarcodeType: BarcodeType = 'qrcode';
    barcodeValue: string = '';
    barcodeHTML: string = '';
    barcodeHTMLSafe: SafeHtml | null = null;
    validationMessage: string = '';
    isGenerating = false;
    isDownloading = false;

    // Size management
    selectedSize: string = 'medium';
    customWidth: number = 2;
    customHeight: number = 100;
    barcodeWidth: number = 2;
    barcodeHeight: number = 100;
    qrCodeSize: number = 300;

    // Quantity management
    quantity: number = 1;
    maxQuantity: number = 100;

    // Barcode value source
    selectedValueSource: string = 'sku';
    customValue: string = '';

    barcodeTypes = [
        { label: 'QR Code', value: 'qrcode' },
        { label: 'Code128', value: 'code128' },
        { label: 'Code39', value: 'code39' },
        { label: 'EAN13', value: 'ean13' }
    ];

    barcodeValueSources = [
        { label: 'SKU', value: 'sku' },
        { label: 'Part Number', value: 'partNumber' },
        { label: 'SKU + Part #', value: 'sku-part' },
        { label: 'Custom', value: 'custom' },
        { label: 'Full Details (QR Only)', value: 'details', disabled: true }
    ];

    barcodeSize = [
        { label: 'Small (50mm)', value: 'small' },
        { label: 'Medium (80mm)', value: 'medium' },
        { label: 'Large (100mm)', value: 'large' },
        { label: 'Custom', value: 'custom' }
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

        this.initializeBarcode();
    }

    /**
     * Initialize barcode from part data
     */
    private initializeBarcode(): void {
        if (!this.part) {
            return;
        }

        this.generateBarcode(this.selectedBarcodeType);
    }

    /**
     * Generate barcode based on selected type
     */
    generateBarcode(type: BarcodeType): void {
        if (!this.part) {
            return;
        }

        this.selectedBarcodeType = type;
        this.isGenerating = true;
        this.validationMessage = '';

        try {
            // Determine the barcode value based on type
            let value = '';
            switch (type) {
                case 'qrcode':
                    if (this.selectedValueSource === 'details') {
                        // QR code encodes part information as JSON
                        value = JSON.stringify({
                            id: this.part.id,
                            sku: this.part.sku,
                            name: this.part.name,
                            partNumber: this.part.partNumber
                        });
                    } else {
                        value = this.getBaseBarcodeValue();
                    }
                    this.barcodeValue = value;
                    break;

                case 'code128':
                    value = this.getBaseBarcodeValue();
                    if (!this.validateLinearValue(type, value)) {
                        return;
                    }
                    this.barcodeValue = value;
                    this.generateLinearBarcode({ type, value });
                    break;

                case 'code39':
                    value = this.getBaseBarcodeValue();
                    if (!this.validateLinearValue(type, value)) {
                        return;
                    }
                    this.barcodeValue = value;
                    this.generateLinearBarcode({ type, value });
                    break;

                case 'ean13':
                    // EAN13 - generate from SKU
                    value = this.barcodeService.generateEAN13FromSKU(this.getBaseBarcodeValue());
                    if (!this.validateLinearValue(type, value)) {
                        return;
                    }
                    this.barcodeValue = value;
                    this.generateLinearBarcode({ type, value });
                    break;
            }

            this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: `${type.toUpperCase()} barcode generated`
            });
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

    onValueSourceChange(source: string): void {
        this.selectedValueSource = source;
        if (this.selectedValueSource === 'details' && this.selectedBarcodeType !== 'qrcode') {
            this.messageService.add({
                severity: 'warn',
                summary: 'QR Only',
                detail: 'Full details are only available for QR codes. Linear barcodes will use SKU.'
            });
        }
        if (this.selectedBarcodeType === 'ean13') {
            // EAN13 requires numeric input; regenerate through generator
            this.generateBarcode(this.selectedBarcodeType);
            return;
        }
        this.generateBarcode(this.selectedBarcodeType);
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
            case 'details':
                // Details are only used for QR codes; fall back to SKU for linear types
                return this.part.sku;
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

        this.barcodeHTML = '';
        this.barcodeHTMLSafe = null;
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

    get valueSourceOptions() {
        const isQr = this.selectedBarcodeType === 'qrcode';
        return this.barcodeValueSources.map(option =>
            option.value === 'details'
                ? { ...option, disabled: !isQr }
                : option
        );
    }

    /**
     * Generate linear barcode (Code128, Code39, EAN13)
     */
    private generateLinearBarcode(config: BarcodeConfig): void {
        try {
            const svg = this.barcodeService.generateLinearBarcode({
                ...config,
                displayText: config.value,
                width: this.barcodeWidth,
                height: this.barcodeHeight
            });
            this.barcodeHTML = svg || '';
            this.barcodeHTMLSafe = svg ? this.sanitizer.bypassSecurityTrustHtml(svg) : null;
        } catch (error) {
            console.error('Error generating linear barcode:', error);
            this.barcodeHTML = '';
            this.barcodeHTMLSafe = null;
        }
    }

    /**
     * Handle barcode size change
     */
    onSizeChange(size: string): void {
        this.selectedSize = size;

        switch (size) {
            case 'small':
                this.barcodeWidth = 1.5;
                this.barcodeHeight = 80;
                this.qrCodeSize = 200;
                break;
            case 'medium':
                this.barcodeWidth = 2;
                this.barcodeHeight = 100;
                this.qrCodeSize = 300;
                break;
            case 'large':
                this.barcodeWidth = 2.5;
                this.barcodeHeight = 120;
                this.qrCodeSize = 400;
                break;
            case 'custom':
                // Use custom values
                this.barcodeWidth = this.customWidth;
                this.barcodeHeight = this.customHeight;
                break;
        }

        // Regenerate barcode with new size
        if (this.selectedBarcodeType !== 'qrcode') {
            this.generateBarcode(this.selectedBarcodeType);
        }
    }

    /**
     * Handle custom size change
     */
    onCustomSizeChange(): void {
        if (this.selectedSize === 'custom') {
            this.barcodeWidth = this.customWidth;
            this.barcodeHeight = this.customHeight;

            // Regenerate barcode
            if (this.selectedBarcodeType !== 'qrcode') {
                this.generateBarcode(this.selectedBarcodeType);
            }
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

    /**
     * Generate array of barcodes for printing
     */
    generateBarcodes(): BarcodeItem[] {
        const barcodes: BarcodeItem[] = [];
        for (let i = 0; i < this.quantity; i++) {
            barcodes.push({
                index: i + 1,
                barcodeHTML: this.barcodeHTML,
                barcodeValue: this.barcodeValue,
                barcodeType: this.selectedBarcodeType
            });
        }
        return barcodes;
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
                await this.barcodeService.downloadBarcodeAsPNG({ type: this.selectedBarcodeType, value: this.barcodeValue }, filename, true);
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

    /**
     * Download barcode as SVG
     */
    downloadBarcodeSVG(): void {
        if (!this.part || this.selectedBarcodeType === 'qrcode') {
            this.messageService.add({
                severity: 'warn',
                summary: 'Warning',
                detail: 'SVG download is only available for linear barcodes'
            });
            return;
        }

        try {
            const filename = `barcode-${this.part.sku}-${Date.now()}`;
            this.barcodeService.downloadBarcodeAsSVG(
                {
                    type: this.selectedBarcodeType,
                    value: this.barcodeValue,
                    displayText: this.barcodeValue
                },
                filename
            );

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
     * Print barcodes (single or bulk)
     */
    printBarcode(): void {
        if (!this.part) {
            return;
        }

        try {
            const printWindow = window.open('', '', 'width=800,height=600');
            if (!printWindow) {
                throw new Error('Could not open print window');
            }

            // Generate HTML for all barcodes
            let barcodesHTML = '';

            if (this.selectedBarcodeType === 'qrcode') {
                const qrCanvas = this.barcodeContainer?.nativeElement?.querySelector('canvas') as HTMLCanvasElement | null;
                const qrDataUrl = qrCanvas ? qrCanvas.toDataURL('image/png') : '';
                if (!qrDataUrl) {
                    throw new Error('Could not capture QR code canvas');
                }

                // For QR codes, embed the current canvas as an image
                for (let i = 0; i < this.quantity; i++) {
                    barcodesHTML += `
            <div class="barcode-item">
              <p class="company-name">SM Motors</p>
              <img src="${qrDataUrl}" width="${this.qrCodeSize}" height="${this.qrCodeSize}" alt="QR Code" />
              <p class="barcode-label">${this.part.name}</p>
              <p class="barcode-value">${this.barcodeValue}</p>
            </div>
          `;
                }
            } else {
                // For linear barcodes
                for (let i = 0; i < this.quantity; i++) {
                    barcodesHTML += `
            <div class="barcode-item">
              <p class="company-name">SM Motors</p>
              ${this.barcodeHTML}
              <p class="barcode-label">${this.part.name}</p>
            </div>
          `;
                }
            }

            const htmlContent = `
        <html>
          <head>
            <title>${this.part.name} - Barcodes</title>
            <style>
              body {
                font-family: Arial, sans-serif;
                margin: 0;
                padding: 10px;
                background-color: #f5f5f5;
              }
              .print-container {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                gap: 10px;
                width: 100%;
              }
              .barcode-item {
                background: white;
                padding: 10px;
                border: 1px solid #ddd;
                border-radius: 4px;
                text-align: center;
                page-break-inside: avoid;
              }
              .barcode-item svg {
                max-width: 100%;
                height: auto;
              }
              .company-name {
                margin: 0 0 8px 0;
                font-size: 10px;
                font-weight: bold;
                color: #1f2937;
                text-transform: uppercase;
                letter-spacing: 0.5px;
              }
              .barcode-label {
                margin: 8px 0 4px 0;
                font-size: 12px;
                font-weight: bold;
                word-break: break-word;
              }
              .barcode-value {
                margin: 0;
                font-size: 9px;
                color: #666;
                word-break: break-all;
              }
              @media print {
                body {
                  background: white;
                  padding: 0;
                }
                .print-container {
                  gap: 0;
                }
                .barcode-item {
                  border: 1px solid #999;
                  page-break-inside: avoid;
                }
              }
            </style>
          </head>
          <body>
            <div class="print-container">
              ${barcodesHTML}
            </div>
          </body>
        </html>
      `;

            printWindow.document.open();
            printWindow.document.write(htmlContent);
            printWindow.document.close();

            setTimeout(() => {
                printWindow.print();
            }, 500);

            this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: `Ready to print ${this.quantity} barcode(s)`
            });
        } catch (error) {
            console.error('Error printing barcodes:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to print barcodes'
            });
        }
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
