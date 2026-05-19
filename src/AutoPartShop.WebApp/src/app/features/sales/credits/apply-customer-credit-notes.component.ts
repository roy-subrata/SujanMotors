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
import { CustomerCreditNoteService, CustomerCreditNoteResponse } from '../services/customer-credit-note.service';

@Component({
  selector: 'app-apply-customer-credit-notes',
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
  templateUrl: './apply-customer-credit-notes.component.html',
  styleUrls: ['./apply-customer-credit-notes.component.css']
})
export class ApplyCustomerCreditNotesComponent implements OnInit {
  private readonly creditNoteService = inject(CustomerCreditNoteService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  @Input() customerId: string | null = null;
  @Input() salesOrderId: string | null = null;
  @Input() invoiceId: string | null = null;
  @Input() soTotalAmount: number = 0;
  @Input() soPaidAmount: number = 0;

  @Output() creditApplied = new EventEmitter<number>();

  availableCredits: CustomerCreditNoteResponse[] = [];
  selectedCreditNote: CustomerCreditNoteResponse | null = null;
  amountToApply: number | null = null;
  loading = false;
  totalAvailableCredit = 0;

  ngOnInit(): void {
    if (this.customerId) {
      this.loadAvailableCredits();
    }
  }

  loadAvailableCredits(): void {
    if (!this.customerId) return;

    this.loading = true;
    this.creditNoteService.getAvailableCredits(this.customerId).subscribe({
      next: (credits: CustomerCreditNoteResponse[]) => {
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

  selectCredit(credit: CustomerCreditNoteResponse): void {
    this.selectedCreditNote = credit;
    this.amountToApply = null;
  }

  applyCredit(): void {
    if (!this.selectedCreditNote || !this.amountToApply || !this.salesOrderId) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please select a credit note and enter amount to apply'
      });
      return;
    }

    if (!this.invoiceId) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Invoice is required to apply credit'
      });
      return;
    }

    const outstandingAmount = this.soTotalAmount - this.soPaidAmount;
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
      message: `Apply ${this.amountToApply} from credit note ${this.selectedCreditNote.creditNoteNumber} to this sales order?`,
      header: 'Confirm Credit Application',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.creditNoteService.applyCredit({
          creditNoteId: this.selectedCreditNote!.id,
          invoiceId: this.invoiceId!,
          salesOrderId: this.salesOrderId!,
          amountToApply: this.amountToApply!
        }).subscribe({
          next: (_response: CustomerCreditNoteResponse) => {
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
    return this.soTotalAmount - this.soPaidAmount;
  }

  resetSelection(): void {
    this.selectedCreditNote = null;
    this.amountToApply = null;
  }
}
