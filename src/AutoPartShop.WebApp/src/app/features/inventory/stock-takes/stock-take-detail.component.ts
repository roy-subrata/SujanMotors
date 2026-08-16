import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SelectButtonModule } from 'primeng/selectbutton';
import { MessageService, ConfirmationService } from 'primeng/api';

import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import {
  StockTakeService,
  StockTakeDetailResponse,
  StockTakeLineResponse,
  StockTakeCountEntry
} from '../services/stock-take.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { extractApiError } from '../../../shared/utils/api-error.util';
import { StatusDisplayService } from '@/shared/services/status-display.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

type LineFilter = 'ALL' | 'UNCOUNTED' | 'VARIANCE';

@Component({
  selector: 'app-stock-take-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    TableModule,
    ToastModule,
    TooltipModule,
    ConfirmDialogModule,
    SelectButtonModule,
    PageContainerComponent,
    PageHeaderComponent,
    TranslatePipe
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './stock-take-detail.component.html',
  styleUrls: ['./stock-take-detail.component.css']
})
export class StockTakeDetailComponent implements OnInit {
  private readonly stockTakeService = inject(StockTakeService);
  private readonly currencyService = inject(CurrencyService);
  private readonly statusDisplay = inject(StatusDisplayService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  stockTake: StockTakeDetailResponse | null = null;
  loading = false;
  saving = false;
  approving = false;

  /** Draft counted quantities keyed by line id — saved in batch via Save Counts. */
  draftCounts = new Map<string, number | null>();
  searchTerm = '';
  lineFilter: LineFilter = 'ALL';
  lineFilterOptions: { label: string; value: LineFilter }[] = [];

  /** Conflict lines returned by a failed approval (stock moved since counting). */
  approvalConflicts: string[] = [];

  ngOnInit(): void {
    this.buildLineFilterOptions();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.buildLineFilterOptions();
    });
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.load(id);
  }

  private buildLineFilterOptions(): void {
    this.lineFilterOptions = [
      { label: this.i18n.t('common.status.all'), value: 'ALL' },
      { label: this.i18n.t('stockTakes.uncounted'), value: 'UNCOUNTED' },
      { label: this.i18n.t('stockTakes.variances'), value: 'VARIANCE' }
    ];
  }

  load(id: string): void {
    this.loading = true;
    this.stockTakeService.getById(id).subscribe({
      next: (st) => {
        this.stockTake = st;
        this.draftCounts.clear();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('stockTakes.messages.loadOneFailed') });
        this.router.navigate(['/inventory/stock-takes']);
      }
    });
  }

  reload(): void {
    if (this.stockTake) this.load(this.stockTake.id);
  }

  // ── Derived view ────────────────────────────────────────────────────────────

  get filteredLines(): StockTakeLineResponse[] {
    if (!this.stockTake) return [];
    const term = this.searchTerm.trim().toLowerCase();
    return this.stockTake.lines.filter(l => {
      if (term && !(`${l.partName} ${l.partCode} ${l.variantName} ${l.location}`.toLowerCase().includes(term)))
        return false;
      switch (this.lineFilter) {
        case 'UNCOUNTED': return this.effectiveCount(l) === null;
        case 'VARIANCE': {
          const c = this.effectiveCount(l);
          return c !== null && c !== l.expectedQuantity;
        }
        default: return true;
      }
    });
  }

  /** Draft value if the user typed one this session, otherwise the saved count. */
  effectiveCount(line: StockTakeLineResponse): number | null {
    return this.draftCounts.has(line.id) ? this.draftCounts.get(line.id)! : line.countedQuantity;
  }

  effectiveVariance(line: StockTakeLineResponse): number | null {
    const c = this.effectiveCount(line);
    return c === null ? null : c - line.expectedQuantity;
  }

  onCountInput(line: StockTakeLineResponse, raw: string): void {
    if (raw === '' || raw === null) {
      this.draftCounts.set(line.id, null);
      return;
    }
    const value = Math.floor(Number(raw));
    if (!Number.isFinite(value) || value < 0) return;
    this.draftCounts.set(line.id, value);
  }

  get dirtyCount(): number {
    let dirty = 0;
    this.draftCounts.forEach((value, lineId) => {
      const line = this.stockTake?.lines.find(l => l.id === lineId);
      if (line && value !== line.countedQuantity) dirty++;
    });
    return dirty;
  }

  get isCounting(): boolean { return this.stockTake?.status === 'COUNTING'; }
  get isReview(): boolean { return this.stockTake?.status === 'REVIEW'; }
  get isOpen(): boolean { return this.isCounting || this.isReview; }

  get countedTotal(): number {
    if (!this.stockTake) return 0;
    return this.stockTake.lines.filter(l => this.effectiveCount(l) !== null).length;
  }

  get varianceTotal(): number {
    if (!this.stockTake) return 0;
    return this.stockTake.lines.filter(l => {
      const v = this.effectiveVariance(l);
      return v !== null && v !== 0;
    }).length;
  }

  get varianceValueTotal(): number {
    if (!this.stockTake) return 0;
    return this.stockTake.lines.reduce((sum, l) => {
      const v = this.effectiveVariance(l);
      return v === null ? sum : sum + v * l.unitCost;
    }, 0);
  }

  pillStatus(status: string): string {
    return this.statusDisplay.getPillAttr(status, 'stock-take');
  }

  formatCurrency(amount: number): string {
    return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
  }

  // ── Actions ─────────────────────────────────────────────────────────────────

  saveCounts(onSaved?: () => void): void {
    if (!this.stockTake || this.saving) return;
    const entries: StockTakeCountEntry[] = [];
    this.draftCounts.forEach((value, lineId) => {
      const line = this.stockTake!.lines.find(l => l.id === lineId);
      if (line && value !== line.countedQuantity)
        entries.push({ lineId, countedQuantity: value });
    });
    if (entries.length === 0) {
      onSaved?.();
      return;
    }

    this.saving = true;
    this.stockTakeService.recordCounts(this.stockTake.id, entries).subscribe({
      next: () => {
        this.saving = false;
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('stockTakes.messages.countsSavedTitle'),
          detail: this.i18n.t('stockTakes.messages.countsSavedDetail', { count: String(entries.length) })
        });
        if (onSaved) onSaved(); else this.reload();
      },
      error: (err) => {
        this.saving = false;
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('stockTakes.messages.saveFailedTitle'),
          detail: extractApiError(err, this.i18n.t('stockTakes.messages.saveCountsFailed'))
        });
      }
    });
  }

  submitForReview(): void {
    if (!this.stockTake) return;
    const uncounted = this.stockTake.lines.filter(l => this.effectiveCount(l) === null).length;
    const message = uncounted > 0
      ? this.i18n.t('stockTakes.messages.submitConfirmUncounted', { count: String(uncounted) })
      : this.i18n.t('stockTakes.messages.submitConfirmClean');

    this.confirmationService.confirm({
      message,
      header: this.i18n.t('common.actions.submitForReview'),
      icon: 'pi pi-question-circle',
      accept: () => this.saveCounts(() => {
        this.stockTakeService.submit(this.stockTake!.id).subscribe({
          next: () => this.reload(),
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('stockTakes.messages.submitFailedTitle'),
              detail: extractApiError(err, this.i18n.t('stockTakes.messages.submitFailed'))
            });
            this.reload();
          }
        });
      })
    });
  }

  reopenCounting(): void {
    if (!this.stockTake) return;
    this.stockTakeService.reopen(this.stockTake.id).subscribe({
      next: () => {
        this.approvalConflicts = [];
        this.reload();
      },
      error: (err) => this.messageService.add({
        severity: 'error',
        summary: this.i18n.t('stockTakes.messages.reopenFailedTitle'),
        detail: extractApiError(err, this.i18n.t('stockTakes.messages.reopenFailed'))
      })
    });
  }

  approve(): void {
    if (!this.stockTake || this.approving) return;
    const variances = this.varianceTotal;
    this.confirmationService.confirm({
      message: variances > 0
        ? this.i18n.t('stockTakes.messages.approveConfirmVariances', { count: String(variances), value: this.formatCurrency(this.varianceValueTotal) })
        : this.i18n.t('stockTakes.messages.approveConfirmClean'),
      header: this.i18n.t('stockTakes.approveStockTake'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.approving = true;
        this.approvalConflicts = [];
        this.stockTakeService.approve(this.stockTake!.id).subscribe({
          next: (result) => {
            this.approving = false;
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('stockTakes.messages.completedTitle'),
              detail: this.i18n.t('stockTakes.messages.completedDetail', { applied: String(result.adjustmentsApplied), skipped: String(result.linesSkippedUncounted) })
            });
            if (result.lotSyncWarnings.length > 0) {
              this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('stockTakes.messages.lotSyncWarningsTitle'),
                detail: this.i18n.t('stockTakes.messages.lotSyncWarningsDetail', { count: String(result.lotSyncWarnings.length) }),
                life: 8000
              });
            }
            this.reload();
          },
          error: (err) => {
            this.approving = false;
            this.approvalConflicts = err?.error?.conflicts ?? [];
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('stockTakes.messages.approvalFailedTitle'),
              detail: extractApiError(err, this.i18n.t('stockTakes.messages.approvalFailed')),
              life: 8000
            });
          }
        });
      }
    });
  }

  cancelStockTake(): void {
    if (!this.stockTake) return;
    this.confirmationService.confirm({
      message: this.i18n.t('stockTakes.messages.cancelConfirm', { number: this.stockTake.stockTakeNumber }),
      header: this.i18n.t('stockTakes.cancelStockTake'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.stockTakeService.cancel(this.stockTake!.id).subscribe({
          next: () => this.reload(),
          error: (err) => this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('stockTakes.messages.cancelFailedTitle'),
            detail: extractApiError(err, this.i18n.t('stockTakes.messages.cancelFailed'))
          })
        });
      }
    });
  }
}
