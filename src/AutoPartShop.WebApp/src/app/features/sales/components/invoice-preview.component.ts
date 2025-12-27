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
          <!-- Invoice Header -->
          <div class="invoice-header">
            <div class="company-section">
              <div class="company-logo">
                <i class="pi pi-car"></i>
              </div>
              <div class="company-info">
                <h1 class="company-name">{{ invoiceData.companyName }}</h1>
                <p>{{ invoiceData.companyAddress }}</p>
                <p><i class="pi pi-phone"></i> {{ invoiceData.companyPhone }}</p>
                <p><i class="pi pi-envelope"></i> {{ invoiceData.companyEmail }}</p>
                <p class="tax-id" *ngIf="invoiceData.companyTaxId">{{ invoiceData.companyTaxId }}</p>
              </div>
            </div>
            <div class="invoice-title-section">
              <h2 class="invoice-title">TAX INVOICE</h2>
              <div class="invoice-meta">
                <div class="meta-row">
                  <span class="meta-label">Invoice No:</span>
                  <span class="meta-value invoice-number">{{ invoiceData.invoiceNumber }}</span>
                </div>
                <div class="meta-row">
                  <span class="meta-label">Date:</span>
                  <span class="meta-value">{{ formatDate(invoiceData.invoiceDate) }}</span>
                </div>
                <div class="meta-row" *ngIf="invoiceData.dueDate">
                  <span class="meta-label">Due Date:</span>
                  <span class="meta-value">{{ formatDate(invoiceData.dueDate) }}</span>
                </div>
                <div class="meta-row" *ngIf="invoiceData.salesOrderNumber">
                  <span class="meta-label">SO #:</span>
                  <span class="meta-value">{{ invoiceData.salesOrderNumber }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Customer & Technician Info -->
          <div class="parties-section">
            <div class="party-box customer-box">
              <div class="party-header">
                <i class="pi pi-user"></i>
                <span>Bill To</span>
              </div>
              <div class="party-content">
                <h4>{{ invoiceData.customerName }}</h4>
                <p *ngIf="invoiceData.customerAddress">{{ invoiceData.customerAddress }}</p>
                <p *ngIf="invoiceData.customerPhone"><i class="pi pi-phone"></i> {{ invoiceData.customerPhone }}</p>
                <p *ngIf="invoiceData.customerEmail"><i class="pi pi-envelope"></i> {{ invoiceData.customerEmail }}</p>
              </div>
            </div>
            <div class="party-box technician-box" *ngIf="invoiceData.technicianName">
              <div class="party-header">
                <i class="pi pi-wrench"></i>
                <span>Technician</span>
              </div>
              <div class="party-content">
                <h4>{{ invoiceData.technicianName }}</h4>
                <p *ngIf="invoiceData.technicianPhone"><i class="pi pi-phone"></i> {{ invoiceData.technicianPhone }}</p>
              </div>
            </div>
          </div>

          <!-- Items Table -->
          <div class="items-section">
            <table class="items-table">
              <thead>
                <tr>
                  <th class="col-sn">S.N.</th>
                  <th class="col-code">Part Code</th>
                  <th class="col-desc">Description</th>
                  <th class="col-qty">Qty</th>
                  <th class="col-rate">Rate</th>
                  <th class="col-disc">Disc.</th>
                  <th class="col-amount">Amount</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of invoiceData.items; let i = index" [class.alt-row]="i % 2 === 1">
                  <td class="col-sn">{{ item.slNo }}</td>
                  <td class="col-code">{{ item.partNumber }}</td>
                  <td class="col-desc">{{ item.description }}</td>
                  <td class="col-qty">{{ item.quantity }}</td>
                  <td class="col-rate">{{ formatCurrency(item.unitPrice) }}</td>
                  <td class="col-disc">{{ formatCurrency(item.discount) }}</td>
                  <td class="col-amount">{{ formatCurrency(item.total) }}</td>
                </tr>
                <!-- Empty rows for consistent look -->
                <tr *ngFor="let empty of getEmptyRows()" class="empty-row">
                  <td>&nbsp;</td>
                  <td></td>
                  <td></td>
                  <td></td>
                  <td></td>
                  <td></td>
                  <td></td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Totals & Payment Section -->
          <div class="totals-payment-section">
            <div class="amount-words">
              <span class="words-label">Amount in Words:</span>
              <span class="words-value">{{ amountInWords }}</span>
            </div>
            <div class="totals-box">
              <div class="total-row">
                <span>Subtotal:</span>
                <span>{{ formatCurrency(invoiceData.subtotal) }}</span>
              </div>
              <div class="total-row" *ngIf="invoiceData.discountAmount > 0">
                <span>Discount:</span>
                <span class="discount">-{{ formatCurrency(invoiceData.discountAmount) }}</span>
              </div>
              <div class="total-row">
                <span>VAT ({{ invoiceData.vatPercentage }}%):</span>
                <span>{{ formatCurrency(invoiceData.vatAmount) }}</span>
              </div>
              <div class="total-row grand-total">
                <span>Grand Total:</span>
                <span>{{ formatCurrency(invoiceData.grandTotal) }}</span>
              </div>
              <div class="divider"></div>
              <div class="total-row paid">
                <span>Paid Amount:</span>
                <span>{{ formatCurrency(invoiceData.paidAmount) }}</span>
              </div>
              <div class="total-row due" *ngIf="invoiceData.dueAmount > 0">
                <span>Balance Due:</span>
                <span class="due-amount">{{ formatCurrency(invoiceData.dueAmount) }}</span>
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

          <!-- Notes Section -->
          <div class="notes-section" *ngIf="invoiceData.notes">
            <h4><i class="pi pi-info-circle"></i> Notes</h4>
            <p>{{ invoiceData.notes }}</p>
          </div>

          <!-- Footer -->
          <div class="invoice-footer">
            <div class="footer-left">
              <div class="terms">
                <h5>Terms & Conditions</h5>
                <p>1. Goods once sold will not be taken back or exchanged.</p>
                <p>2. All disputes subject to local jurisdiction only.</p>
                <p>3. E.&O.E. (Errors and Omissions Excepted)</p>
              </div>
            </div>
            <div class="footer-right">
              <div class="signature-box">
                <div class="signature-line"></div>
                <span>Authorized Signature</span>
              </div>
            </div>
          </div>

          <!-- Print Footer -->
          <div class="print-footer">
            <p>Thank you for your business!</p>
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
      padding: 2rem;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
      border-radius: 4px;
    }

    /* Invoice Header */
    .invoice-header {
      display: flex;
      justify-content: space-between;
      padding-bottom: 1.5rem;
      border-bottom: 2px solid #1e3a5f;
      margin-bottom: 1.5rem;
    }

    .company-section {
      display: flex;
      gap: 1rem;
    }

    .company-logo {
      width: 64px;
      height: 64px;
      background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%);
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;

      i {
        font-size: 2rem;
        color: white;
      }
    }

    .company-info {
      .company-name {
        font-size: 1.5rem;
        font-weight: 700;
        color: #1e3a5f;
        margin-bottom: 0.25rem;
      }

      p {
        font-size: 0.85rem;
        color: #64748b;
        margin: 0.15rem 0;
        display: flex;
        align-items: center;
        gap: 0.5rem;

        i { font-size: 0.8rem; color: #94a3b8; }
      }

      .tax-id {
        font-weight: 600;
        color: #475569;
        margin-top: 0.5rem;
      }
    }

    .invoice-title-section {
      text-align: right;
    }

    .invoice-title {
      font-size: 1.75rem;
      font-weight: 800;
      color: #1e3a5f;
      letter-spacing: 2px;
      margin-bottom: 1rem;
    }

    .invoice-meta {
      .meta-row {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        margin-bottom: 0.35rem;

        .meta-label {
          font-size: 0.85rem;
          color: #64748b;
        }

        .meta-value {
          font-size: 0.85rem;
          font-weight: 600;
          color: #1e293b;
          min-width: 100px;
          text-align: left;

          &.invoice-number {
            color: #1e3a5f;
            font-size: 1rem;
          }
        }
      }
    }

    /* Parties Section */
    .parties-section {
      display: flex;
      gap: 1.5rem;
      margin-bottom: 1.5rem;
    }

    .party-box {
      flex: 1;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      overflow: hidden;

      .party-header {
        background: #f8fafc;
        padding: 0.5rem 1rem;
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-weight: 600;
        color: #475569;
        border-bottom: 1px solid #e2e8f0;

        i { color: #1e3a5f; }
      }

      .party-content {
        padding: 1rem;

        h4 {
          font-size: 1rem;
          font-weight: 600;
          color: #1e293b;
          margin-bottom: 0.5rem;
        }

        p {
          font-size: 0.85rem;
          color: #64748b;
          margin: 0.25rem 0;
          display: flex;
          align-items: center;
          gap: 0.5rem;

          i { font-size: 0.75rem; color: #94a3b8; }
        }
      }
    }

    /* Items Table */
    .items-section {
      margin-bottom: 1.5rem;
    }

    .items-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.85rem;

      th {
        background: #1e3a5f;
        color: white;
        padding: 0.75rem 0.5rem;
        text-align: left;
        font-weight: 600;
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.5px;

        &:first-child { border-radius: 4px 0 0 0; }
        &:last-child { border-radius: 0 4px 0 0; }
      }

      td {
        padding: 0.65rem 0.5rem;
        border-bottom: 1px solid #e2e8f0;
        color: #475569;
      }

      .alt-row td {
        background: #f8fafc;
      }

      .empty-row td {
        height: 32px;
        background: #fafafa;
      }

      .col-sn { width: 40px; text-align: center; }
      .col-code { width: 100px; }
      .col-desc { }
      .col-qty { width: 60px; text-align: center; }
      .col-rate, .col-disc, .col-amount { width: 100px; text-align: right; }
    }

    /* Totals Section */
    .totals-payment-section {
      display: flex;
      gap: 1.5rem;
      margin-bottom: 1.5rem;
    }

    .amount-words {
      flex: 1;
      padding: 1rem;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;

      .words-label {
        font-size: 0.75rem;
        color: #64748b;
        display: block;
        margin-bottom: 0.5rem;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }

      .words-value {
        font-size: 0.9rem;
        font-weight: 600;
        color: #1e293b;
        font-style: italic;
      }
    }

    .totals-box {
      width: 280px;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 1rem;
      background: #fafafa;

      .total-row {
        display: flex;
        justify-content: space-between;
        padding: 0.4rem 0;
        font-size: 0.9rem;

        span:first-child { color: #64748b; }
        span:last-child { font-weight: 600; color: #1e293b; }

        &.grand-total {
          padding: 0.75rem 0;
          margin-top: 0.5rem;
          border-top: 2px solid #1e3a5f;

          span:first-child { font-weight: 700; color: #1e3a5f; }
          span:last-child { 
            font-size: 1.1rem; 
            font-weight: 700; 
            color: #1e3a5f; 
          }
        }

        &.due {
          padding-top: 0.5rem;
          border-top: 1px dashed #e2e8f0;

          .due-amount {
            color: #dc2626;
            font-weight: 700;
          }
        }

        .discount { color: #16a34a; }
      }

      .divider {
        height: 1px;
        background: #e2e8f0;
        margin: 0.5rem 0;
      }
    }

    /* Payment Details */
    .payment-details {
      margin-bottom: 1.5rem;
      padding: 1rem;
      background: #f0fdf4;
      border: 1px solid #bbf7d0;
      border-radius: 8px;

      h4 {
        font-size: 0.9rem;
        font-weight: 600;
        color: #166534;
        margin-bottom: 0.75rem;
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      .payment-list {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
      }

      .payment-item {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 0.75rem;
        background: white;
        border-radius: 6px;
        font-size: 0.85rem;

        .payment-method {
          color: #475569;
          font-weight: 500;
        }

        .payment-amount {
          font-weight: 700;
          color: #166534;
        }

        .payment-ref {
          color: #94a3b8;
          font-size: 0.8rem;
        }
      }
    }

    /* Notes Section */
    .notes-section {
      margin-bottom: 1.5rem;
      padding: 1rem;
      background: #fef3c7;
      border: 1px solid #fcd34d;
      border-radius: 8px;

      h4 {
        font-size: 0.9rem;
        font-weight: 600;
        color: #92400e;
        margin-bottom: 0.5rem;
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      p {
        font-size: 0.85rem;
        color: #78350f;
      }
    }

    /* Footer */
    .invoice-footer {
      display: flex;
      justify-content: space-between;
      padding-top: 1.5rem;
      border-top: 1px solid #e2e8f0;
      margin-top: 1rem;
    }

    .terms {
      h5 {
        font-size: 0.8rem;
        font-weight: 600;
        color: #475569;
        margin-bottom: 0.5rem;
      }

      p {
        font-size: 0.75rem;
        color: #94a3b8;
        margin: 0.15rem 0;
      }
    }

    .signature-box {
      text-align: center;
      padding-top: 2rem;

      .signature-line {
        width: 180px;
        height: 1px;
        background: #1e293b;
        margin-bottom: 0.5rem;
      }

      span {
        font-size: 0.8rem;
        color: #64748b;
      }
    }

    .print-footer {
      text-align: center;
      padding-top: 1.5rem;
      margin-top: 1rem;
      border-top: 1px dashed #e2e8f0;

      p {
        font-size: 0.85rem;
        color: #64748b;

        &:first-child {
          font-weight: 600;
          color: #1e3a5f;
        }
      }

      .generated-at {
        font-size: 0.75rem;
        margin-top: 0.5rem;
      }
    }

    /* Print Styles */
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
