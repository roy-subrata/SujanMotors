import { Injectable } from '@angular/core';
import { InvoicePdfData } from './invoice-pdf.service';

/**
 * Prints a compact 80mm thermal receipt for a completed sale.
 *
 * Renders the invoice data into a self-contained, monospace HTML document sized for an
 * 80mm roll (72mm printable) and prints it via a hidden iframe so it never disturbs the
 * on-screen app. Reuses the same {@link InvoicePdfData} the A4 invoice/preview already builds.
 */
@Injectable({ providedIn: 'root' })
export class ThermalReceiptService {
  /**
   * @param data    the completed-sale invoice data
   * @param fmt     money formatter (pass the caller's formatCurrency for consistent symbol/locale)
   */
  print(data: InvoicePdfData, fmt: (n: number) => string): void {
    const html = this.buildHtml(data, fmt);
    this.printViaIframe(html);
  }

  private buildHtml(d: InvoicePdfData, fmt: (n: number) => string): string {
    const esc = (s: unknown) =>
      String(s ?? '').replace(/[&<>"']/g, c =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c] as string));

    const date = (d.invoiceDate instanceof Date ? d.invoiceDate : new Date(d.invoiceDate))
      .toLocaleString();

    const items = d.items.map(it => `
      <div class="item">
        <div class="desc">${esc(it.description || it.partNumber)}</div>
        <div class="line">
          <span>${it.quantity} × ${fmt(it.unitPrice)}${it.discount ? ' (-' + fmt(it.discount) + ')' : ''}</span>
          <span>${fmt(it.total)}</span>
        </div>
      </div>`).join('');

    const totalRow = (label: string, value: string, cls = '') =>
      `<div class="line ${cls}"><span>${label}</span><span>${value}</span></div>`;

    const payments = (d.payments ?? [])
      .map(p => totalRow(esc(p.method), fmt(p.amount)))
      .join('');

    const change = d.paidAmount > d.grandTotal ? d.paidAmount - d.grandTotal : 0;

    return `<!doctype html><html><head><meta charset="utf-8"><title>Receipt ${esc(d.invoiceNumber)}</title>
<style>
  @page { size: 80mm auto; margin: 0; }
  * { box-sizing: border-box; }
  html, body { margin: 0; padding: 0; }
  body { width: 72mm; padding: 3mm 2mm; font-family: 'Courier New', monospace; color: #000;
         font-size: 12px; line-height: 1.35; }
  .center { text-align: center; }
  .shop { font-size: 15px; font-weight: 700; }
  .muted { font-size: 11px; }
  .hr { border-top: 1px dashed #000; margin: 6px 0; }
  .line { display: flex; justify-content: space-between; gap: 6px; }
  .line > span:last-child { text-align: right; white-space: nowrap; }
  .item { margin: 3px 0; }
  .item .desc { font-weight: 600; }
  .item .line { font-size: 11px; }
  .grand { font-size: 14px; font-weight: 700; }
  .totals .line { margin: 2px 0; }
  .foot { margin-top: 8px; text-align: center; font-size: 11px; }
</style></head><body>
  <div class="center">
    <div class="shop">${esc(d.companyName || 'Receipt')}</div>
    ${d.companyAddress ? `<div class="muted">${esc(d.companyAddress)}</div>` : ''}
    ${d.companyPhone ? `<div class="muted">${esc(d.companyPhone)}</div>` : ''}
    ${d.companyTaxId ? `<div class="muted">TIN: ${esc(d.companyTaxId)}</div>` : ''}
  </div>
  <div class="hr"></div>
  <div class="line"><span>Invoice</span><span>${esc(d.invoiceNumber)}</span></div>
  <div class="line"><span>Date</span><span>${esc(date)}</span></div>
  <div class="line"><span>Customer</span><span>${esc(d.customerName)}</span></div>
  ${d.customerPhone ? `<div class="line"><span>Phone</span><span>${esc(d.customerPhone)}</span></div>` : ''}
  <div class="hr"></div>
  ${items}
  <div class="hr"></div>
  <div class="totals">
    ${totalRow('Subtotal', fmt(d.subtotal))}
    ${d.discountAmount > 0 ? totalRow('Discount', '-' + fmt(d.discountAmount)) : ''}
    ${d.vatAmount > 0 ? totalRow('VAT (' + d.vatPercentage + '%)', fmt(d.vatAmount)) : ''}
    ${totalRow('TOTAL', fmt(d.grandTotal), 'grand')}
    ${payments}
    ${totalRow('Paid', fmt(d.paidAmount))}
    ${change > 0 ? totalRow('Change', fmt(change)) : ''}
    ${d.dueAmount > 0 ? totalRow('Due', fmt(d.dueAmount)) : ''}
  </div>
  ${d.notes ? `<div class="hr"></div><div class="muted">${esc(d.notes)}</div>` : ''}
  <div class="foot">${esc(d.paymentTerms || 'Thank you for your business!')}</div>
</body></html>`;
  }

  private printViaIframe(html: string): void {
    const iframe = document.createElement('iframe');
    iframe.setAttribute('aria-hidden', 'true');
    Object.assign(iframe.style, {
      position: 'fixed', right: '0', bottom: '0', width: '0', height: '0', border: '0',
    } as CSSStyleDeclaration);
    document.body.appendChild(iframe);

    const win = iframe.contentWindow;
    if (!win) { document.body.removeChild(iframe); return; }

    const doc = win.document;
    doc.open();
    doc.write(html);
    doc.close();

    const cleanup = () => { if (iframe.parentNode) iframe.parentNode.removeChild(iframe); };
    // Give the browser a tick to lay out the content before printing.
    win.onafterprint = () => setTimeout(cleanup, 200);
    setTimeout(() => {
      try { win.focus(); win.print(); } catch { cleanup(); }
      // Fallback cleanup in case onafterprint never fires (some browsers).
      setTimeout(cleanup, 60000);
    }, 250);
  }
}
