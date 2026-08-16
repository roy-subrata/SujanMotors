import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { InvoiceService, InvoicePrintData } from '../../services/invoice.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
  selector: 'app-invoice-print',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './invoice-print.component.html',
  styleUrls: ['./invoice-print.component.css']
})
export class InvoicePrintComponent implements OnInit {
  private readonly route   = inject(ActivatedRoute);
  private readonly svc     = inject(InvoiceService);
  private readonly fxSvc   = inject(CurrencyService);
  private readonly i18n    = inject(I18nService);

  data    = signal<InvoicePrintData | null>(null);
  loading = signal(true);
  error   = signal<string | null>(null);

  /** When true, renders a Delivery Challan instead of a Tax Invoice */
  get isChallan(): boolean {
    return this.route.snapshot.queryParamMap.get('type') === 'challan';
  }

  get title(): string {
    return this.i18n.t(this.isChallan ? 'invoicePrint.deliveryChallan' : 'invoicePrint.taxInvoice');
  }

  /** Server enum -> localized label, falling back to the raw value when untranslated. */
  statusLabel(status: string | undefined): string {
    if (!status) return '';
    const key = 'invoices.statusOptions.' + status.toLowerCase()
      .replace(/_(.)/g, (_m, c: string) => c.toUpperCase());
    const label = this.i18n.t(key);
    return label === key ? status : label;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getPrintData(id).subscribe({
      next: d => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set(this.i18n.t('invoicePrint.notFound')); this.loading.set(false); }
    });
  }

  print(): void { window.print(); }

  formatCurrency(v: number): string {
    return this.fxSvc.formatCurrency(v, this.fxSvc.selectedCurrency());
  }

  formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatDateTime(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  get isPaid(): boolean {
    const inv = this.data()?.invoice;
    return !!inv && inv.outstandingAmount <= 0;
  }

  get isPartiallyPaid(): boolean {
    const inv = this.data()?.invoice;
    return !!inv && inv.amountPaid > 0 && inv.outstandingAmount > 0;
  }
}
