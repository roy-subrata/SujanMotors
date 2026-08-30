import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { TableModule } from 'primeng/table';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { CashBookService, DailyCashBook, LedgerRow, CashBookEntry } from '../services/cash-book.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { StatStripComponent, StatStripItem } from '@/shared/components/stat-strip/stat-strip.component';
import { I18nService } from '@/shared/services/i18n.service';

type Preset = 'today' | 'yesterday' | 'this_week' | 'this_month' | 'custom';
type ViewMode = 'ledger' | 'split';

const MAX_RANGE_DAYS = 366;
const CREDIT_METHODS = new Set(['DUE', 'PART_PAY']);

@Component({
  selector: 'app-cash-book',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ToastModule, TableModule, DatePickerModule, TooltipModule, SelectModule, PageContainerComponent, PageHeaderComponent, StatStripComponent],
  providers: [MessageService],
  templateUrl: './cash-book.component.html',
  styleUrls: ['./cash-book.component.css']
})
export class CashBookComponent implements OnInit {
  private readonly svc    = inject(CashBookService);
  private readonly toast  = inject(MessageService);
  private readonly fxSvc  = inject(CurrencyService);
  readonly i18n           = inject(I18nService);

  // ── State ────────────────────────────────────────────────────────
  loading   = signal(false);
  book      = signal<DailyCashBook | null>(null);
  viewMode  = signal<ViewMode>('ledger');
  preset    = signal<Preset>('today');

  // Date range
  dateFrom  = new Date();
  dateTo    = new Date();
  maxDate   = new Date();

  // Filters
  filterMethod = signal('');

  // ── Computed ─────────────────────────────────────────────────────
  ledgerRows = computed(() => {
    const b = this.book();
    if (!b) return [];
    const method = this.filterMethod();
    return method
      ? b.ledger.filter(r => r.paymentMethod === method)
      : b.ledger;
  });

  cashInRows = computed(() => {
    const b = this.book();
    if (!b) return [];
    const method = this.filterMethod();
    return method
      ? b.cashIn.filter(r => r.paymentMethod === method)
      : b.cashIn;
  });

  cashOutRows = computed(() => {
    const b = this.book();
    if (!b) return [];
    const method = this.filterMethod();
    return method
      ? b.cashOut.filter(r => r.paymentMethod === method)
      : b.cashOut;
  });

  // Totals come straight from the backend's per-method breakdown (or the day's
  // top-level totals when no method filter is applied) — never recomputed here.
  filteredTotals = computed(() => {
    const b = this.book();
    if (!b) return { in: 0, out: 0, net: 0 };
    const method = this.filterMethod();
    if (!method) return { in: b.totalCashIn, out: b.totalCashOut, net: b.netCash };
    const entry = b.paymentMethodBreakdown.find(x => x.method === method);
    return entry ? { in: entry.in, out: entry.out, net: entry.net } : { in: 0, out: 0, net: 0 };
  });

  filteredIn  = computed(() => this.filteredTotals().in);
  filteredOut = computed(() => this.filteredTotals().out);
  filteredNet = computed(() => this.filteredTotals().net);

  methodOptions = computed(() => {
    const b = this.book();
    if (!b) return [];
    const methods = [...new Set([...b.cashIn, ...b.cashOut].map(e => e.paymentMethod))].sort();
    return [{ label: this.i18n.t('cashBook.allMethods'), value: '' }, ...methods.map(m => ({ label: this.methodLabel(m), value: m }))];
  });

  headerStats = computed<StatStripItem[]>(() => {
    const b = this.book();
    if (!b) return [];
    return [
      { label: this.i18n.t('cashBook.in'), value: this.formatCurrency(b.totalCashIn) },
      { label: this.i18n.t('cashBook.out'), value: this.formatCurrency(b.totalCashOut) },
      { label: this.i18n.t('cashBook.net'), value: (b.netCash >= 0 ? '+' : '') + this.formatCurrency(b.netCash) },
    ];
  });

  // ── Lifecycle ────────────────────────────────────────────────────
  ngOnInit(): void { this.applyPreset('today'); }

  // ── Preset navigation ────────────────────────────────────────────
  applyPreset(p: Preset): void {
    this.preset.set(p);
    this.filterMethod.set('');
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    switch (p) {
      case 'today':
        this.dateFrom = this.dateTo = new Date(today);
        break;
      case 'yesterday': {
        const y = new Date(today); y.setDate(y.getDate() - 1);
        this.dateFrom = this.dateTo = y;
        break;
      }
      case 'this_week': {
        const mon = new Date(today);
        mon.setDate(today.getDate() - ((today.getDay() + 6) % 7));
        this.dateFrom = mon;
        this.dateTo   = new Date(today);
        break;
      }
      case 'this_month': {
        this.dateFrom = new Date(today.getFullYear(), today.getMonth(), 1);
        this.dateTo   = new Date(today);
        break;
      }
      case 'custom':
        return; // handled by date pickers
    }
    this.load();
  }

  prevDay(): void {
    const d = new Date(this.dateFrom); d.setDate(d.getDate() - 1);
    this.dateFrom = this.dateTo = d;
    this.preset.set('custom');
    this.load();
  }

  nextDay(): void {
    const d = new Date(this.dateFrom); d.setDate(d.getDate() + 1);
    if (d > this.maxDate) return;
    this.dateFrom = this.dateTo = d;
    this.preset.set('custom');
    this.load();
  }

  onDateChange(): void {
    const days = this.daysBetween(this.dateFrom, this.dateTo);
    if (days > MAX_RANGE_DAYS) {
      this.toast.add({
        severity: 'warn',
        summary: this.i18n.t('cashBook.rangeTooLargeSummary'),
        detail: this.i18n.t('cashBook.rangeTooLargeDetail', { days: String(MAX_RANGE_DAYS) })
      });
      return;
    }
    this.preset.set('custom');
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const obs = this.isSingleDay()
      ? this.svc.getDaily(this.dateFrom)
      : this.svc.getRange(this.dateFrom, this.dateTo);

    obs.subscribe({
      next: b => { this.book.set(b); this.loading.set(false); },
      error: (err) => {
        const msg = err?.error?.data?.detail ?? err?.error?.message ?? this.i18n.t('cashBook.loadFailed');
        this.toast.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
        this.loading.set(false);
      }
    });
  }

  // ── CSV Export ────────────────────────────────────────────────────
  exportCsv(): void {
    const b = this.book();
    if (!b) return;

    const rows = this.ledgerRows();
    const header = [
      this.i18n.t('cashBook.colTime'), this.i18n.t('cashBook.colType'), this.i18n.t('cashBook.colDescription'),
      this.i18n.t('common.labels.reference'), this.i18n.t('cashBook.colPaymentMethod'), this.i18n.t('cashBook.colCashIn'),
      this.i18n.t('cashBook.colCashOut'), this.i18n.t('cashBook.colBalance'), this.i18n.t('common.labels.status'), this.i18n.t('common.labels.notes')
    ].join(',');
    const lines  = rows.map(r => [
      `"${new Date(r.time).toLocaleString('en-GB')}"`,
      `"${this.typeLabel(r.type)}"`,
      `"${r.description.replace(/"/g, '""')}"`,
      `"${r.reference ?? ''}"`,
      `"${this.methodLabel(r.paymentMethod)}"`,
      r.cashIn  != null ? r.cashIn  : '',
      r.cashOut != null ? r.cashOut : '',
      r.balance,
      `"${r.status}"`,
      `"${(r.notes ?? '').replace(/"/g, '""')}"`
    ].join(','));

    const csv     = [header, ...lines].join('\n');
    const blob    = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url     = URL.createObjectURL(blob);
    const anchor  = document.createElement('a');
    anchor.href   = url;
    anchor.download = `cashbook-${b.from}${b.isSingleDay ? '' : '_to_' + b.to}.csv`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  // ── Helpers ──────────────────────────────────────────────────────
  isSingleDay(): boolean {
    return this.dateFrom.toDateString() === this.dateTo.toDateString();
  }

  isToday(): boolean {
    return this.isSingleDay() && this.dateFrom.toDateString() === new Date().toDateString();
  }

  isCreditMethod(method: string): boolean {
    return CREDIT_METHODS.has(method?.toUpperCase());
  }

  daysBetween(a: Date, b: Date): number {
    return Math.round(Math.abs(b.getTime() - a.getTime()) / 86_400_000);
  }

  formatCurrency(v: number | null | undefined): string {
    if (v == null) return '';
    return this.fxSvc.formatCurrency(v, this.fxSvc.selectedCurrency());
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short' });
  }

  typeIcon(type: string): string {
    const m: Record<string, string> = {
      CUSTOMER_PAYMENT: 'pi-arrow-circle-down',
      EXPENSE:          'pi-receipt',
      SUPPLIER_PAYMENT: 'pi-arrow-circle-up',
      REFUND:           'pi-replay'
    };
    return m[type] ?? 'pi-circle';
  }

  typeLabel(type: string): string {
    const m: Record<string, string> = {
      CUSTOMER_PAYMENT: 'cashBook.types.customerPayment',
      EXPENSE:          'cashBook.types.expense',
      SUPPLIER_PAYMENT: 'cashBook.types.supplierPayment',
      REFUND:           'cashBook.types.refund'
    };
    return m[type] ? this.i18n.t(m[type]) : type;
  }

  methodLabel(m: string): string {
    const map: Record<string, string> = {
      CASH: 'cashBook.methods.cash', MOBILE_BANKING: 'cashBook.methods.mobileBanking', CARD: 'cashBook.methods.card',
      BANK_TRANSFER: 'cashBook.methods.bankTransfer', CHEQUE: 'cashBook.methods.cheque', CHECK: 'cashBook.methods.cheque',
      DUE: 'cashBook.methods.due', PART_PAY: 'cashBook.methods.partPay', ADVANCE: 'cashBook.methods.advance',
      REFUND: 'cashBook.methods.refund', REFUND_REVERSAL: 'cashBook.methods.refundReversal'
    };
    return map[m] ? this.i18n.t(map[m]) : m;
  }

  methodIcon(m: string): string {
    const map: Record<string, string> = {
      CASH: 'pi-money-bill', MOBILE_BANKING: 'pi-mobile',
      CARD: 'pi-credit-card', BANK_TRANSFER: 'pi-building',
      CHEQUE: 'pi-file', CHECK: 'pi-file', DUE: 'pi-clock',
      PART_PAY: 'pi-clock', REFUND: 'pi-undo'
    };
    return map[m] ?? 'pi-wallet';
  }

  trackById(_: number, row: LedgerRow): string { return row.id; }

  headingDate(): string {
    if (this.isSingleDay()) {
      if (this.isToday()) return this.i18n.t('cashBook.today');
      const y = new Date(); y.setDate(y.getDate() - 1);
      if (this.dateFrom.toDateString() === y.toDateString()) return this.i18n.t('cashBook.yesterday');
      return this.dateFrom.toLocaleDateString('en-GB', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' });
    }
    return `${this.dateFrom.toLocaleDateString('en-GB', { day: '2-digit', month: 'short' })} – ${this.dateTo.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}`;
  }
}
