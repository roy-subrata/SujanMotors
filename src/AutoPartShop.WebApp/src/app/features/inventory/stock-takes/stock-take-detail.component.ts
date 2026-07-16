import { Component, OnInit, inject } from '@angular/core';
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
    PageHeaderComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './stock-take-detail.component.html',
  styleUrls: ['./stock-take-detail.component.css']
})
export class StockTakeDetailComponent implements OnInit {
  private readonly stockTakeService = inject(StockTakeService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  stockTake: StockTakeDetailResponse | null = null;
  loading = false;
  saving = false;
  approving = false;

  /** Draft counted quantities keyed by line id — saved in batch via Save Counts. */
  draftCounts = new Map<string, number | null>();
  searchTerm = '';
  lineFilter: LineFilter = 'ALL';
  lineFilterOptions = [
    { label: 'All', value: 'ALL' as LineFilter },
    { label: 'Uncounted', value: 'UNCOUNTED' as LineFilter },
    { label: 'Variances', value: 'VARIANCE' as LineFilter }
  ];

  /** Conflict lines returned by a failed approval (stock moved since counting). */
  approvalConflicts: string[] = [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.load(id);
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
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load stock take' });
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
    switch (status) {
      case 'COUNTING': return 'pending';
      case 'REVIEW': return 'info';
      case 'COMPLETED': return 'completed';
      case 'CANCELLED': return 'cancelled';
      default: return 'draft';
    }
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
        this.messageService.add({ severity: 'success', summary: 'Counts Saved', detail: `${entries.length} line(s) updated` });
        if (onSaved) onSaved(); else this.reload();
      },
      error: (err) => {
        this.saving = false;
        this.messageService.add({ severity: 'error', summary: 'Save Failed', detail: extractApiError(err, 'Could not save counts') });
      }
    });
  }

  submitForReview(): void {
    if (!this.stockTake) return;
    const uncounted = this.stockTake.lines.filter(l => this.effectiveCount(l) === null).length;
    const message = uncounted > 0
      ? `${uncounted} line(s) are still uncounted and will be SKIPPED when adjustments are applied. Submit for review anyway?`
      : 'Lock counting and move to variance review?';

    this.confirmationService.confirm({
      message,
      header: 'Submit for Review',
      icon: 'pi pi-question-circle',
      accept: () => this.saveCounts(() => {
        this.stockTakeService.submit(this.stockTake!.id).subscribe({
          next: () => this.reload(),
          error: (err) => {
            this.messageService.add({ severity: 'error', summary: 'Submit Failed', detail: extractApiError(err, 'Could not submit') });
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
      error: (err) => this.messageService.add({ severity: 'error', summary: 'Reopen Failed', detail: extractApiError(err, 'Could not reopen') })
    });
  }

  approve(): void {
    if (!this.stockTake || this.approving) return;
    const variances = this.varianceTotal;
    this.confirmationService.confirm({
      message: variances > 0
        ? `Apply ${variances} variance adjustment(s) (${this.formatCurrency(this.varianceValueTotal)}) to stock now? This cannot be undone.`
        : 'No variances found — complete this stock take without any stock changes?',
      header: 'Approve Stock Take',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.approving = true;
        this.approvalConflicts = [];
        this.stockTakeService.approve(this.stockTake!.id).subscribe({
          next: (result) => {
            this.approving = false;
            this.messageService.add({
              severity: 'success',
              summary: 'Stock Take Completed',
              detail: `${result.adjustmentsApplied} adjustment(s) applied, ${result.linesSkippedUncounted} uncounted line(s) skipped`
            });
            if (result.lotSyncWarnings.length > 0) {
              this.messageService.add({
                severity: 'warn',
                summary: 'Lot Sync Warnings',
                detail: `${result.lotSyncWarnings.length} line(s) adjusted with incomplete lot data — see stock lots`,
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
              summary: 'Approval Failed',
              detail: extractApiError(err, 'Could not approve stock take'),
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
      message: `Cancel stock take ${this.stockTake.stockTakeNumber}? Recorded counts are kept for reference but no stock will be adjusted.`,
      header: 'Cancel Stock Take',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.stockTakeService.cancel(this.stockTake!.id).subscribe({
          next: () => this.reload(),
          error: (err) => this.messageService.add({ severity: 'error', summary: 'Cancel Failed', detail: extractApiError(err, 'Could not cancel') })
        });
      }
    });
  }
}
