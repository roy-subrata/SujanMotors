/**
 * Single source of truth for the Warehouse Location label (100mm x 50mm).
 *
 * Structurally unrelated to the PRODUCT label system in
 * `features/inventory/parts/barcode-dialog/label-template.ts` (completely
 * different fields — zone/aisle/rack/bin vs. SKU/price/brand — no shared
 * markup), so it's kept as its own sibling file rather than merged in there.
 * It does reuse the same visual design language (ink, hairlines, IBM Plex
 * Sans/Mono via the same `--apl-*` custom-property names) so a printed sheet
 * that mixes product and location labels still looks like one system.
 *
 * Matches `design_handoff_barcode_labels` section 3 ("Warehouse Location
 * Label") exactly: a 10mm solid accent "ZONE {letter}" tab on the left,
 * store/category header, a large Aisle-Rack-Bin code + QR in the middle, and
 * a linear barcode + full location code at the bottom.
 */

export interface LocationLabelMarkupOptions {
    zone: string;
    aisle: string;
    rack: string;
    bin: string;
    /** Full "Zone-Aisle-Rack-Bin" code, e.g. "A-04-B-12" — encoded in both the QR and the barcode. */
    locationCode: string;
    /** Optional category hint shown top-right; omitted entirely when the location has no category. */
    categoryName?: string | null;
    /** Accent color for the zone tab — derived client-side, see `zone-color.ts`. */
    zoneColor: string;
    /** Inline QR SVG encoding `locationCode`. */
    qrSvg: string;
    /** Inline linear-barcode SVG encoding `locationCode`. */
    barcodeSvg: string;
}

function esc(value: string): string {
    return (value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

/**
 * Build the inner HTML for one 100mm x 50mm location label. Self-contained;
 * styling comes from {@link LOCATION_LABEL_CSS}.
 */
export function buildLocationLabelMarkup(o: LocationLabelMarkupOptions): string {
    const categoryRow = o.categoryName
        ? `<span class="wl-category">${esc(o.categoryName.toUpperCase())}</span>`
        : '';

    return `<div class="wl-label" style="width:100mm;height:50mm">
        <div class="wl-tab" style="background:${esc(o.zoneColor)}">
            <div class="wl-tab-text">ZONE ${esc((o.zone ?? '').toUpperCase())}</div>
        </div>
        <div class="wl-content">
            <div class="wl-header">
                <span class="wl-store">SUJAN MOTORS &middot; WAREHOUSE</span>
                ${categoryRow}
            </div>
            <div class="wl-middle">
                <div class="wl-code-block">
                    <div class="wl-caption">Aisle &middot; Rack &middot; Bin</div>
                    <div class="wl-code">${esc(o.aisle)}-${esc(o.rack)}-${esc(o.bin)}</div>
                </div>
                <div class="wl-qr">${o.qrSvg || ''}</div>
            </div>
            <div class="wl-bottom">
                <div class="wl-barcode">${o.barcodeSvg || ''}</div>
                <div class="wl-barcode-text">${esc(o.locationCode)}</div>
            </div>
        </div>
    </div>`;
}

/**
 * Canonical, print-safe styles for the location label. Scoped under
 * `.wl-label` so it can be injected globally (preview) or inlined into the
 * print window without leaking into the rest of the app. Reuses the same
 * `--apl-*` custom-property names/values as the product label system
 * (`barcode-dialog/label-template.ts`'s `LABEL_CSS`) for visual consistency,
 * defined locally here so this file has no runtime dependency on that one.
 */
export const LOCATION_LABEL_CSS = `
:root {
    --apl-ink: #1c1917;
    --apl-muted: #57534e;
    --apl-muted-2: #78716c;
    --apl-hairline: #1c1917;
    --apl-font-sans: 'IBM Plex Sans', 'Helvetica Neue', Arial, sans-serif;
    --apl-font-mono: 'IBM Plex Mono', 'Courier New', monospace;
}

.wl-label {
    box-sizing: border-box;
    background: #fff;
    color: var(--apl-ink);
    display: flex;
    font-family: var(--apl-font-sans);
    overflow: hidden;
}
.wl-label * { box-sizing: border-box; }

.wl-tab {
    width: 10mm;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
}
.wl-tab-text {
    color: #ffffff;
    font-size: 12.5px;
    font-weight: 700;
    letter-spacing: 2px;
    white-space: nowrap;
    writing-mode: vertical-rl;
    transform: rotate(180deg);
}

.wl-content {
    flex: 1;
    min-width: 0;
    padding: 4mm 5mm;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
}

.wl-header {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    gap: 2mm;
    padding-bottom: 2mm;
    border-bottom: 0.35mm solid var(--apl-hairline);
}
.wl-store {
    font-size: 8.5px;
    font-weight: 700;
    letter-spacing: 1.5px;
    color: var(--apl-ink);
    white-space: nowrap;
}
.wl-category {
    font-size: 8.5px;
    font-weight: 600;
    letter-spacing: 0.5px;
    color: var(--apl-muted-2);
    text-align: right;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    min-width: 0;
}

.wl-middle {
    display: flex;
    align-items: center;
    gap: 6mm;
}
.wl-code-block { flex: 1; min-width: 0; }
.wl-caption {
    font-size: 8.5px;
    letter-spacing: 2px;
    text-transform: uppercase;
    color: var(--apl-muted-2);
    font-weight: 600;
}
.wl-code {
    margin-top: 3px;
    font-size: 32px;
    font-weight: 700;
    font-family: var(--apl-font-mono);
    color: var(--apl-ink);
    letter-spacing: 1px;
    line-height: 1.1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.wl-qr {
    width: 22mm;
    height: 22mm;
    flex-shrink: 0;
    background: #ffffff;
    border: 0.3mm solid var(--apl-ink);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0.6mm;
}
.wl-qr svg { display: block; width: 100%; height: 100%; }

.wl-bottom {
    padding-top: 2mm;
    border-top: 0.35mm solid var(--apl-hairline);
}
.wl-barcode {
    display: flex;
    align-items: stretch;
    height: 10mm;
    background: #ffffff;
}
.wl-barcode svg { display: block; width: 100%; height: 100%; }
.wl-barcode-text {
    margin-top: 1.5mm;
    text-align: center;
    font-size: 9px;
    font-family: var(--apl-font-mono);
    font-weight: 600;
    letter-spacing: 3.5px;
    color: var(--apl-ink);
}
`;
