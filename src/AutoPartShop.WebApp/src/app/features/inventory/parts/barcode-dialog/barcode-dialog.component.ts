import { Component, OnInit, inject, DestroyRef } from '@angular/core';
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
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { BarcodeService, BarcodeType } from '../../services/barcode.service';
import { PartResponse, PartService, VehicleCompatibilityResponse } from '../../services/part.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
    LABEL_CSS,
    LABEL_SIZE_PRESETS,
    DEFAULT_FIELDS_BY_SIZE,
    LabelSizeKey,
    LabelLayout,
    buildLabelMarkup,
} from './label-template';
import { LabelData, labelFromPart } from './label-data';

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
    imports: [CommonModule, FormsModule, ButtonModule, CardModule, SelectModule, TooltipModule, ToastModule, InputTextModule, CheckboxModule, TranslatePipe],
    providers: [MessageService],
    templateUrl: './barcode-dialog.component.html',
    styleUrls: ['./barcode-dialog.component.css']
})
export class BarcodeDialogComponent implements OnInit {
    private readonly barcodeService = inject(BarcodeService);
    private readonly partService = inject(PartService);
    private readonly messageService = inject(MessageService);
    private readonly dialogRef = inject(DynamicDialogRef);
    private readonly dialogConfig = inject(DynamicDialogConfig);
    private readonly sanitizer = inject(DomSanitizer);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    /** Normalised label data, regardless of where the dialog was opened from. */
    public data: LabelData | null = null;
    /** Visual style: classic (id-row label) or combo (company + fields + barcode, retail). */
    selectedLayout: LabelLayout = 'classic';
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
    maxQuantity: number = 500;

    layoutOptions: { label: string; value: LabelLayout }[] = [];

    /** Combo sub-design (manual choice; only relevant when layout = combo). */
    selectedComboDesign: 'spotlight' | 'detailed' = 'spotlight';
    comboDesignOptions: { label: string; value: 'spotlight' | 'detailed' }[] = [];

    // Barcode value source
    selectedValueSource: string = 'sku';
    customValue: string = '';

    // Label fields
    companyName: string = 'SM Motors';
    labelFields: LabelField[] = [];

    barcodeTypes: { label: string; value: string }[] = [];

    barcodeValueSources: { label: string; value: string }[] = [];

    barcodeSizeOptions: { label: string; value: string }[] = [];

    /** (Re)builds every static dropdown option list from the current translations. */
    private buildOptionLists(): void {
        this.layoutOptions = [
            { label: this.i18n.t('parts.barcodeDialog.layoutClassic'), value: 'classic' as LabelLayout },
            { label: this.i18n.t('parts.barcodeDialog.layoutCombo'), value: 'combo' as LabelLayout },
        ];

        this.comboDesignOptions = [
            { label: this.i18n.t('parts.barcodeDialog.comboSpotlight'), value: 'spotlight' as const },
            { label: this.i18n.t('parts.barcodeDialog.comboDetailed'), value: 'detailed' as const },
        ];

        this.barcodeTypes = [
            { label: this.i18n.t('parts.barcodeDialog.typeCode128'), value: 'code128' },
            { label: this.i18n.t('parts.barcodeDialog.typeCode39'), value: 'code39' },
            { label: this.i18n.t('parts.barcodeDialog.typeEan13'), value: 'ean13' },
            { label: this.i18n.t('parts.barcodeDialog.typeQrCode'), value: 'qrcode' }
        ];

        this.barcodeValueSources = [
            { label: this.i18n.t('parts.sku'), value: 'sku' },
            { label: this.i18n.t('parts.barcodeDialog.sourceStoredBarcode'), value: 'barcode' },
            { label: this.i18n.t('parts.partNumber'), value: 'partNumber' },
            { label: this.i18n.t('parts.barcodeDialog.sourceSkuPart'), value: 'sku-part' },
            { label: this.i18n.t('parts.barcodeDialog.sourceCustom'), value: 'custom' }
        ];

        this.barcodeSizeOptions = [
            { label: this.i18n.t('parts.barcodeDialog.sizeLarge'), value: 'large' },
            { label: this.i18n.t('parts.barcodeDialog.sizeStandard'), value: 'standard' },
            { label: this.i18n.t('parts.barcodeDialog.sizeCompact'), value: 'compact' },
            { label: this.i18n.t('parts.barcodeDialog.sizeTiny'), value: 'tiny' },
            { label: this.i18n.t('parts.barcodeDialog.sizeCustom'), value: 'custom' }
        ];
    }

    /** Maps a label-field key to its translated display label — mirrors `initializeLabelFields`. */
    private fieldLabelFor(key: string): string {
        const map: Record<string, string> = {
            categoryName: this.i18n.t('common.labels.category'),
            brandName: this.i18n.t('parts.brand'),
            name: this.i18n.t('common.labels.name'),
            localName: this.i18n.t('parts.barcodeDialog.fieldLocalName'),
            compatibility: this.i18n.t('parts.fits'),
            sku: this.i18n.t('parts.sku'),
            partNumber: this.i18n.t('parts.barcodeDialog.fieldPartHash'),
            oemNumber: this.i18n.t('parts.barcodeDialog.fieldOemNumber'),
            unitCode: this.i18n.t('parts.unit'),
            batchNumber: this.i18n.t('parts.barcodeDialog.fieldBatch'),
            mfgDate: this.i18n.t('parts.barcodeDialog.fieldMfgDate'),
            expiryDate: this.i18n.t('parts.barcodeDialog.fieldExpiry'),
            sellingPrice: this.i18n.t('parts.barcodeDialog.fieldMrp'),
        };
        return map[key] ?? key;
    }

    /** Re-translates the visible label text of each field row in place (keeps values/visibility). */
    private translateFieldLabels(): void {
        this.labelFields.forEach(f => { f.label = this.fieldLabelFor(f.key); });
    }

    ngOnInit(): void {
        this.buildOptionLists();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildOptionLists();
            if (this.labelFields.length) {
                this.translateFieldLabels();
                this.rebuildLabel();
            }
        });

        const cfg = this.dialogConfig.data ?? {};
        // Accept normalised LabelData, or adapt a raw PartResponse for back-compat.
        this.data = cfg.label ?? (cfg.part ? labelFromPart(cfg.part as PartResponse) : null);
        if (!this.data) {
            this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('parts.barcodeDialog.messages.missingDataTitle'),
                detail: this.i18n.t('parts.barcodeDialog.messages.missingDataDetail')
            });
            return;
        }

        // Default to the combo layout when lot context (batch/dates) is present.
        this.selectedLayout = cfg.layout
            ?? (this.data.batchNumber || this.data.expiryDate || this.data.mfgDate ? 'combo' : 'classic');

        // Auto-quantity: pre-fill with the caller's default (e.g. received qty).
        this.quantity = Math.min(Math.max(this.data.defaultQuantity ?? 1, 1), this.maxQuantity);

        // Prefer the stored manufacturer barcode as the value source when present.
        if (this.data.barcode) {
            this.selectedValueSource = 'barcode';
        }

        this.ensureLabelStyles();
        this.initializeLabelFields();
        this.applySizeDefaults(this.selectedLayout === 'combo' ? 'large' : 'standard');
        void this.generateBarcode(this.selectedBarcodeType);

        // Receiving/reprint labels carry no brand/category/MRP/compatibility — pull them
        // from the catalog so the label can show the full product context.
        this.enrichFromPart();
    }

    /**
     * Fill in catalog-only fields (brand, category, MRP, part#, OEM, vehicle
     * compatibility) from the Part master when the source row didn't carry them.
     * Best-effort: failures leave the label as-is.
     */
    private enrichFromPart(): void {
        const d = this.data;
        if (!d?.partId) return;

        // Only fetch what's missing (Parts-catalog source already has most of it).
        const needsPart = !d.brand || !d.category || d.price == null
            || !d.partNumber || !d.oemNumber;

        forkJoin({
            part: needsPart
                ? this.partService.getPartById(d.partId).pipe(catchError(() => of(null)))
                : of(null),
            compat: this.partService.getPartCompatibleVehicles(d.partId)
                .pipe(catchError(() => of([] as VehicleCompatibilityResponse[]))),
        }).subscribe(({ part, compat }) => {
            if (!this.data) return;
            if (part) {
                this.data.brand ??= part.brandName ?? undefined;
                this.data.category ||= part.categoryName;
                this.data.partNumber ||= part.partNumber;
                this.data.oemNumber ??= part.oemNumber ?? undefined;
                if (this.data.price == null) this.data.price = part.sellingPrice ?? null;
            }
            const summary = this.summarizeCompatibility(compat);
            if (summary) this.data.compatibility = summary;

            // Re-derive the field list (values + default visibility) and repaint.
            this.initializeLabelFields();
            this.applySizeDefaults(this.selectedSize);
            this.rebuildLabel();
        });
    }

    /** "Honda Civic, Toyota Corolla +3" from the compatible-vehicle list. */
    private summarizeCompatibility(list: VehicleCompatibilityResponse[]): string {
        const names = (list ?? [])
            .filter(v => v.isCompatible !== false)
            .map(v => `${v.vehicleMake ?? ''} ${v.vehicleModel ?? ''}`.trim())
            .filter(Boolean);
        const unique = Array.from(new Set(names));
        if (unique.length === 0) return '';
        const shown = unique.slice(0, 2);
        const extra = unique.length - shown.length;
        return extra > 0 ? `${shown.join(', ')} +${extra}` : shown.join(', ');
    }

    /** Switch label style (classic/combo) and regenerate. */
    onLayoutChange(layout: LabelLayout): void {
        this.selectedLayout = layout;
        this.applySizeDefaults(layout === 'combo' ? 'large' : this.selectedSize);
        void this.generateBarcode(this.selectedBarcodeType);
    }

    /** Switch combo sub-design (spotlight/detailed) and repaint. */
    onComboDesignChange(): void {
        this.rebuildLabel();
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
        const d = this.data;
        if (!d) return;

        this.labelFields = [
            { key: 'categoryName', label: this.fieldLabelFor('categoryName'), value: d.category || '', visible: true },
            { key: 'brandName', label: this.fieldLabelFor('brandName'), value: d.brand || '', visible: true },
            { key: 'name', label: this.fieldLabelFor('name'), value: d.name || '', visible: true },
            { key: 'localName', label: this.fieldLabelFor('localName'), value: d.localName || '', visible: !!d.localName },
            { key: 'compatibility', label: this.fieldLabelFor('compatibility'), value: d.compatibility || '', visible: !!d.compatibility },
            { key: 'sku', label: this.fieldLabelFor('sku'), value: d.sku || '', visible: true },
            { key: 'partNumber', label: this.fieldLabelFor('partNumber'), value: d.partNumber || '', visible: !!d.partNumber },
            { key: 'oemNumber', label: this.fieldLabelFor('oemNumber'), value: d.oemNumber || '', visible: !!d.oemNumber },
            { key: 'unitCode', label: this.fieldLabelFor('unitCode'), value: d.unit || '', visible: true },
            { key: 'batchNumber', label: this.fieldLabelFor('batchNumber'), value: d.batchNumber || '', visible: !!d.batchNumber },
            { key: 'mfgDate', label: this.fieldLabelFor('mfgDate'), value: this.formatDate(d.mfgDate), visible: !!d.mfgDate },
            { key: 'expiryDate', label: this.fieldLabelFor('expiryDate'), value: this.formatDate(d.expiryDate), visible: !!d.expiryDate },
            { key: 'sellingPrice', label: this.fieldLabelFor('sellingPrice'), value: d.price ? `৳ ${d.price.toLocaleString()}` : '', visible: !!d.price },
        ];
    }

    /** Format an ISO date to dd-MMM-yyyy for the label; empty when missing. */
    private formatDate(iso?: string | null): string {
        if (!iso) return '';
        const dt = new Date(iso);
        if (isNaN(dt.getTime())) return '';
        return dt.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
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
        if (!this.data) {
            return;
        }

        this.selectedBarcodeType = type;
        this.isGenerating = true;
        this.validationMessage = '';

        try {
            const isCombo = this.selectedLayout === 'combo';
            // In combo mode a linear barcode always anchors the bottom; if the user
            // picked "QR Code" as the type we fall back to Code128 for that bar.
            const linearType: BarcodeType = type === 'qrcode' ? (isCombo ? 'code128' : 'qrcode') : type;

            let value = this.getBaseBarcodeValue();
            if (linearType === 'ean13') {
                // Keep a value that's already a valid EAN-13 (e.g. a real stored barcode).
                value = this.barcodeService.validateBarcodeValue('ean13', value)
                    ? value
                    : this.barcodeService.generateEAN13FromSKU(value);
            }

            this.barcodeValue = value;

            if (linearType === 'qrcode') {
                // Classic + QR: the single barcode is the QR itself.
                this.barcodeSvg = await QRCode.toString(value, { type: 'svg', margin: 0, errorCorrectionLevel: 'M' });
                this.rebuildLabel();
                return;
            }

            if (!this.validateLinearValue(linearType, value)) {
                this.barcodeSvg = '';
                this.rebuildLabel();
                return;
            }

            this.barcodeSvg = await this.generateBarcodeSvg(linearType, value);
            this.rebuildLabel();
        } catch (error) {
            console.error('Error generating barcode:', error);
            this.messageService.add({
                severity: 'error',
                summary: this.i18n.t('common.messages.error'),
                detail: this.i18n.t('parts.barcodeDialog.messages.generateFailed')
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

    /** Current label options — single source shared by preview and print. */
    private labelOptions() {
        return {
            sizeKey: this.selectedSize,
            layout: this.selectedLayout,
            widthMm: this.widthMm,
            heightMm: this.heightMm,
            isQr: this.selectedBarcodeType === 'qrcode',
            barcodeSvg: this.barcodeSvg,
            barcodeValue: this.barcodeValue,
            comboDesign: this.selectedComboDesign,
            companyName: this.companyName,
            category: this.getFieldValue('categoryName'),
            brand: this.getFieldValue('brandName'),
            compatibility: this.getFieldValue('compatibility'),
            name: this.getFieldValue('name'),
            localName: this.getFieldValue('localName'),
            sku: this.getFieldValue('sku'),
            partNumber: this.getFieldValue('partNumber'),
            oemNumber: this.getFieldValue('oemNumber'),
            unit: this.getFieldValue('unitCode'),
            price: this.getFieldValue('sellingPrice'),
            batchNumber: this.getFieldValue('batchNumber'),
            mfgDate: this.getFieldValue('mfgDate'),
            expiryDate: this.getFieldValue('expiryDate'),
        };
    }

    /** Rebuild the label HTML from current fields + barcode. */
    private rebuildLabel(): void {
        const html = buildLabelMarkup(this.labelOptions());
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
        const d = this.data;
        if (!d) {
            return '';
        }

        switch (this.selectedValueSource) {
            case 'barcode':
                return d.barcode || d.sku;
            case 'partNumber':
                return d.partNumber || d.sku;
            case 'sku-part':
                return d.partNumber ? `${d.sku}-${d.partNumber}` : d.sku;
            case 'custom':
                return this.customValue?.trim() || d.sku;
            case 'sku':
            default:
                return d.sku;
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
            summary: this.i18n.t('parts.barcodeDialog.messages.invalidValueTitle'),
            detail: this.validationMessage
        });
        return false;
    }

    private getValidationMessage(type: BarcodeType, value: string): string {
        switch (type) {
            case 'code39':
                return this.i18n.t('parts.barcodeDialog.messages.code39Hint');
            case 'ean13':
                return this.i18n.t('parts.barcodeDialog.messages.ean13Hint');
            case 'code128':
                return value.length > 80
                    ? this.i18n.t('parts.barcodeDialog.messages.code128LengthHint')
                    : this.i18n.t('parts.barcodeDialog.messages.code128InvalidHint');
            default:
                return this.i18n.t('parts.barcodeDialog.messages.invalidGeneric');
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

        // The combo (retail) layout shows the product context (name big, brand,
        // category, compatibility, MRP) and trims by size so it still fits the
        // stock. SKU is off by default — it already prints under the barcode.
        // Extra fields (incl. SKU) can still be toggled on manually.
        const comboDefaultsBySize: Record<LabelSizeKey, string[]> = {
            large: ['name', 'brandName', 'categoryName', 'compatibility', 'batchNumber', 'mfgDate', 'expiryDate', 'sellingPrice'],
            standard: ['name', 'brandName', 'batchNumber', 'mfgDate', 'expiryDate', 'sellingPrice'],
            compact: ['name', 'batchNumber', 'expiryDate', 'sellingPrice'],
            tiny: ['name', 'sellingPrice'],
            custom: ['name', 'brandName', 'categoryName', 'compatibility', 'batchNumber', 'mfgDate', 'expiryDate', 'sellingPrice'],
        };
        const defaults = this.selectedLayout === 'combo'
            ? comboDefaultsBySize[size]
            : DEFAULT_FIELDS_BY_SIZE[size];
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

    // ── Preview scaling ──
    // The label renders at exact physical mm (so preview == print). To keep any
    // size visible inside the fixed preview panel, scale it down to fit while
    // preserving aspect ratio (never scaled above 1:1).
    private readonly MM_TO_PX = 96 / 25.4;
    private readonly PREVIEW_MAX_W = 330;
    private readonly PREVIEW_MAX_H = 340;

    /** Factor that fits the current label within the preview area (<= 1). */
    get previewScale(): number {
        const w = this.widthMm * this.MM_TO_PX;
        const h = this.heightMm * this.MM_TO_PX;
        if (!w || !h) return 1;
        return Math.min(1, this.PREVIEW_MAX_W / w, this.PREVIEW_MAX_H / h);
    }

    /** Outer box size (px) after scaling — keeps layout flow correct. */
    get previewBox(): { w: number; h: number } {
        const s = this.previewScale;
        return {
            w: this.widthMm * this.MM_TO_PX * s,
            h: this.heightMm * this.MM_TO_PX * s,
        };
    }

    /**
     * Download barcode as PNG
     */
    async downloadBarcode(): Promise<void> {
        if (!this.data) {
            return;
        }

        this.isDownloading = true;

        try {
            const filename = `barcode-${this.data.sku}-${Date.now()}`;

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
                summary: this.i18n.t('common.messages.success'),
                detail: this.i18n.t('parts.barcodeDialog.messages.downloadSuccess')
            });
        } catch (error) {
            console.error('Error downloading barcode:', error);
            this.messageService.add({
                severity: 'error',
                summary: this.i18n.t('common.messages.error'),
                detail: this.i18n.t('parts.barcodeDialog.messages.downloadFailed')
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
        if (!this.data) {
            return;
        }

        try {
            const filename = `barcode-${this.data.sku}-${Date.now()}`;
            const blob = new Blob([this.barcodeSvg], { type: 'image/svg+xml' });
            const url = window.URL.createObjectURL(blob);
            this.triggerDownload(url, `${filename}.svg`);
            window.URL.revokeObjectURL(url);

            this.messageService.add({
                severity: 'success',
                summary: this.i18n.t('common.messages.success'),
                detail: this.i18n.t('parts.barcodeDialog.messages.svgDownloadSuccess')
            });
        } catch (error) {
            console.error('Error downloading SVG:', error);
            this.messageService.add({
                severity: 'error',
                summary: this.i18n.t('common.messages.error'),
                detail: this.i18n.t('parts.barcodeDialog.messages.svgDownloadFailed')
            });
        }
    }

    /**
     * Print labels (single or bulk) — reuses the exact preview markup.
     */
    printBarcode(): void {
        if (!this.data) {
            return;
        }

        try {
            const printWindow = window.open('', '', 'width=900,height=650');
            if (!printWindow) {
                throw new Error('Could not open print window');
            }

            const labelHtml = buildLabelMarkup(this.labelOptions());

            const htmlContent = `<!DOCTYPE html>
<html>
<head>
    <title>${this.escapeHtml(this.data.name)} — Labels</title>
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
                summary: this.i18n.t('common.messages.success'),
                detail: this.i18n.t('parts.barcodeDialog.messages.printReady', { count: String(this.quantity) })
            });
        } catch (error) {
            console.error('Error printing labels:', error);
            this.messageService.add({
                severity: 'error',
                summary: this.i18n.t('common.messages.error'),
                detail: this.i18n.t('parts.barcodeDialog.messages.printFailed')
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
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('parts.barcodeDialog.messages.copySuccess')
                });
            },
            (error) => {
                console.error('Error copying to clipboard:', error);
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('parts.barcodeDialog.messages.copyFailed')
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
