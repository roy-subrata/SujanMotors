import { Injectable, inject } from '@angular/core';
import { InvoicePdfData } from './invoice-pdf.service';
import { I18nService } from '@/shared/services/i18n.service';

/**
 * Prints a compact 80mm thermal receipt for a completed sale.
 *
 * Renders the invoice data into a self-contained, monospace HTML document sized for an
 * 80mm roll (72mm printable) and prints it via a hidden iframe so it never disturbs the
 * on-screen app. Reuses the same {@link InvoicePdfData} the A4 invoice/preview already builds.
 */
@Injectable({ providedIn: 'root' })
export class ThermalReceiptService {
    private readonly i18n = inject(I18nService);

    /** Shorthand for the receipt's own label namespace. */
    private t(key: string, params?: Record<string, string | number>): string {
        return this.i18n.t(`thermalReceipt.${key}`, params);
    }

    /**
     * @param data    the completed-sale invoice data
     * @param fmt     money formatter (pass the caller's formatCurrency for consistent symbol/locale)
     */
    print(data: InvoicePdfData, fmt: (n: number) => string): void {
        const html = this.buildReceiptHtml(data, fmt);
        this.printViaIframe(html);
    }

    /**
     * Builds the same self-contained 80mm HTML document {@link print} sends to the hidden
     * iframe — exposed publicly so the on-screen thermal preview (pos-print-preview) renders
     * byte-for-byte the same markup instead of a hand-maintained duplicate that could drift.
     */
    buildReceiptHtml(d: InvoicePdfData, fmt: (n: number) => string): string {
        const esc = (s: unknown) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c] as string);

        const dt = d.invoiceDate instanceof Date ? d.invoiceDate : new Date(d.invoiceDate);
        const date = dt.toLocaleDateString();
        const time = dt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        // Only print a real, uploaded logo — skip bundled placeholders that won't mean anything on paper.
        const hasLogo = !!d.companyLogo && !d.companyLogo.startsWith('assets');

        const totalQty = d.items.reduce((sum, it) => sum + (it.quantity || 0), 0);

        const items = d.items
            .map(
                (it, i) => `
      <div class="item">
        <div class="name">${i + 1}. ${esc(it.description || it.partNumber)}</div>
        <div class="line">
          <span class="qty">${it.quantity} × ${fmt(it.unitPrice)}${it.discount ? ' − ' + fmt(it.discount) : ''}</span>
          <span class="amt">${fmt(it.total)}</span>
        </div>
      </div>`
            )
            .join('');

        const totalRow = (label: string, value: string, cls = '') => `<div class="trow ${cls}"><span class="lbl">${label}</span><span class="val">${value}</span></div>`;

        const methodLabel = (method: string) => {
            const key = `paymentMethods.pos.${method}`;
            const label = this.i18n.t(key);
            return label === key ? method : label;
        };

        const payments = (d.payments ?? []).map((p) => totalRow(esc(methodLabel(p.method)), fmt(p.amount), 'sub')).join('');

        const change = d.changeDue ?? (d.paidAmount > d.grandTotal ? d.paidAmount - d.grandTotal : 0);

        const status = d.dueAmount > 0.001 ? `<div class="status due">${esc(this.t('statusDue'))} ${fmt(d.dueAmount)}</div>` : `<div class="status paid">${esc(this.t('statusPaidInFull'))}</div>`;

        // Colors/rules mirror design_handoff_pos_sale README §5a (mono, ink-on-paper, dashed
        // section rules) — this HTML is shared byte-for-byte between the on-screen preview and the
        // actual print job (see buildReceiptHtml's own doc comment), so restyling it here fixes both
        // at once instead of drifting a preview-only copy. `'JetBrains Mono'` falls back to a plain
        // monospace stack if a thermal printer's own renderer has no webfont access — appropriate
        // either way for a receipt (columns align on digits).
        return `<!doctype html><html><head><meta charset="utf-8"><title>${esc(this.t('receiptTitle'))} ${esc(d.invoiceNumber)}</title>
<style>
  @page { size: 80mm auto; margin: 0; }
  * { box-sizing: border-box; }
  html, body { margin: 0; padding: 0; }
  body { width: 72mm; padding: 4mm 3mm; color: #17171a;
         font-family: 'JetBrains Mono', 'Courier New', monospace;
         font-size: 12px; line-height: 1.55; -webkit-font-smoothing: antialiased;
         font-variant-numeric: tabular-nums; }
  .center { text-align: center; }
  .logo { max-width: 42mm; max-height: 16mm; object-fit: contain; display: block;
          margin: 0 auto 4px; filter: grayscale(1) contrast(1.15); }
  .shop { font-size: 15px; font-weight: 700; letter-spacing: .4px; text-transform: uppercase; line-height: 1.2; }
  .muted { font-size: 11px; color: #17171a; line-height: 1.5; }
  .doc { margin: 6px 0 2px; font-size: 11px; font-weight: 700; letter-spacing: 2px; text-transform: uppercase; color: #17171a; }
  .rule { border: 0; border-top: 1px dashed #17171a; margin: 8px 0; }
  .rule.thin { border-top: 1px dashed #17171a; }
  .meta { font-size: 11px; }
  .meta .mrow { display: flex; justify-content: space-between; gap: 8px; margin: 2px 0; }
  .meta .mrow .k { color: #17171a; }
  .meta .mrow .v { font-weight: 600; text-align: right; }
  .ihead { display: flex; justify-content: space-between; font-size: 11px; font-weight: 700;
           text-transform: uppercase; letter-spacing: .3px; color: #17171a; padding-bottom: 2px; }
  .item { margin: 7px 0; }
  .item .name { font-weight: 700; font-size: 12px; line-height: 1.3; }
  .item .line { display: flex; justify-content: space-between; gap: 6px; font-size: 11px; margin-top: 1px; }
  .item .line .amt { font-weight: 600; white-space: nowrap; }
  .totals { font-size: 12px; }
  .trow { display: flex; justify-content: space-between; gap: 6px; margin: 3px 0; }
  .trow .lbl { color: #17171a; }
  .trow .val { font-weight: 600; white-space: nowrap; text-align: right; }
  .trow.sub .lbl, .trow.sub .val { font-weight: 500; font-size: 11px; }
  .grand { font-size: 16px; font-weight: 700; padding: 2px 0; margin: 3px 0; }
  .grand .lbl { letter-spacing: .5px; }
  .status { text-align: center; font-size: 11px; font-weight: 700; letter-spacing: 1px;
            padding: 4px; margin: 8px 0 2px; }
  .notes { font-size: 11px; }
  .foot { margin-top: 10px; text-align: center; line-height: 1.6; }
  .foot .thanks { font-size: 12px; font-weight: 700; letter-spacing: .3px; }
  .foot .terms { font-size: 11px; margin-top: 3px; line-height: 1.5; }
  .foot .barcode { margin-top: 8px; letter-spacing: -1px; font-size: 22px; }
  .foot .sys { font-size: 10px; margin-top: 8px; letter-spacing: .2px; }
</style></head><body>
  <div class="center">
    ${hasLogo ? `<img class="logo" src="${esc(d.companyLogo)}" alt="">` : ''}
    <div class="shop">${esc(d.companyName || this.t('fallbackShopName'))}</div>
    ${d.companyAddress ? `<div class="muted">${esc(d.companyAddress)}</div>` : ''}
    ${d.companyPhone ? `<div class="muted">${esc(this.t('tel'))} ${esc(d.companyPhone)}</div>` : ''}
    ${d.companyTaxId ? `<div class="muted">${esc(this.t('tin'))} ${esc(d.companyTaxId)}</div>` : ''}
    <div class="doc">${esc(this.t('docType'))}</div>
  </div>
  <hr class="rule">
  <div class="meta">
    <div class="mrow"><span class="k">${esc(this.t('invoice'))}</span><span class="v">${esc(d.invoiceNumber)}</span></div>
    <div class="mrow"><span class="k">${esc(this.t('date'))}</span><span class="v">${esc(date)} ${esc(time)}</span></div>
    <div class="mrow"><span class="k">${esc(this.t('customer'))}</span><span class="v">${esc(d.customerName)}</span></div>
    ${d.customerPhone ? `<div class="mrow"><span class="k">${esc(this.t('phone'))}</span><span class="v">${esc(d.customerPhone)}</span></div>` : ''}
    ${d.createdBy ? `<div class="mrow"><span class="k">${esc(this.t('servedBy'))}</span><span class="v">${esc(d.createdBy)}</span></div>` : ''}
  </div>
  <hr class="rule">
  <div class="ihead"><span>${esc(this.t('item'))}</span><span>${esc(this.t('amount'))}</span></div>
  <hr class="rule thin">
  ${items}
  <hr class="rule">
  <div class="totals">
    ${totalRow(esc(this.t(totalQty === 1 ? 'subtotalOne' : 'subtotalMany', { count: totalQty })), fmt(d.subtotal))}
    ${d.discountAmount > 0 ? totalRow(esc(this.t('discount')), '− ' + fmt(d.discountAmount)) : ''}
    ${d.vatAmount > 0 ? totalRow(esc(this.t('vat', { percent: d.vatPercentage })), fmt(d.vatAmount)) : ''}
    ${totalRow(esc(this.t('total')), fmt(d.grandTotal), 'grand')}
    ${payments}
    ${totalRow(esc(this.t('paid')), fmt(d.paidAmount))}
    ${change > 0 ? totalRow(esc(this.t('change')), fmt(change)) : ''}
    ${d.dueAmount > 0 ? totalRow(esc(this.t('due')), fmt(d.dueAmount)) : ''}
  </div>
  ${status}
  ${d.notes ? `<hr class="rule thin"><div class="notes">${esc(d.notes)}</div>` : ''}
  <div class="foot">
    <div class="thanks">${esc(this.t('thanks'))}</div>
    <div class="terms">${esc(d.paymentTerms || this.t('defaultTerms'))}</div>
    <div class="barcode">▌│█║▌║▌║█│▌║</div>
    <div class="terms">${esc(d.invoiceNumber)}</div>
    <div class="sys">${esc(this.t('poweredBy', { company: d.companyName || this.t('fallbackCompany') }))}</div>
  </div>
</body></html>`;
    }

    private printViaIframe(html: string): void {
        const iframe = document.createElement('iframe');
        iframe.setAttribute('aria-hidden', 'true');
        Object.assign(iframe.style, {
            position: 'fixed',
            right: '0',
            bottom: '0',
            width: '0',
            height: '0',
            border: '0'
        } as CSSStyleDeclaration);
        document.body.appendChild(iframe);

        const win = iframe.contentWindow;
        if (!win) {
            document.body.removeChild(iframe);
            return;
        }

        const doc = win.document;
        doc.open();
        doc.write(html);
        doc.close();

        const cleanup = () => {
            if (iframe.parentNode) iframe.parentNode.removeChild(iframe);
        };
        // Give the browser a tick to lay out the content before printing.
        win.onafterprint = () => setTimeout(cleanup, 200);
        setTimeout(() => {
            try {
                win.focus();
                win.print();
            } catch {
                cleanup();
            }
            // Fallback cleanup in case onafterprint never fires (some browsers).
            setTimeout(cleanup, 60000);
        }, 250);
    }
}
