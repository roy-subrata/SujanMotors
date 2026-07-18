import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';

import {
    TillSessionService,
    TillSessionResponse,
    OpenTillSessionRequest,
    RecordCashDropRequest,
    CloseTillSessionRequest
} from '../../services/till-session.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

/**
 * Standalone Till Session admin page — the cashier's own current-session dashboard. Purely a
 * shift cash-management tool; it has no connection to the Quick Sale checkout shortcut and does
 * not gate or block any sales flow.
 *
 * State machine is driven entirely by `session`:
 *  - null            -> "Open Till" form
 *  - status === OPEN  -> session details + Record Cash Drop / Close Till actions
 *  - status === CLOSED -> reconciliation summary + Download Shift Report PDF + "Open New Till"
 */
@Component({
    selector: 'app-till-session-current',
    standalone: true,
    imports: [
        CommonModule,
        RouterLink,
        FormsModule,
        ButtonModule,
        InputTextModule,
        TextareaModule,
        DialogModule,
        ToastModule,
        ConfirmDialogModule,
        TooltipModule,
        PageContainerComponent,
        PageHeaderComponent
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './till-session-current.component.html',
    styleUrls: ['./till-session-current.component.scss']
})
export class TillSessionCurrentComponent implements OnInit {
    private readonly tillSessionService = inject(TillSessionService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    loading = signal(true);
    session = signal<TillSessionResponse | null>(null);

    // ── Open Till form ──────────────────────────────────────────────────
    terminalLabel = '';
    openingFloat = 0;
    shiftLabel = '';
    openNotes = '';
    opening = signal(false);
    openError = signal<string | null>(null);
    /** Hint shown under the Opening Float field when it was pre-filled from this terminal's last closed session. */
    openingFloatSuggestedFromCashier = signal<string | null>(null);
    /** Hint shown under the Shift Label field when it was pre-filled from the cashier's assigned HR shift. */
    shiftLabelSuggestedHours = signal<string | null>(null);

    // ── Record Cash Drop dialog (plain boolean — two-way bound to p-dialog [(visible)]) ─────
    showCashDropDialog = false;
    cashDropAmount = 0;
    cashDropNotes = '';
    recordingDrop = signal(false);
    cashDropError = signal<string | null>(null);

    // ── Close Till dialog (plain boolean — two-way bound to p-dialog [(visible)]) ───────────
    showCloseDialog = false;
    closeCountedAmount = 0;
    closeNotes = '';
    closingSession = signal(false);
    closeError = signal<string | null>(null);

    cashDropsTotal = computed(() => {
        const s = this.session();
        if (!s) return 0;
        return s.cashDrops.reduce((sum, d) => sum + d.amount, 0);
    });

    ngOnInit(): void {
        this.loadCurrent();
    }

    loadCurrent(): void {
        this.loading.set(true);
        this.tillSessionService.getCurrent().subscribe({
            next: (session) => {
                this.session.set(session);
                this.loading.set(false);
                // Shift suggestion doesn't need a terminal (it's resolved from the cashier's own
                // HR assignment) — fetch it now. Opening float DOES need a terminal, so it waits
                // until the cashier types one (see onTerminalBlur()).
                if (session === null) this.loadSuggestions();
            },
            error: (err) => {
                this.loading.set(false);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message ?? 'Failed to load your till session.'
                });
            }
        });
    }

    /**
     * Fetches Open Till suggestions. Shift label is always resolved (cashier-scoped). Opening
     * float is only resolved when `terminalLabel` is given — it's scoped to that specific
     * terminal's own history, not the cashier's, since the cash physically stays in the drawer
     * regardless of who counts it next. Best-effort UI hints only; every field stays editable.
     */
    private loadSuggestions(terminalLabel?: string): void {
        this.tillSessionService.getSuggestedOpeningFloat(terminalLabel).subscribe({
            next: (result) => {
                if (result.suggestedOpeningFloat != null) {
                    this.openingFloat = result.suggestedOpeningFloat;
                    this.openingFloatSuggestedFromCashier.set(result.suggestedOpeningFloatFromCashier);
                }
                if (result.suggestedShiftLabel) {
                    this.shiftLabel = result.suggestedShiftLabel;
                    this.shiftLabelSuggestedHours.set(result.suggestedShiftHours);
                }
            },
            error: () => {
                // Best-effort UI hint only — silently skip if it fails, fields just stay blank.
            }
        });
    }

    /** Re-checks the opening-float suggestion once the cashier has typed/edited a terminal. */
    onTerminalBlur(): void {
        const terminal = this.terminalLabel.trim();
        if (terminal) this.loadSuggestions(terminal);
    }

    // ── Open Till ────────────────────────────────────────────────────────
    openTill(): void {
        if (!this.terminalLabel.trim()) {
            this.openError.set('Terminal label is required.');
            return;
        }
        if (this.openingFloat == null || this.openingFloat < 0) {
            this.openError.set('Opening float must be zero or greater.');
            return;
        }

        this.opening.set(true);
        this.openError.set(null);

        const request: OpenTillSessionRequest = {
            terminalLabel: this.terminalLabel.trim(),
            openingFloat: this.openingFloat,
            shiftLabel: this.shiftLabel.trim() || null,
            notes: this.openNotes.trim()
        };

        this.tillSessionService.open(request).subscribe({
            next: (session) => {
                this.opening.set(false);
                this.session.set(session);
                this.resetOpenForm();
                this.messageService.add({
                    severity: 'success',
                    summary: 'Till Opened',
                    detail: `Till session opened on ${session.terminalLabel}.`
                });
            },
            error: (err) => {
                this.opening.set(false);
                this.openError.set(err?.error?.message ?? 'Failed to open the till session.');
            }
        });
    }

    private resetOpenForm(): void {
        this.terminalLabel = '';
        this.openingFloat = 0;
        this.shiftLabel = '';
        this.openNotes = '';
        this.openError.set(null);
        this.openingFloatSuggestedFromCashier.set(null);
        this.shiftLabelSuggestedHours.set(null);
    }

    /** Reset back to the Open Till form for the next shift, after a session has been closed. */
    startNewSession(): void {
        // Default the terminal to the one just closed — reopening the same counter is the common
        // case — then immediately look up its opening-float suggestion since we already know
        // which terminal to ask about. The cashier can still edit it if they're switching counters.
        const justClosedTerminal = this.session()?.terminalLabel ?? '';
        this.session.set(null);
        this.terminalLabel = justClosedTerminal;
        this.loadSuggestions(justClosedTerminal || undefined);
    }

    // ── Record Cash Drop ─────────────────────────────────────────────────
    openCashDropDialog(): void {
        this.cashDropAmount = 0;
        this.cashDropNotes = '';
        this.cashDropError.set(null);
        this.showCashDropDialog = true;
    }

    submitCashDrop(): void {
        const current = this.session();
        if (!current) return;

        if (!this.cashDropAmount || this.cashDropAmount <= 0) {
            this.cashDropError.set('Amount must be greater than zero.');
            return;
        }

        this.recordingDrop.set(true);
        this.cashDropError.set(null);

        const request: RecordCashDropRequest = {
            amount: this.cashDropAmount,
            notes: this.cashDropNotes.trim()
        };

        this.tillSessionService.recordCashDrop(current.id, request).subscribe({
            next: (session) => {
                this.recordingDrop.set(false);
                this.session.set(session);
                this.showCashDropDialog = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Cash Drop Recorded',
                    detail: `${this.formatCurrency(request.amount)} recorded as a cash drop.`
                });
            },
            error: (err) => {
                this.recordingDrop.set(false);
                this.cashDropError.set(err?.error?.message ?? 'Failed to record the cash drop.');
            }
        });
    }

    // ── Close Till ───────────────────────────────────────────────────────
    openCloseDialog(): void {
        const current = this.session();
        this.closeCountedAmount = current?.expectedAmount ?? 0;
        this.closeNotes = '';
        this.closeError.set(null);
        this.showCloseDialog = true;
    }

    confirmClose(): void {
        const current = this.session();
        if (!current) return;

        if (this.closeCountedAmount == null || this.closeCountedAmount < 0) {
            this.closeError.set('Counted amount must be zero or greater.');
            return;
        }

        this.confirmationService.confirm({
            message: 'Closing the till freezes its reconciliation and cannot be undone. Continue?',
            header: 'Close Till Session',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => this.doClose(current.id)
        });
    }

    private doClose(id: string): void {
        this.closingSession.set(true);
        this.closeError.set(null);

        const request: CloseTillSessionRequest = {
            countedAmount: this.closeCountedAmount,
            notes: this.closeNotes.trim()
        };

        this.tillSessionService.close(id, request).subscribe({
            next: (session) => {
                this.closingSession.set(false);
                this.session.set(session);
                this.showCloseDialog = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Till Closed',
                    detail: `Till session on ${session.terminalLabel} closed.`
                });
            },
            error: (err) => {
                this.closingSession.set(false);
                this.closeError.set(err?.error?.message ?? 'Failed to close the till session.');
            }
        });
    }

    // ── PDF ──────────────────────────────────────────────────────────────
    downloadPdf(): void {
        const current = this.session();
        if (!current || current.status !== 'CLOSED') return;

        this.tillSessionService.downloadPdf(current.id, current.terminalLabel, current.openedAt).subscribe({
            error: () => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to download the shift report PDF.'
                });
            }
        });
    }

    // ── Formatting helpers ───────────────────────────────────────────────
    formatCurrency(amount: number | null | undefined): string {
        if (amount == null || isNaN(amount)) return '—';
        return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
    }

    formatDateTime(date: string | null | undefined): string {
        if (!date) return '-';
        return new Date(date).toLocaleString('en-IN', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    overShortClass(amount: number | null | undefined): string {
        if (amount == null) return '';
        if (Math.abs(amount) < 0.005) return 'amount-exact';
        return amount > 0 ? 'amount-over' : 'amount-short';
    }
}
