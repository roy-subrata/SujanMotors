import { Injectable, inject } from '@angular/core';
import { InvoicePdfService } from '../../sales/services/invoice-pdf.service';
import { WarrantyClaimResponse } from './warranty.service';

/**
 * Prints a warranty-claim document (acknowledgement slip while open, completion report when
 * resolved) via a hidden iframe — the same self-contained approach used by the thermal receipt
 * and A4 invoice, so it never disturbs the on-screen app.
 */
@Injectable({ providedIn: 'root' })
export class ClaimPrintService {
  private readonly invoicePdf = inject(InvoicePdfService);

  print(claim: WarrantyClaimResponse): void {
    this.printViaIframe(this.buildHtml(claim));
  }

  private buildHtml(c: WarrantyClaimResponse): string {
    const esc = (s: unknown) =>
      String(s ?? '').replace(/[&<>"']/g, ch =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch] as string));

    const shop = this.invoicePdf.getCompanyConfig();
    const completed = c.status === 'COMPLETED' || c.status === 'CLOSED';
    const title = completed ? 'Warranty Service Report' : 'Warranty Claim Acknowledgement';
    const fmtDate = (d?: Date | string) => d ? new Date(d).toLocaleString() : '-';
    const money = (n?: number) => (n == null ? '-' : this.invoicePdf.formatCurrency(n));

    const row = (label: string, value: string) =>
      `<tr><td class="k">${esc(label)}</td><td class="v">${value}</td></tr>`;

    return `<!doctype html><html><head><meta charset="utf-8"><title>${esc(c.claimNumber)}</title>
<style>
  @page { size: A4; margin: 14mm; }
  * { box-sizing: border-box; }
  body { font-family: 'Segoe UI', Arial, sans-serif; color: #1f2937; font-size: 13px; margin: 0; }
  .head { text-align: center; border-bottom: 2px solid #1976d2; padding-bottom: 10px; margin-bottom: 14px; }
  .shop { font-size: 20px; font-weight: 800; color: #1976d2; }
  .muted { color: #6b7280; font-size: 12px; }
  .doc-title { margin-top: 8px; font-size: 14px; font-weight: 700; letter-spacing: 1px; text-transform: uppercase; }
  table { width: 100%; border-collapse: collapse; margin: 4px 0 14px; }
  td { padding: 5px 6px; vertical-align: top; }
  td.k { color: #6b7280; width: 38%; }
  td.v { font-weight: 600; }
  .section { font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .5px;
             color: #374151; border-bottom: 1px solid #e5e7eb; padding-bottom: 4px; margin: 10px 0 4px; }
  .box { border: 1px solid #e5e7eb; border-radius: 6px; padding: 8px 10px; font-size: 12px; white-space: pre-wrap; }
  .status { display: inline-block; padding: 3px 12px; border-radius: 999px; font-weight: 700; font-size: 12px;
            border: 1.5px solid #1976d2; color: #1976d2; }
  .foot { margin-top: 26px; display: flex; justify-content: space-between; font-size: 12px; }
  .sign { width: 45%; border-top: 1px solid #9ca3af; padding-top: 4px; text-align: center; color: #6b7280; }
  .thanks { text-align: center; margin-top: 18px; color: #6b7280; font-size: 12px; }
</style></head><body>
  <div class="head">
    <div class="shop">${esc(shop.companyName || 'Auto Part Shop')}</div>
    ${shop.companyAddress ? `<div class="muted">${esc(shop.companyAddress)}</div>` : ''}
    ${shop.companyPhone ? `<div class="muted">Tel: ${esc(shop.companyPhone)}</div>` : ''}
    <div class="doc-title">${esc(title)}</div>
  </div>

  <div class="section">Claim</div>
  <table>
    ${row('Claim No.', esc(c.claimNumber))}
    ${row('Status', `<span class="status">${esc(c.status)}</span>`)}
    ${row('Claim Date', esc(fmtDate(c.claimDate)))}
    ${row('Service Type', esc(c.serviceType))}
    ${c.warrantyNumber ? row('Warranty No.', esc(c.warrantyNumber)) : ''}
  </table>

  <div class="section">Customer & Item</div>
  <table>
    ${row('Customer', esc(c.customerName))}
    ${c.customerPhone ? row('Phone', esc(c.customerPhone)) : ''}
    ${row('Item', esc(c.partName))}
    ${c.partSKU ? row('SKU', esc(c.partSKU)) : ''}
    ${c.technicianName ? row('Technician', esc(c.technicianName)) : ''}
  </table>

  <div class="section">Reported Issue</div>
  <div class="box">${esc(c.issueDescription || '-')}</div>

  ${completed ? `
  <div class="section">Resolution</div>
  <table>
    ${row('Resolution', esc(c.resolutionDetails || '-'))}
    ${row('Service Cost', esc(money(c.serviceCost)))}
    ${row('Completed', esc(fmtDate(c.serviceCompletedDate)))}
  </table>` : ''}

  <div class="foot">
    <div class="sign">Customer Signature</div>
    <div class="sign">Authorized Signature</div>
  </div>
  <div class="thanks">Please retain this document for any future reference to claim ${esc(c.claimNumber)}.</div>
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
    win.onafterprint = () => setTimeout(cleanup, 200);
    setTimeout(() => {
      try { win.focus(); win.print(); } catch { cleanup(); }
      setTimeout(cleanup, 60000);
    }, 250);
  }
}
