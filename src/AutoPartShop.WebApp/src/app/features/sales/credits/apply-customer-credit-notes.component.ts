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
import { TooltipModule } from 'primeng/tooltip';
import { CustomerCreditNoteService, CustomerCreditNoteResponse } from '../services/customer-credit-note.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { AppCurrencyPipe } from '@/shared/pipes/app-currency.pipe';

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
    ConfirmDialogModule,
    TooltipModule,
    TranslatePipe,
    AppCurrencyPipe
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './apply-customer-credit-notes.component.html',
  styleUrls: ['./apply-customer-credit-notes.component.css']
})
export class ApplyCustomerCreditNotesComponent implements OnInit {
  private readonly creditNoteService = inject(CustomerCreditNoteService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly i18n = inject(I18nService);

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
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('customerCredits.messages.loadFailed')
        });
        this.loading = false;
      }
    });
  }

  selectCredit(credit: CustomerCreditNoteResponse): void {
    this.selectedCreditNote = credit;
    this.amountToApply = null;
  }

  downloadPdf(credit: CustomerCreditNoteResponse): void {
    this.creditNoteService.downloadPdf(credit.id, credit.creditNoteNumber).subscribe({
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('customerCredits.messages.pdfFailed')
        });
      }
    });
  }

  applyCredit(): void {
    if (!this.selectedCreditNote || !this.amountToApply || !this.salesOrderId) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('customerCredits.messages.validationTitle'),
        detail: this.i18n.t('customerCredits.messages.selectCreditAndAmount')
      });
      return;
    }

    if (!this.invoiceId) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('customerCredits.messages.validationTitle'),
        detail: this.i18n.t('customerCredits.messages.invoiceRequired')
      });
      return;
    }

    const outstandingAmount = this.soTotalAmount - this.soPaidAmount;
    if (this.amountToApply > outstandingAmount) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('customerCredits.messages.validationTitle'),
        detail: this.i18n.t('customerCredits.messages.exceedsOutstanding', { amount: outstandingAmount })
      });
      return;
    }

    if (this.amountToApply > this.selectedCreditNote.availableAmount) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('customerCredits.messages.validationTitle'),
        detail: this.i18n.t('customerCredits.messages.exceedsAvailable', { amount: this.selectedCreditNote.availableAmount })
      });
      return;
    }

    this.confirmationService.confirm({
      message: this.i18n.t('customerCredits.messages.applyConfirm', { amount: this.amountToApply, number: this.selectedCreditNote.creditNoteNumber }),
      header: this.i18n.t('customerCredits.messages.applyHeader'),
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
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('customerCredits.messages.applySuccess')
            });
            this.creditApplied.emit(this.amountToApply!);
            this.selectedCreditNote = null;
            this.amountToApply = null;
            this.loadAvailableCredits();
          },
          error: (_err: unknown) => {
            const errorMsg = _err && typeof _err === 'object' && 'error' in _err
              ? (_err as { error?: { message?: string } }).error?.message
              : this.i18n.t('customerCredits.messages.applyFailed');
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
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
