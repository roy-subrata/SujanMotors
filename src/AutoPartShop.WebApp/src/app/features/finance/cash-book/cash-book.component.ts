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

type Preset = 'today' | 'yesterday' | 'this_week' | 'this_month' | 'custom';
type ViewMode = 'ledger' | 'split';

const MAX_RANGE_DAYS = 366;
const CREDIT_METHODS = new Set(['DUE', 'PART_PAY']);

@Component({
  selector: 'app-cash-book',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ToastModule, TableModule, DatePickerModule, TooltipModule, SelectModule, PageContainerComponent, PageHeaderComponent],
  providers: [MessageService],
  templateUrl: './cash-book.component.html',
  styleUrls: ['./cash-book.component.css']
})
export class CashBookComponent implements OnInit {
  private readonly svc    = inject(CashBookService);
  private readonly toast  = inject(MessageService);
  private readonly fxSvc  = inject(CurrencyService);

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
  filterMethod = '';

  // ── Computed ─────────────────────────────────────────────────────
  ledgerRows = computed(() => {
    const b = this.book();
    if (!b) return [];
    return this.filterMethod
      ? b.ledger.filter(r => r.paymentMethod === this.filterMethod)
      : b.ledger;
  });

  cashInRows = computed(() => {
    const b = this.book();
    if (!b) return [];
    return this.filterMethod
      ? b.cashIn.filter(r => r.paymentMethod === this.filterMethod)
      : b.cashIn;
  });

  cashOutRows = computed(() => {
    const b = this.book();
    if (!b) return [];
    return this.filterMethod
      ? b.cashOut.filter(r => r.paymentMethod === this.filterMethod)
      : b.cashOut;
  });

  filteredIn        = computed(() => this.cashInRows().reduce((s, r) => s + r.amount, 0));
  filteredOut       = computed(() => this.cashOutRows().reduce((s, r) => s + r.amount, 0));
  filteredNet       = computed(() => this.filteredIn() - this.filteredOut());
  filteredCreditIn  = computed(() => this.cashInRows().filter(r => r.isCreditSale).reduce((s, r) => s + r.amount, 0));
  filteredActualIn  = computed(() => this.filteredIn() - this.filteredCreditIn());
  filteredActualNet = computed(() => this.filteredActualIn() - this.filteredOut());

  hasCreditSales = computed(() => this.cashInRows().some(r => r.isCreditSale));

  methodOptions = computed(() => {
    const b = this.book();
    if (!b) return [];
    const methods = [...new Set([...b.cashIn, ...b.cashOut].map(e => e.paymentMethod))].sort();
    return [{ label: 'All Methods', value: '' }, ...methods.map(m => ({ label: this.methodLabel(m), value: m }))];
  });

  // ── Lifecycle ────────────────────────────────────────────────────
  ngOnInit(): void { this.applyPreset('today'); }

  // ── Preset navigation ────────────────────────────────────────────
  applyPreset(p: Preset): void {
    this.preset.set(p);
    this.filterMethod = '';
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
        summary: 'Range Too Large',
        detail: `Maximum range is ${MAX_RANGE_DAYS} days. Please narrow your selection.`
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
        const msg = err?.error?.data?.detail ?? err?.error?.message ?? 'Failed to load cash book';
        this.toast.add({ severity: 'error', summary: 'Error', detail: msg });
        this.loading.set(false);
      }
    });
  }

  // ── CSV Export ────────────────────────────────────────────────────
  exportCsv(): void {
    const b = this.book();
    if (!b) return;

    const rows = this.ledgerRows();
    const header = ['Time', 'Type', 'Description', 'Reference', 'Payment Method', 'Cash In', 'Cash Out', 'Balance', 'Status', 'Notes'].join(',');
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
      CUSTOMER_PAYMENT: 'Sale / Receipt',
      EXPENSE:          'Expense',
      SUPPLIER_PAYMENT: 'Supplier Pmt',
      REFUND:           'Refund'
    };
    return m[type] ?? type;
  }

  methodLabel(m: string): string {
    const map: Record<string, string> = {
      CASH: 'Cash', MOBILE_BANKING: 'Mobile Banking', CARD: 'Card',
      BANK_TRANSFER: 'Bank Transfer', CHEQUE: 'Cheque', CHECK: 'Cheque',
      DUE: 'Due / Credit', PART_PAY: 'Part Pay', ADVANCE: 'Advance',
      REFUND: 'Refund', REFUND_REVERSAL: 'Refund Reversal'
    };
    return map[m] ?? m;
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
      if (this.isToday()) return 'Today';
      const y = new Date(); y.setDate(y.getDate() - 1);
      if (this.dateFrom.toDateString() === y.toDateString()) return 'Yesterday';
      return this.dateFrom.toLocaleDateString('en-GB', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' });
    }
    return `${this.dateFrom.toLocaleDateString('en-GB', { day: '2-digit', month: 'short' })} – ${this.dateTo.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}`;
  }
}
