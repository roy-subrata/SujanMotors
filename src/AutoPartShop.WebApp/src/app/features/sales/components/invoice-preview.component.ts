import { Component, Input, Output, EventEmitter, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { DividerModule } from 'primeng/divider';
import { InvoicePdfService, InvoicePdfData } from '../services/invoice-pdf.service';

@Component({
  selector: 'app-invoice-preview',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ButtonModule,
    TooltipModule,
    DividerModule
  ],
  template: `
    <p-dialog
      [(visible)]="visible"
      [header]="'Invoice Preview - ' + invoiceData?.invoiceNumber"
      [modal]="true"
      [style]="{ width: '900px', maxWidth: '95vw' }"
      [maximizable]="true"
      [draggable]="false"
      [resizable]="false"
      styleClass="invoice-preview-dialog"
    >
      <!-- Dialog Header Actions -->
      <ng-template pTemplate="header">
        <div class="dialog-header">
          <div class="header-title">
            <i class="pi pi-file-pdf"></i>
            <span>Invoice Preview - {{ invoiceData?.invoiceNumber }}</span>
          </div>
        </div>
      </ng-template>

      <!-- Invoice Content -->
      <div class="invoice-container" id="invoice-print-area">
        <div class="invoice-paper" *ngIf="invoiceData">
          <!-- Header -->
          <div class="header">
            <div class="company-block">
              <img *ngIf="invoiceData.companyLogo && !invoiceData.companyLogo.startsWith('assets')"
                   [src]="invoiceData.companyLogo" class="company-logo" alt="logo">
              <div class="company-name">{{ invoiceData.companyName }}</div>
              <div class="company-detail" *ngIf="invoiceData.companyAddress">{{ invoiceData.companyAddress }}</div>
              <div class="company-detail" *ngIf="invoiceData.companyPhone">{{ invoiceData.companyPhone }}</div>
              <div class="company-detail" *ngIf="invoiceData.companyEmail">{{ invoiceData.companyEmail }}</div>
              <div class="company-tax" *ngIf="invoiceData.companyTaxId">{{ invoiceData.companyTaxId }}</div>
            </div>
            <div class="title-section">
              <h1>Invoice</h1>
              <table class="meta-table">
                <tr><td>Invoice no.</td><td>{{ invoiceData.invoiceNumber }}</td></tr>
                <tr><td>Date</td><td>{{ formatDate(invoiceData.invoiceDate) }}</td></tr>
                <tr *ngIf="invoiceData.dueDate"><td>Due date</td><td>{{ formatDate(invoiceData.dueDate) }}</td></tr>
                <tr *ngIf="invoiceData.salesOrderNumber"><td>SO #</td><td>{{ invoiceData.salesOrderNumber }}</td></tr>
              </table>
            </div>
          </div>

          <!-- Address Section -->
          <div class="address-section">
            <div class="address-block">
              <div class="address-label">From</div>
              <div class="address-name">{{ invoiceData.companyName }}</div>
              <div class="address-detail">
                {{ invoiceData.companyAddress }}<br>
                {{ invoiceData.companyEmail }}<br>
                {{ invoiceData.companyPhone }}
              </div>
            </div>
            <div class="address-block right">
              <div class="address-label">Bill to</div>
              <div class="address-name">{{ invoiceData.customerName }}</div>
              <div class="address-detail">
                <span *ngIf="invoiceData.customerAddress">{{ invoiceData.customerAddress }}<br></span>
                <span *ngIf="invoiceData.customerEmail">{{ invoiceData.customerEmail }}<br></span>
                <span *ngIf="invoiceData.customerPhone">{{ invoiceData.customerPhone }}</span>
              </div>
              <div class="tech-block" *ngIf="invoiceData.technicianName">
                <div class="address-label">Technician</div>
                <div class="address-detail">
                  {{ invoiceData.technicianName }}<span *ngIf="invoiceData.technicianPhone"> | {{ invoiceData.technicianPhone }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Items Table -->
          <table class="items-table">
            <thead>
              <tr>
                <th>Description</th>
                <th class="num-col">Unit Price</th>
                <th class="num-col">Qty</th>
                <th class="num-col">Disc</th>
                <th class="num-col">Amount</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of invoiceData.items; let i = index">
                <td class="desc-cell">
                  <div class="item-name">{{ item.description }}</div>
                  <div class="item-desc">{{ item.partNumber || '-' }}</div>
                </td>
                <td class="num-cell">{{ formatCurrency(item.unitPrice) }}</td>
                <td class="num-cell">{{ item.quantity }}</td>
                <td class="num-cell">{{ item.discount > 0 ? item.discount + '%' : '-' }}</td>
                <td class="num-cell">{{ formatCurrency(item.total) }}</td>
              </tr>
              <tr *ngFor="let empty of getEmptyRows()" class="empty-row">
                <td>&nbsp;</td>
                <td></td>
                <td></td>
                <td></td>
                <td></td>
              </tr>
            </tbody>
          </table>

          <!-- Summary Section -->
          <div class="summary-section">
            <div class="payment-info">
              <div *ngIf="invoiceData.notes">
                <h4>Notes</h4>
                <p>{{ invoiceData.notes }}</p>
              </div>
              <div class="amount-words" *ngIf="amountInWords">
                <h4>Amount in Words</h4>
                <p>{{ amountInWords }}</p>
              </div>
              <div *ngIf="invoiceData.paymentTerms">
                <h4>Payment Terms</h4>
                <p>{{ invoiceData.paymentTerms }}</p>
              </div>
            </div>
            <div class="totals-box">
              <div class="totals-row">
                <span class="totals-label">Subtotal:</span>
                <span class="totals-value">{{ formatCurrency(invoiceData.subtotal) }}</span>
              </div>
              <div class="totals-row" *ngIf="invoiceData.discountAmount > 0">
                <span class="totals-label">Discount:</span>
                <span class="totals-value">-{{ formatCurrency(invoiceData.discountAmount) }}</span>
              </div>
              <div class="totals-row">
                <span class="totals-label">VAT ({{ invoiceData.vatPercentage }}%):</span>
                <span class="totals-value">{{ formatCurrency(invoiceData.vatAmount) }}</span>
              </div>
              <div class="totals-row total">
                <span class="totals-label">Total:</span>
                <span class="totals-value">{{ formatCurrency(invoiceData.grandTotal) }}</span>
              </div>
              <div class="totals-row" *ngIf="invoiceData.paidAmount > 0">
                <span class="totals-label">Paid:</span>
                <span class="totals-value">{{ formatCurrency(invoiceData.paidAmount) }}</span>
              </div>
              <div class="totals-row" *ngIf="invoiceData.dueAmount > 0">
                <span class="totals-label">Balance Due:</span>
                <span class="totals-value due">{{ formatCurrency(invoiceData.dueAmount) }}</span>
              </div>
            </div>
          </div>

          <!-- Payment Details -->
          <div class="payment-details" *ngIf="invoiceData.payments.length > 0">
            <h4><i class="pi pi-credit-card"></i> Payment Details</h4>
            <div class="payment-list">
              <div class="payment-item" *ngFor="let payment of invoiceData.payments">
                <span class="payment-method">{{ getPaymentMethodLabel(payment.method) }}</span>
                <span class="payment-amount">{{ formatCurrency(payment.amount) }}</span>
                <span class="payment-ref" *ngIf="payment.reference">Ref: {{ payment.reference }}</span>
              </div>
            </div>
          </div>

          <!-- Footer -->
          <div class="footer">
            <p>Thank you for choosing {{ invoiceData.companyName }}</p>
            <p class="generated-at">Generated on {{ currentDateTime }}</p>
          </div>
        </div>
      </div>

      <!-- Dialog Footer -->
      <ng-template pTemplate="footer">
        <div class="dialog-footer">
          <div class="footer-left-actions">
            <button
              pButton
              label="Thermal Print"
              icon="pi pi-print"
              class="p-button-outlined p-button-secondary"
              pTooltip="Print on thermal printer (POS)"
              (click)="printThermal()"
            ></button>
          </div>
          <div class="footer-right-actions">
            <button
              pButton
              label="Download PDF"
              icon="pi pi-download"
              class="p-button-info"
              pTooltip="Download as PDF file"
              [loading]="downloading()"
              (click)="downloadPdf()"
            ></button>
            <button
              pButton
              label="Print"
              icon="pi pi-print"
              class="p-button-success"
              pTooltip="Print invoice (Ctrl+P)"
              (click)="printInvoice()"
            ></button>
            <button
              pButton
              label="Close"
              icon="pi pi-times"
              class="p-button-secondary p-button-outlined"
              (click)="close()"
            ></button>
          </div>
        </div>
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    /* Dialog Styling */
    :host ::ng-deep .invoice-preview-dialog {
      .p-dialog-header {
        background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%);
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 8px 8px 0 0;
      }

      .p-dialog-content {
        padding: 0;
        background: #f0f2f5;
      }

      .p-dialog-footer {
        padding: 1rem 1.5rem;
        background: #f8f9fa;
        border-top: 1px solid #e2e8f0;
      }
    }

    .dialog-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;

      .header-title {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        font-size: 1.1rem;
        font-weight: 600;

        i { font-size: 1.25rem; }
      }
    }

    .dialog-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;

      .footer-right-actions {
        display: flex;
        gap: 0.75rem;
      }
    }

    /* Invoice Container */
    .invoice-container {
      max-height: 70vh;
      overflow-y: auto;
      padding: 1.5rem;
      background: #f0f2f5;
    }

    .invoice-paper {
      background: white;
      max-width: 800px;
      margin: 0 auto;
      padding: 20px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
      border-radius: 4px;
      font-family: 'Segoe UI', Arial, sans-serif;
      color: #333;
    }

    /* Header */
    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 24px;
      margin-bottom: 20px;
    }

    /* Left column: grows, never squeezes the right */
    .company-block {
      flex: 3 1 0;
      min-width: 0;
    }

    .company-logo {
      max-height: 44px;
      max-width: 140px;
      object-fit: contain;
      margin-bottom: 6px;
      display: block;
    }

    .company-name {
      font-size: 18px;
      font-weight: 700;
      color: #1976d2;
      line-height: 1.25;
      margin-bottom: 2px;
    }

    .company-detail {
      font-size: 11px;
      color: #666;
      line-height: 1.45;
    }

    .company-tax {
      margin-top: 3px;
      font-size: 11px;
      color: #333;
      font-weight: 600;
    }

    /* Right column: fixed proportion, right-aligned */
    .title-section {
      flex: 2 0 auto;
      text-align: right;
    }

    .title-section h1 {
      font-size: 26px;
      color: #1976d2;
      font-weight: 300;
      margin-bottom: 10px;
    }

    /* Two-column meta table: label left, value right */
    .meta-table {
      border-collapse: collapse;
      width: 100%;
    }

    .meta-table td {
      padding: 2px 0;
      font-size: 11px;
      vertical-align: middle;
    }

    .meta-table td:first-child {
      color: #999;
      text-align: left;
      white-space: nowrap;
      padding-right: 12px;
    }

    .meta-table td:last-child {
      color: #333;
      font-weight: 500;
      text-align: right;
    }

    /* Address Section */
    .address-section {
      display: flex;
      justify-content: space-between;
      margin-bottom: 20px;
      padding-bottom: 15px;
      border-bottom: 1px solid #e0e0e0;
    }

    .address-block {
      flex: 1;
    }

    .address-block.right {
      text-align: right;
    }

    .address-label {
      font-size: 10px;
      color: #999;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-bottom: 4px;
    }

    .address-name {
      font-size: 14px;
      font-weight: 600;
      color: #333;
      margin-bottom: 4px;
    }

    .address-detail {
      font-size: 11px;
      color: #666;
      line-height: 1.5;
    }

    .tech-block {
      margin-top: 10px;
    }

    .items-table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 20px;
      font-size: 11px;
    }

    .items-table th {
      background: #1976d2;
      color: white;
      padding: 10px 8px;
      font-size: 10px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      font-weight: 500;
      text-align: left;
    }

    .items-table th.num-col {
      text-align: right;
    }

    .items-table td {
      padding: 10px 8px;
      border-bottom: 1px solid #eee;
      vertical-align: top;
    }

    .items-table tr:last-child td {
      border-bottom: none;
    }

    .desc-cell {
      width: 40%;
    }

    .num-cell {
      text-align: right;
      width: 15%;
    }

    .item-name {
      font-weight: 500;
      color: #333;
    }

    .item-desc {
      font-size: 10px;
      color: #999;
      margin-top: 2px;
    }

    .empty-row td {
      height: 28px;
      border-bottom: 1px solid #eee;
    }

    /* Summary Section */
    .summary-section {
      display: flex;
      gap: 1.5rem;
      margin-bottom: 1.5rem;
    }

    .payment-info {
      flex: 1;
      padding-right: 20px;
    }

    .payment-info h4 {
      font-size: 11px;
      color: #999;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-bottom: 6px;
    }

    .payment-info p {
      font-size: 11px;
      color: #666;
      line-height: 1.6;
      margin-bottom: 10px;
    }

    .totals-box {
      width: 260px;
    }

    .totals-row {
      display: flex;
      justify-content: space-between;
      padding: 6px 0;
      font-size: 11px;
    }

    .totals-row.total {
      border-top: 2px solid #1976d2;
      margin-top: 8px;
      padding-top: 10px;
      font-size: 14px;
      font-weight: 600;
      color: #1976d2;
    }

    .totals-label {
      color: #666;
    }

    .totals-value {
      font-weight: 500;
    }

    .totals-value.due {
      color: #d32f2f;
      font-weight: 700;
    }

    /* Payment Details */
    .payment-details {
      margin-bottom: 15px;
      padding: 12px;
      background: #f9f9f9;
      border: 1px solid #e0e0e0;
      border-radius: 4px;
    }

    .payment-details h4 {
      font-size: 11px;
      color: #1976d2;
      margin-bottom: 6px;
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .payment-list {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }

    .payment-item {
      font-size: 11px;
      color: #666;
      background: white;
      border: 1px solid #eee;
      padding: 6px 8px;
      border-radius: 4px;
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .payment-amount {
      font-weight: 600;
      color: #1976d2;
    }

    .payment-ref {
      color: #999;
      font-size: 10px;
    }

    /* Footer */
    .footer {
      text-align: center;
      color: #999;
      font-size: 10px;
      padding-top: 10px;
      border-top: 1px solid #eee;
    }

    .footer .generated-at {
      margin-top: 4px;
    }

    /* Print / PDF Styles */
    @media print {
      .invoice-container {
        padding: 0;
        background: white;
        max-height: none;
        overflow: visible;
      }

      .invoice-paper {
        box-shadow: none;
        padding: 0;
      }

      /* Re-assert header flex for print engines */
      .header {
        display: flex !important;
        justify-content: space-between !important;
        align-items: flex-start !important;
        gap: 24px !important;
        page-break-inside: avoid;
      }

      .company-block { flex: 3 1 0 !important; }
      .title-section { flex: 2 0 auto !important; text-align: right !important; }
    }
  `]
})
export class InvoicePreviewComponent implements OnInit {
  private readonly pdfService = inject(InvoicePdfService);

  @Input() visible = false;
  @Input() invoiceData: InvoicePdfData | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() onPrint = new EventEmitter<void>();
  @Output() onDownload = new EventEmitter<void>();

  downloading = signal(false);
  amountInWords = '';
  currentDateTime = '';

  ngOnInit(): void {
    this.updateDerivedValues();
  }

  ngOnChanges(): void {
    this.updateDerivedValues();
  }

  private updateDerivedValues(): void {
    if (this.invoiceData) {
      this.amountInWords = this.pdfService.numberToWords(this.invoiceData.grandTotal);
      this.currentDateTime = new Date().toLocaleString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    }
  }

  formatCurrency(amount: number): string {
    return this.pdfService.formatCurrency(amount);
  }

  formatDate(date: Date | string): string {
    return this.pdfService.formatDate(date);
  }

  getPaymentMethodLabel(method: string): string {
    return this.pdfService.getPaymentMethodLabel(method);
  }

  getEmptyRows(): number[] {
    const itemCount = this.invoiceData?.items.length || 0;
    const minRows = 5;
    const emptyCount = Math.max(0, minRows - itemCount);
    return Array(emptyCount).fill(0);
  }

  printInvoice(): void {
    this.pdfService.printInvoice('invoice-print-area');
    this.onPrint.emit();
  }

  printThermal(): void {
    // Thermal print implementation - opens a compact receipt view
    this.printInvoice(); // For now, same as regular print
  }

  async downloadPdf(): Promise<void> {
    if (!this.invoiceData) return;
    
    this.downloading.set(true);
    try {
      await this.pdfService.downloadAsPdf(
        'invoice-print-area',
        `Invoice-${this.invoiceData.invoiceNumber}`
      );
      this.onDownload.emit();
    } finally {
      this.downloading.set(false);
    }
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }
}
