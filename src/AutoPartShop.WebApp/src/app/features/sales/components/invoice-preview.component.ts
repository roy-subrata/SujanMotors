import { Component, Input, Output, EventEmitter, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { DividerModule } from 'primeng/divider';
import { MessageService } from 'primeng/api';
import { InvoicePdfService, InvoicePdfData } from '../services/invoice-pdf.service';
import { ThermalReceiptService } from '../services/thermal-receipt.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
  selector: 'app-invoice-preview',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ButtonModule,
    TooltipModule,
    DividerModule,
    TranslatePipe
  ],
  templateUrl: './invoice-preview.component.html',
  styleUrl: './invoice-preview.component.scss',
})
export class InvoicePreviewComponent implements OnInit {
  private readonly pdfService = inject(InvoicePdfService);
  private readonly thermalReceipt = inject(ThermalReceiptService);
  private readonly messageService = inject(MessageService);
  private readonly i18n = inject(I18nService);

  @Input() visible = false;
  @Input() invoiceData: InvoicePdfData | null = null;
  /** Required for "Download PDF" — it fetches the server-rendered QuestPDF invoice. */
  @Input() invoiceId: string | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() onPrint = new EventEmitter<void>();
  @Output() onDownload = new EventEmitter<void>();

  downloading = signal(false);
  amountInWords = '';
  currentDateTime = '';
  paymentStatusClass = 'paid';

  /** Getter, not a field: a field set in a lifecycle hook would keep the label
   *  in whichever language was active when the dialog was opened. */
  get paymentStatusLabel(): string {
    const due = this.invoiceData?.dueAmount || 0;
    const paid = this.invoiceData?.paidAmount || 0;
    if (due > 0.001) {
      return this.i18n.t(paid > 0.001 ? 'invoicePreview.statusPartiallyPaid' : 'invoicePreview.statusUnpaid');
    }
    return this.i18n.t('invoicePreview.statusPaid');
  }

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

      const due = this.invoiceData.dueAmount || 0;
      const paid = this.invoiceData.paidAmount || 0;
      if (due > 0.001) {
        this.paymentStatusClass = paid > 0.001 ? 'partial' : 'due';
      } else {
        this.paymentStatusClass = 'paid';
      }
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
    if (!this.invoiceData) return;

    if (!this.invoiceId) {
      // Should not happen on the one path that opens this dialog (quick-sale-shortcut always
      // sets currentInvoiceId before showing the preview), but fail loudly rather than silently
      // do nothing if that ever changes.
      this.messageService.add({
        severity: 'error',
        summary: this.i18n.t('invoicePreview.downloadFailed'),
        detail: this.i18n.t('invoicePreview.notSavedYet')
      });
      return;
    }

    this.pdfService.downloadServerPdf(this.invoiceId, this.invoiceData.invoiceNumber).subscribe({
      next: () => this.onPrint.emit(),
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('invoicePreview.downloadFailed'),
          detail: err?.error?.message || this.i18n.t('invoicePreview.notSavedYet')
        });
      }
    });
  }

  printThermal(): void {
    if (!this.invoiceData) return;
    this.thermalReceipt.print(this.invoiceData, (n) => this.pdfService.formatCurrency(n));
    this.onPrint.emit();
  }

  async downloadPdf(): Promise<void> {
    if (!this.invoiceData) return;

    if (!this.invoiceId) {
      // Should not happen on the one path that opens this dialog (quick-sale-shortcut always
      // sets currentInvoiceId before showing the preview), but fail loudly rather than silently
      // do nothing if that ever changes.
      this.messageService.add({
        severity: 'error',
        summary: this.i18n.t('invoicePreview.downloadFailed'),
        detail: this.i18n.t('invoicePreview.notSavedYet')
      });
      return;
    }

    this.downloading.set(true);
    try {
      await new Promise<void>((resolve, reject) =>
        this.pdfService.downloadServerPdf(this.invoiceId!, this.invoiceData!.invoiceNumber)
          .subscribe({ next: () => resolve(), error: reject })
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
