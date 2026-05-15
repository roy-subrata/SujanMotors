import { Component, EventEmitter, Input, Output, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { CreditNoteService, CreditNoteResponse } from '../services/credit-note.service';

@Component({
  selector: 'app-apply-credit-notes',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    TableModule,
    CardModule,
    InputNumberModule,
    ToastModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './apply-credit-notes.component.html',
  styleUrls: ['./apply-credit-notes.component.css']
})
export class ApplyCreditNotesComponent implements OnInit {
  private readonly creditNoteService = inject(CreditNoteService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  @Input() supplierId: string | null = null;
  @Input() purchaseOrderId: string | null = null;
  @Input() poTotalAmount: number = 0;
  @Input() poPaidAmount: number = 0;

  @Output() creditApplied = new EventEmitter<number>();

  availableCredits: CreditNoteResponse[] = [];
  selectedCreditNote: CreditNoteResponse | null = null;
  amountToApply: number | null = null;
  loading = false;
  totalAvailableCredit = 0;

  ngOnInit(): void {
    if (this.supplierId) {
      this.loadAvailableCredits();
    }
  }

  loadAvailableCredits(): void {
    if (!this.supplierId) return;

    this.loading = true;
    this.creditNoteService.getAvailableCredits(this.supplierId).subscribe({
      next: (credits: CreditNoteResponse[]) => {
        this.availableCredits = credits;
        this.totalAvailableCredit = credits.reduce((sum, c) => sum + c.availableAmount, 0);
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load available credits'
        });
        this.loading = false;
      }
    });
  }

  selectCredit(credit: CreditNoteResponse): void {
    this.selectedCreditNote = credit;
    this.amountToApply = null;
  }

  applyCredit(): void {
    if (!this.selectedCreditNote || !this.amountToApply || !this.purchaseOrderId) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please select a credit note and enter amount to apply'
      });
      return;
    }

    const outstandingAmount = this.poTotalAmount - this.poPaidAmount;
    if (this.amountToApply > outstandingAmount) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: `Amount exceeds outstanding amount (${outstandingAmount})`
      });
      return;
    }

    if (this.amountToApply > this.selectedCreditNote.availableAmount) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: `Amount exceeds available credit (${this.selectedCreditNote.availableAmount})`
      });
      return;
    }

    this.confirmationService.confirm({
      message: `Apply ${this.amountToApply} from credit note ${this.selectedCreditNote.creditNoteNumber} to this purchase order?`,
      header: 'Confirm Credit Application',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.creditNoteService.applyCredit({
          creditNoteId: this.selectedCreditNote!.id,
          purchaseOrderId: this.purchaseOrderId!,
          amountToApply: this.amountToApply!
        }).subscribe({
          next: (_response: CreditNoteResponse) => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Credit note applied successfully`
            });
            this.creditApplied.emit(this.amountToApply!);
            this.selectedCreditNote = null;
            this.amountToApply = null;
            this.loadAvailableCredits();
          },
          error: (_err: unknown) => {
            const errorMsg = _err && typeof _err === 'object' && 'error' in _err
              ? (_err as { error?: { message?: string } }).error?.message
              : 'Failed to apply credit note';
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: errorMsg
            });
          }
        });
      }
    });
  }

  getOutstandingAmount(): number {
    return this.poTotalAmount - this.poPaidAmount;
  }

  resetSelection(): void {
    this.selectedCreditNote = null;
    this.amountToApply = null;
  }
}
