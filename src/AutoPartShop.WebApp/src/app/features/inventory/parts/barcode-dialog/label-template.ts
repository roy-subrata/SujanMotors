/**
 * Single source of truth for the auto-parts label.
 *
 * Both the live preview (rendered into the dialog via [innerHTML]) and the
 * print window use {@link buildLabelMarkup} + {@link LABEL_CSS}, so what the
 * user sees is exactly what prints. All rules are scoped under `.apl-label`
 * so they can be injected globally without leaking into the rest of the app.
 */

export type LabelSizeKey = 'large' | 'standard' | 'compact' | 'tiny' | 'custom';

/** Physical label-stock presets (thermal roll), width x height in millimetres. */
export const LABEL_SIZE_PRESETS: Record<Exclude<LabelSizeKey, 'custom'>, { widthMm: number; heightMm: number }> = {
    large: { widthMm: 100, heightMm: 50 },
    standard: { widthMm: 50, heightMm: 40 },
    compact: { widthMm: 40, heightMm: 25 },
    tiny: { widthMm: 30, heightMm: 15 },
};

/** Field keys that are shown by default for each size (user can still override). */
export const DEFAULT_FIELDS_BY_SIZE: Record<Exclude<LabelSizeKey, 'custom'>, string[]> = {
    large: ['categoryName', 'brandName', 'name', 'sku', 'partNumber', 'oemNumber', 'unitCode', 'sellingPrice'],
    standard: ['brandName', 'name', 'sku', 'partNumber', 'sellingPrice'],
    compact: ['name', 'sku', 'sellingPrice'],
    tiny: ['sku'],
};

export interface LabelMarkupOptions {
    sizeKey: LabelSizeKey;
    widthMm: number;
    heightMm: number;
    isQr: boolean;
    /** Inline SVG markup for the barcode (linear) or QR code. */
    barcodeSvg: string;
    /** Human-readable value printed under a linear barcode. */
    barcodeValue: string;
    companyName: string;
    category: string;
    brand: string;
    name: string;
    sku: string;
    partNumber: string;
    oemNumber: string;
    unit: string;
    price: string;
}

function esc(value: string): string {
    return (value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

/** A single id row, e.g. `SKU  BRK-1234`. Empty values are skipped. */
function idRow(label: string, value: string): string {
    if (!value) return '';
    return `<div class="apl-id"><span class="apl-id-key">${esc(label)}</span><span class="apl-id-val">${esc(value)}</span></div>`;
}

/**
 * Build the inner HTML for one label. Returns a self-contained `.apl-label`
 * element sized to exact mm; styling comes from {@link LABEL_CSS}.
 */
export function buildLabelMarkup(o: LabelMarkupOptions): string {
    const headerRight = [o.brand, o.category].filter(Boolean).map(esc).join(' &middot; ');
    const header = (o.companyName || headerRight)
        ? `<div class="apl-header">
             <span class="apl-company">${esc(o.companyName)}</span>
             ${headerRight ? `<span class="apl-brandcat">${headerRight}</span>` : ''}
           </div>`
        : '';

    const nameRow = o.name ? `<div class="apl-name">${esc(o.name)}</div>` : '';

    const ids = [
        idRow('SKU', o.sku),
        idRow('Part#', o.partNumber),
        idRow('OEM', o.oemNumber),
    ].join('');

    const barcodeBlock = `
        <div class="apl-barcode ${o.isQr ? 'apl-barcode-qr' : 'apl-barcode-linear'}">
            ${o.barcodeSvg || ''}
            ${!o.isQr && o.barcodeValue ? `<div class="apl-barcode-text">${esc(o.barcodeValue)}</div>` : ''}
        </div>`;

    const main = `
        <div class="apl-main">
            ${barcodeBlock}
            ${ids ? `<div class="apl-ids">${ids}</div>` : ''}
        </div>`;

    const footer = (o.price || o.unit)
        ? `<div class="apl-footer">
             ${o.price ? `<span class="apl-price"><span class="apl-mrp">M.R.P.</span>${esc(o.price)}</span>` : '<span></span>'}
             ${o.unit ? `<span class="apl-unit">${esc(o.unit)}</span>` : ''}
           </div>`
        : '';

    return `<div class="apl-label apl-${o.sizeKey}" style="width:${o.widthMm}mm;height:${o.heightMm}mm">
        ${header}
        ${nameRow}
        ${main}
        ${footer}
    </div>`;
}

/**
 * Canonical, print-safe (mono) styles for the label. Scoped under `.apl-label`.
 * Injected once into the document for the preview and inlined into the print
 * window — keep this the only place label styling lives.
 */
export const LABEL_CSS = `
.apl-label {
    box-sizing: border-box;
    background: #fff;
    color: #000;
    border: 0.4mm solid #000;
    border-radius: 0.6mm;
    padding: 1.4mm 1.8mm;
    font-family: 'Arial', 'Helvetica Neue', sans-serif;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    line-height: 1.15;
}
.apl-label * { box-sizing: border-box; }

.apl-header {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    gap: 1mm;
    border-bottom: 0.3mm solid #000;
    padding-bottom: 0.8mm;
    margin-bottom: 0.8mm;
}
.apl-company {
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.2mm;
    white-space: nowrap;
}
.apl-brandcat {
    font-weight: 600;
    text-transform: uppercase;
    color: #222;
    text-align: right;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.apl-name {
    font-weight: 700;
    margin-bottom: 1mm;
    overflow: hidden;
    text-overflow: ellipsis;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
}
.apl-main {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 2mm;
    min-height: 0;
}
.apl-barcode {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-width: 0;
}
.apl-barcode-linear { flex: 1; }
.apl-barcode svg {
    display: block;
    max-width: 100%;
    height: auto;
    width: 100%;
}
.apl-barcode-qr svg { width: auto; height: 100%; max-height: 100%; }
.apl-barcode-qr { height: 100%; }
.apl-barcode-text {
    font-family: 'Courier New', monospace;
    letter-spacing: 0.4mm;
    text-align: center;
    margin-top: 0.4mm;
}
.apl-ids {
    display: flex;
    flex-direction: column;
    gap: 0.4mm;
    flex-shrink: 0;
}
.apl-id { display: flex; gap: 1mm; align-items: baseline; }
.apl-id-key {
    font-weight: 700;
    text-transform: uppercase;
    min-width: 9mm;
}
.apl-id-val { font-weight: 500; word-break: break-all; }
.apl-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1mm;
    border-top: 0.3mm solid #000;
    padding-top: 0.8mm;
    margin-top: 0.8mm;
}
.apl-price { font-weight: 800; white-space: nowrap; }
.apl-mrp { font-weight: 600; margin-right: 1mm; font-size: 0.8em; }
.apl-unit {
    font-weight: 600;
    text-transform: uppercase;
    color: #222;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

/* ── Per-size typography (mono, scales to stock) ── */
.apl-large    { font-size: 10pt; }
.apl-large .apl-company { font-size: 12pt; }
.apl-large .apl-name { font-size: 13pt; }
.apl-large .apl-price { font-size: 14pt; }
.apl-large .apl-barcode-text { font-size: 8pt; }

.apl-standard { font-size: 7pt; }
.apl-standard .apl-company { font-size: 8pt; }
.apl-standard .apl-name { font-size: 9pt; }
.apl-standard .apl-price { font-size: 10pt; }
.apl-standard .apl-barcode-text { font-size: 6pt; }

.apl-compact { font-size: 5.5pt; padding: 1mm 1.2mm; }
.apl-compact .apl-company { font-size: 6pt; }
.apl-compact .apl-name { font-size: 6.5pt; -webkit-line-clamp: 1; }
.apl-compact .apl-price { font-size: 8pt; }
.apl-compact .apl-barcode-text { font-size: 5pt; }
.apl-compact .apl-mrp { display: none; }

.apl-tiny { font-size: 5pt; padding: 0.6mm 0.8mm; border-width: 0.3mm; }
.apl-tiny .apl-header, .apl-tiny .apl-name, .apl-tiny .apl-footer, .apl-tiny .apl-ids { display: none; }
.apl-tiny .apl-main { gap: 0; }
.apl-tiny .apl-barcode-text { font-size: 5pt; letter-spacing: 0.2mm; }

.apl-custom { font-size: 7pt; }
.apl-custom .apl-company { font-size: 8pt; }
.apl-custom .apl-name { font-size: 9pt; }
.apl-custom .apl-price { font-size: 10pt; }
`;
