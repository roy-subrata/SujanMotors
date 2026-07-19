/**
 * Single source of truth for the auto-parts label.
 *
 * Both the live preview (rendered into the dialog via [innerHTML]) and the
 * print window use {@link buildLabelMarkup} + {@link LABEL_CSS}, so what the
 * user sees is exactly what prints. All rules are scoped under `.apl-label`
 * so they can be injected globally without leaking into the rest of the app.
 */

export type LabelSizeKey = 'large' | 'standard' | 'compact' | 'tiny' | 'custom';

/**
 * Label visual style.
 * - `classic`: the original auto-parts label (single barcode, id rows).
 * - `combo`:   retail/product style matching docs/barcode.png — a left field
 *              column + QR (top-right) + a full-width linear barcode at the
 *              bottom. Used for receiving (GRN) and stock-lot reprints.
 */
export type LabelLayout = 'classic' | 'combo';

/**
 * Physical label-stock presets (thermal roll), width x height in millimetres.
 *
 * `compact` (50x25) and `standard` (70x40) match the two product-label sizes
 * in the `design_handoff_barcode_labels` handoff exactly — `compact`'s height
 * and `standard`'s height already matched the old presets, so only the width
 * changed (40->50 and 50->70) to land on the handoff's physical dimensions
 * without introducing new size keys or touching the field-visibility /
 * custom-size logic keyed on these same names.
 */
export const LABEL_SIZE_PRESETS: Record<Exclude<LabelSizeKey, 'custom'>, { widthMm: number; heightMm: number }> = {
    large: { widthMm: 100, heightMm: 50 },
    standard: { widthMm: 70, heightMm: 40 },
    compact: { widthMm: 50, heightMm: 25 },
    tiny: { widthMm: 30, heightMm: 15 },
};

/** Field keys that are shown by default for each size (user can still override). */
export const DEFAULT_FIELDS_BY_SIZE: Record<Exclude<LabelSizeKey, 'custom'>, string[]> = {
    large: ['categoryName', 'brandName', 'name', 'sku', 'partNumber', 'oemNumber', 'unitCode', 'sellingPrice'],
    standard: ['brandName', 'name', 'sku', 'partNumber', 'sellingPrice'],
    // No SKU id-row by default — the code string already prints under the
    // barcode, matching the handoff's compact fields (name, price, code only).
    compact: ['name', 'sellingPrice'],
    tiny: ['sku'],
};

export interface LabelMarkupOptions {
    sizeKey: LabelSizeKey;
    /** Visual style. Defaults to `classic` when omitted. */
    layout?: LabelLayout;
    widthMm: number;
    heightMm: number;
    isQr: boolean;
    /** Inline SVG markup for the barcode (linear) or QR code. */
    barcodeSvg: string;
    /** Human-readable value printed under a linear barcode. */
    barcodeValue: string;
    /** Inline QR SVG for the `combo` layout (rendered alongside the linear barcode). */
    qrSvg?: string;
    /** Caption shown under the QR in the `combo` layout. */
    qrCaption?: string;
    companyName: string;
    category: string;
    brand: string;
    /** Compact vehicle-compatibility summary, e.g. "Honda Civic, Toyota Corolla +3". */
    compatibility?: string;
    /** Combo sub-design: name-led `spotlight` (default) or field-grid `detailed`. */
    comboDesign?: 'spotlight' | 'detailed';
    name: string;
    /** Local-language name shown below the catalog name. */
    localName?: string;
    sku: string;
    partNumber: string;
    oemNumber: string;
    unit: string;
    price: string;
    // ── Lot / batch fields (combo layout) ──
    batchNumber?: string;
    /** Pre-formatted manufacture/production date string. */
    mfgDate?: string;
    /** Pre-formatted expiry date string. */
    expiryDate?: string;
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
    if (o.layout === 'combo') {
        return buildComboMarkup(o);
    }

    // Header mirrors the handoff's compact/with-QR mockups: store name left,
    // price right, hairline below — the price is the most prominent element
    // and sits at the top of the label rather than in a footer band.
    const priceBlock = o.price
        ? `<span class="apl-price"><span class="apl-mrp">M.R.P.</span><span class="apl-price-val">${esc(o.price)}</span></span>`
        : '';
    const header = (o.companyName || priceBlock)
        ? `<div class="apl-header">
             <span class="apl-company">${esc(o.companyName)}</span>
             ${priceBlock}
           </div>`
        : '';

    const metaText = [o.brand, o.category].filter(Boolean).map(esc).join(' &middot; ');
    const metaRow = metaText ? `<div class="apl-meta">${metaText}</div>` : '';

    const nameRow = o.name ? `<div class="apl-name">${esc(o.name)}${o.localName ? `<span class="apl-localname"> · ${esc(o.localName)}</span>` : ''}</div>` : '';

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

    const footer = o.unit
        ? `<div class="apl-footer"><span class="apl-unit">${esc(o.unit)}</span></div>`
        : '';

    return `<div class="apl-label apl-${o.sizeKey}" style="width:${o.widthMm}mm;height:${o.heightMm}mm">
        ${header}
        ${metaRow}
        ${nameRow}
        ${main}
        ${footer}
    </div>`;
}

/** A colon-aligned `Key : Value` row for the combo layout. Empty values skipped. */
function comboRow(label: string, value: string): string {
    if (!value) return '';
    return `<div class="apl-c-row"><span class="apl-c-key">${esc(label)}</span><span class="apl-c-sep">:</span><span class="apl-c-val">${esc(value)}</span></div>`;
}

/**
 * Auto-fit tier for the product name — bigger for short names, smaller for long
 * ones, so the headline is as large as possible without clipping. Deterministic
 * (length-based) so the preview and the print window agree without DOM measuring.
 */
function nameTier(name: string): 'xl' | 'lg' | 'md' | 'sm' {
    const len = (name ?? '').trim().length;
    if (len <= 12) return 'xl';
    if (len <= 22) return 'lg';
    if (len <= 36) return 'md';
    return 'sm';
}

/** Shared combo pieces (barcode block + MRP band). */
function comboBarcode(o: LabelMarkupOptions): string {
    if (!o.barcodeSvg) return '';
    return `<div class="apl-c-barcode">
             ${o.barcodeSvg}
             ${o.barcodeValue ? `<div class="apl-barcode-text">${esc(o.barcodeValue)}</div>` : ''}
           </div>`;
}
function comboPriceBand(o: LabelMarkupOptions): string {
    return o.price
        ? `<div class="apl-c-price"><span class="apl-mrp">M.R.P.</span><span class="apl-c-price-val">${esc(o.price)}</span></div>`
        : '';
}
function comboName(o: LabelMarkupOptions): string {
    return o.name
        ? `<div class="apl-c-name apl-c-name--${nameTier(o.name)}">${esc(o.name)}</div>`
        : '';
}
function comboCompany(o: LabelMarkupOptions): string {
    return o.companyName ? `<div class="apl-c-company">${esc(o.companyName)}</div>` : '';
}

/** Dispatch to the chosen combo sub-design (default: spotlight). */
function buildComboMarkup(o: LabelMarkupOptions): string {
    return o.comboDesign === 'detailed'
        ? buildComboDetailed(o)
        : buildComboSpotlight(o);
}

/**
 * Spotlight — name-led: company → big auto-fit NAME → brand · category → "Fits"
 * → optional SKU/lot rows → full-width barcode → M.R.P. band. Best when few fields.
 */
function buildComboSpotlight(o: LabelMarkupOptions): string {
    const brandCat = [o.brand, o.category].filter(Boolean).map(esc).join(' &middot; ');
    const brandCatLine = brandCat ? `<div class="apl-c-meta">${brandCat}</div>` : '';

    const fitsLine = o.compatibility
        ? `<div class="apl-c-fits"><span class="apl-c-fits-key">Fits:</span> ${esc(o.compatibility)}</div>`
        : '';

    const detailRows = [
        o.localName ? comboRow('Local', o.localName) : '',
        comboRow('SKU', o.sku),
        comboRow('Batch', o.batchNumber ?? ''),
        comboRow('Mfg', o.mfgDate ?? ''),
        comboRow('Expiry', o.expiryDate ?? ''),
    ].join('');
    const details = detailRows ? `<div class="apl-c-fields">${detailRows}</div>` : '';

    return `<div class="apl-label apl-combo apl-design-spotlight apl-${o.sizeKey}" style="width:${o.widthMm}mm;height:${o.heightMm}mm">
        ${comboCompany(o)}
        <div class="apl-c-top">
            ${comboName(o)}
            ${brandCatLine}
            ${fitsLine}
            ${details}
        </div>
        ${comboBarcode(o)}
        ${comboPriceBand(o)}
    </div>`;
}

/**
 * Detailed — field-grid: company → bold NAME → an aligned `key : value` grid
 * (SKU, Brand, Category, Fits, Batch, Mfg, Expiry) → full-width barcode →
 * M.R.P. band. Best when many fields need to line up neatly.
 */
function buildComboDetailed(o: LabelMarkupOptions): string {
    const rows = [
        o.localName ? comboRow('Local', o.localName) : '',
        comboRow('SKU', o.sku),
        comboRow('Brand', o.brand),
        comboRow('Category', o.category),
        comboRow('Fits', o.compatibility ?? ''),
        comboRow('Batch', o.batchNumber ?? ''),
        comboRow('Mfg', o.mfgDate ?? ''),
        comboRow('Expiry', o.expiryDate ?? ''),
    ].join('');
    const fields = rows ? `<div class="apl-c-fields">${rows}</div>` : '';

    return `<div class="apl-label apl-combo apl-design-detailed apl-${o.sizeKey}" style="width:${o.widthMm}mm;height:${o.heightMm}mm">
        ${comboCompany(o)}
        <div class="apl-c-top">
            ${comboName(o)}
            ${fields}
        </div>
        ${comboBarcode(o)}
        ${comboPriceBand(o)}
    </div>`;
}

/**
 * Canonical, print-safe (mono) styles for the label. Scoped under `.apl-label`.
 * Injected once into the document for the preview and inlined into the print
 * window — keep this the only place label styling lives.
 */
export const LABEL_CSS = `
:root {
    --apl-ink: #1c1917;
    --apl-muted: #57534e;
    --apl-muted-2: #78716c;
    --apl-hairline: #1c1917;
    --apl-font-sans: 'IBM Plex Sans', 'Helvetica Neue', Arial, sans-serif;
    --apl-font-mono: 'IBM Plex Mono', 'Courier New', monospace;
}

.apl-label {
    box-sizing: border-box;
    background: #fff;
    color: var(--apl-ink);
    border: none;
    border-radius: 0;
    padding: 1.4mm 1.8mm;
    font-family: var(--apl-font-sans);
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
    border-bottom: 0.32mm solid var(--apl-hairline);
    padding-bottom: 0.8mm;
    margin-bottom: 0.8mm;
}
.apl-company {
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.3mm;
    white-space: nowrap;
    color: var(--apl-ink);
}
.apl-meta {
    font-weight: 600;
    text-transform: uppercase;
    color: var(--apl-muted);
    letter-spacing: 0.15mm;
    margin-bottom: 0.6mm;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.apl-name {
    font-weight: 600;
    margin-bottom: 1mm;
    overflow: hidden;
    text-overflow: ellipsis;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    color: var(--apl-ink);
}
.apl-localname {
    font-weight: 500;
    font-style: italic;
    font-size: 0.85em;
    color: var(--apl-muted);
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
    font-family: var(--apl-font-mono);
    font-weight: 600;
    letter-spacing: 0.5mm;
    text-align: center;
    margin-top: 0.5mm;
    color: var(--apl-ink);
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
    color: var(--apl-muted);
}
.apl-id-val {
    font-weight: 500;
    font-family: var(--apl-font-mono);
    word-break: break-all;
    color: var(--apl-ink);
}
.apl-footer {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 1mm;
    border-top: 0.32mm solid var(--apl-hairline);
    padding-top: 0.8mm;
    margin-top: 0.8mm;
}
.apl-price {
    font-weight: 700;
    font-family: var(--apl-font-mono);
    white-space: nowrap;
    color: var(--apl-ink);
}
.apl-price-val { font-weight: 700; }
.apl-mrp {
    font-weight: 600;
    font-family: var(--apl-font-sans);
    margin-right: 1mm;
    font-size: 0.7em;
    color: var(--apl-muted-2);
    text-transform: uppercase;
    letter-spacing: 0.2mm;
}
.apl-unit {
    font-weight: 600;
    text-transform: uppercase;
    color: var(--apl-muted);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

/* ── Per-size typography (matches the handoff's ink/mono/hairline system,
      scaled proportionally to stock size) ── */
.apl-large    { font-size: 10pt; }
.apl-large .apl-company { font-size: 12pt; }
.apl-large .apl-meta { font-size: 8pt; }
.apl-large .apl-name { font-size: 13pt; }
.apl-large .apl-price { font-size: 16pt; }
.apl-large .apl-barcode-text { font-size: 8pt; letter-spacing: 0.9mm; }
.apl-large .apl-barcode-linear svg { max-height: 15mm; }

.apl-standard { font-size: 7pt; }
.apl-standard .apl-company { font-size: 7.5pt; letter-spacing: 0.25mm; }
.apl-standard .apl-meta { font-size: 6.5pt; }
.apl-standard .apl-name { font-size: 11pt; }
.apl-standard .apl-price { font-size: 17pt; }
.apl-standard .apl-mrp { display: none; }
.apl-standard .apl-barcode-text { font-size: 6.5pt; letter-spacing: 0.7mm; }
.apl-standard .apl-barcode-linear svg { max-height: 11mm; }

.apl-compact { font-size: 5.5pt; padding: 2mm 2.4mm; }
.apl-compact .apl-company { font-size: 6.5pt; letter-spacing: 0.25mm; }
.apl-compact .apl-meta { display: none; }
.apl-compact .apl-name { font-size: 8.5pt; font-weight: 600; -webkit-line-clamp: 2; }
.apl-compact .apl-price { font-size: 12pt; }
.apl-compact .apl-barcode-text { font-size: 6pt; letter-spacing: 0.5mm; }
.apl-compact .apl-barcode-linear svg { max-height: 8mm; }
.apl-compact .apl-mrp { display: none; }

.apl-tiny { font-size: 5pt; padding: 0.6mm 0.8mm; }
.apl-tiny .apl-header, .apl-tiny .apl-meta, .apl-tiny .apl-name, .apl-tiny .apl-footer, .apl-tiny .apl-ids { display: none; }
.apl-tiny .apl-main { gap: 0; }
.apl-tiny .apl-barcode-text { font-size: 5pt; letter-spacing: 0.2mm; }
.apl-tiny .apl-barcode-linear svg { max-height: 7mm; }

.apl-custom { font-size: 7pt; }
.apl-custom .apl-company { font-size: 8pt; }
.apl-custom .apl-meta { font-size: 6.5pt; }
.apl-custom .apl-name { font-size: 9pt; }
.apl-custom .apl-price { font-size: 12pt; }
.apl-custom .apl-barcode-linear svg { max-height: 11mm; }

/* ── Combo (retail/product) layout — name-led hierarchy + barcode + MRP band ──
      Structural logic unchanged; only recolored/refonted/respaced to the
      handoff's ink/mono/hairline design language. ── */
.apl-combo { padding: 1.2mm 1.6mm; gap: 0.6mm; line-height: 1.1; justify-content: flex-start; }
.apl-combo .apl-c-company {
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.3mm;
    text-align: center;
    color: var(--apl-muted);
    flex-shrink: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.apl-combo .apl-c-top {
    display: flex;
    flex-direction: column;
    gap: 0.4mm;
    flex: 0 1 auto;     /* size to content; shrink + clip if overfull */
    min-height: 0;
    overflow: hidden;
}
/* Product name — the headline of the label */
.apl-combo .apl-c-name {
    font-weight: 700;
    line-height: 1.1;
    overflow: hidden;
    text-overflow: ellipsis;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    color: var(--apl-ink);
}
.apl-combo .apl-c-meta {
    font-weight: 600;
    color: var(--apl-muted);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.apl-combo .apl-c-fits {
    color: var(--apl-muted);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.apl-combo .apl-c-fits-key { font-weight: 700; color: var(--apl-ink); }
.apl-combo .apl-c-fields {
    display: grid;
    grid-template-columns: auto auto 1fr;
    align-content: start;
    column-gap: 0.6mm;
    row-gap: 0.2mm;
    min-width: 0;
}
.apl-combo .apl-c-row { display: contents; }
.apl-combo .apl-c-key { font-weight: 700; white-space: nowrap; color: var(--apl-muted); }
.apl-combo .apl-c-sep { font-weight: 700; color: var(--apl-muted-2); }
.apl-combo .apl-c-val {
    font-weight: 500;
    font-family: var(--apl-font-mono);
    padding-left: 1mm;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    min-width: 0;
    color: var(--apl-ink);
}
.apl-combo .apl-c-barcode {
    display: flex;
    flex-direction: column;
    align-items: center;
    width: 100%;
    flex-shrink: 0;
    margin-top: 0.4mm;
}
.apl-combo .apl-c-barcode svg { display: block; width: 100%; height: auto; }
.apl-combo .apl-barcode-text {
    font-family: var(--apl-font-mono);
    font-weight: 600;
    letter-spacing: 0.8mm;
    text-align: center;
    margin-top: 0.3mm;
    width: 100%;
    color: var(--apl-ink);
}
/* MRP band anchored to the bottom of the label */
.apl-combo .apl-c-price {
    display: flex;
    align-items: baseline;
    justify-content: center;
    gap: 1mm;
    flex-shrink: 0;
    margin-top: 0.4mm;
    padding-top: 0.5mm;
    border-top: 0.32mm solid var(--apl-hairline);
    font-weight: 700;
    font-family: var(--apl-font-mono);
    color: var(--apl-ink);
}
.apl-combo .apl-c-price .apl-mrp { font-weight: 600; font-family: var(--apl-font-sans); }

/* ── Design alignment ── */
/* Spotlight = centred, retail look; Detailed = left-aligned field grid. */
.apl-combo.apl-design-spotlight .apl-c-top { align-items: center; text-align: center; }
.apl-combo.apl-design-spotlight .apl-c-fields { justify-items: center; }
.apl-combo.apl-design-detailed .apl-c-top { align-items: stretch; text-align: left; }

/* Combo typography per stock size (shared); name sizes are tier × design below. */
.apl-combo.apl-large { font-size: 8.5pt; }
.apl-combo.apl-large .apl-c-company { font-size: 9pt; }
.apl-combo.apl-large .apl-c-meta { font-size: 9pt; }
.apl-combo.apl-large .apl-c-fits { font-size: 8.5pt; }
.apl-combo.apl-large .apl-c-price { font-size: 14pt; }
.apl-combo.apl-large .apl-barcode-text { font-size: 9pt; }
.apl-combo.apl-large .apl-c-barcode svg { max-height: 15mm; }

.apl-combo.apl-standard { font-size: 6pt; }
.apl-combo.apl-standard .apl-c-company { font-size: 6.5pt; }
.apl-combo.apl-standard .apl-c-meta { font-size: 6.5pt; }
.apl-combo.apl-standard .apl-c-fits { font-size: 6pt; }
.apl-combo.apl-standard .apl-c-price { font-size: 10pt; }
.apl-combo.apl-standard .apl-barcode-text { font-size: 6pt; letter-spacing: 0.4mm; }
.apl-combo.apl-standard .apl-c-barcode svg { max-height: 11mm; }

.apl-combo.apl-compact { font-size: 4.5pt; padding: 0.8mm 1mm; gap: 0.4mm; }
.apl-combo.apl-compact .apl-c-company { display: none; }
.apl-combo.apl-compact .apl-c-name { -webkit-line-clamp: 1; }
.apl-combo.apl-compact .apl-c-meta { font-size: 4.5pt; }
.apl-combo.apl-compact .apl-c-price { font-size: 8pt; }
.apl-combo.apl-compact .apl-barcode-text { font-size: 4.5pt; letter-spacing: 0.3mm; }
.apl-combo.apl-compact .apl-c-barcode svg { max-height: 8mm; }

.apl-combo.apl-tiny { font-size: 4pt; padding: 0.6mm 0.8mm; }
.apl-combo.apl-tiny .apl-c-company,
.apl-combo.apl-tiny .apl-c-top,
.apl-combo.apl-tiny .apl-c-price { display: none; }
.apl-combo.apl-tiny .apl-c-barcode svg { max-height: 9mm; }
.apl-combo.apl-tiny .apl-barcode-text { font-size: 4pt; letter-spacing: 0.2mm; }

.apl-combo.apl-custom { font-size: 7pt; }
.apl-combo.apl-custom .apl-c-company { font-size: 8pt; }
.apl-combo.apl-custom .apl-c-meta { font-size: 7pt; }
.apl-combo.apl-custom .apl-c-fits { font-size: 7pt; }
.apl-combo.apl-custom .apl-c-price { font-size: 10pt; }
.apl-combo.apl-custom .apl-barcode-text { font-size: 7pt; }
.apl-combo.apl-custom .apl-c-barcode svg { max-height: 14mm; }

/* ── Auto-fit product name: size = stock × design × length-tier ── */
/* Spotlight (headline) — as big as the stock allows. */
.apl-combo.apl-design-spotlight.apl-large    .apl-c-name--xl { font-size: 22pt; }
.apl-combo.apl-design-spotlight.apl-large    .apl-c-name--lg { font-size: 17pt; }
.apl-combo.apl-design-spotlight.apl-large    .apl-c-name--md { font-size: 13pt; }
.apl-combo.apl-design-spotlight.apl-large    .apl-c-name--sm { font-size: 11pt; }
.apl-combo.apl-design-spotlight.apl-standard .apl-c-name--xl { font-size: 13pt; }
.apl-combo.apl-design-spotlight.apl-standard .apl-c-name--lg { font-size: 11pt; }
.apl-combo.apl-design-spotlight.apl-standard .apl-c-name--md { font-size: 9.5pt; }
.apl-combo.apl-design-spotlight.apl-standard .apl-c-name--sm { font-size: 8pt; }
.apl-combo.apl-design-spotlight.apl-compact  .apl-c-name--xl { font-size: 8.5pt; }
.apl-combo.apl-design-spotlight.apl-compact  .apl-c-name--lg { font-size: 7.5pt; }
.apl-combo.apl-design-spotlight.apl-compact  .apl-c-name--md { font-size: 7pt; }
.apl-combo.apl-design-spotlight.apl-compact  .apl-c-name--sm { font-size: 6pt; }
.apl-combo.apl-design-spotlight.apl-custom   .apl-c-name--xl { font-size: 13pt; }
.apl-combo.apl-design-spotlight.apl-custom   .apl-c-name--lg { font-size: 11pt; }
.apl-combo.apl-design-spotlight.apl-custom   .apl-c-name--md { font-size: 10pt; }
.apl-combo.apl-design-spotlight.apl-custom   .apl-c-name--sm { font-size: 9pt; }
/* Detailed (capped so the field grid still fits). */
.apl-combo.apl-design-detailed.apl-large    .apl-c-name--xl { font-size: 14pt; }
.apl-combo.apl-design-detailed.apl-large    .apl-c-name--lg { font-size: 12pt; }
.apl-combo.apl-design-detailed.apl-large    .apl-c-name--md { font-size: 11pt; }
.apl-combo.apl-design-detailed.apl-large    .apl-c-name--sm { font-size: 10pt; }
.apl-combo.apl-design-detailed.apl-standard .apl-c-name--xl { font-size: 9.5pt; }
.apl-combo.apl-design-detailed.apl-standard .apl-c-name--lg { font-size: 8.5pt; }
.apl-combo.apl-design-detailed.apl-standard .apl-c-name--md { font-size: 7.5pt; }
.apl-combo.apl-design-detailed.apl-standard .apl-c-name--sm { font-size: 7pt; }
.apl-combo.apl-design-detailed.apl-compact  .apl-c-name--xl { font-size: 7pt; }
.apl-combo.apl-design-detailed.apl-compact  .apl-c-name--lg { font-size: 6.5pt; }
.apl-combo.apl-design-detailed.apl-compact  .apl-c-name--md { font-size: 6pt; }
.apl-combo.apl-design-detailed.apl-compact  .apl-c-name--sm { font-size: 5.5pt; }
.apl-combo.apl-design-detailed.apl-custom   .apl-c-name--xl { font-size: 10pt; }
.apl-combo.apl-design-detailed.apl-custom   .apl-c-name--lg { font-size: 9pt; }
.apl-combo.apl-design-detailed.apl-custom   .apl-c-name--md { font-size: 8pt; }
.apl-combo.apl-design-detailed.apl-custom   .apl-c-name--sm { font-size: 7.5pt; }
`;
