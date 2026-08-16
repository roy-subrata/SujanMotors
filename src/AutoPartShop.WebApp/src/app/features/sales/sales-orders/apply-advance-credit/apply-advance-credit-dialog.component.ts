import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { ApplyCustomerAdvanceCreditRequest, ApplyCustomerAdvanceCreditResponse, AvailableCustomerAdvancePayment, CustomerPaymentService } from '../../services/customer-payment.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
    selector: 'app-apply-customer-advance-credit-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, InputNumberModule, TableModule, TagModule,
        TranslatePipe],
    templateUrl: './apply-advance-credit-dialog.component.html',
    styleUrls: ['./apply-advance-credit-dialog.component.scss']
})
export class ApplyCustomerAdvanceCreditDialogComponent implements OnInit {
    private readonly customerPaymentService: CustomerPaymentService = inject(CustomerPaymentService);
    private readonly dialogRef: DynamicDialogRef = inject(DynamicDialogRef);
    private readonly config: DynamicDialogConfig = inject(DynamicDialogConfig);
    private readonly messageService: MessageService = inject(MessageService);
    private readonly currencyService: CurrencyService = inject(CurrencyService);
    private readonly i18n = inject(I18nService);

    availableAdvances: AvailableCustomerAdvancePayment[] = [];
    selectedAdvance: AvailableCustomerAdvancePayment | null = null;
    amountToApply: number = 0;
    description: string = '';
    isLoading = false;
    isApplying = false;

    customerId: string = '';
    invoiceId: string = '';
    invoiceOutstandingAmount: number = 0;

    ngOnInit() {
        this.customerId = this.config.data?.customerId;
        this.invoiceId = this.config.data?.invoiceId;
        this.invoiceOutstandingAmount = this.config.data?.invoiceOutstandingAmount || 0;

        if (this.customerId) {
            this.loadAvailableAdvances();
        }
    }

    loadAvailableAdvances() {
        this.isLoading = true;
        this.customerPaymentService.getAvailableAdvances(this.customerId).subscribe({
            next: (advances: AvailableCustomerAdvancePayment[]) => {
                this.availableAdvances = advances;
                this.isLoading = false;
            },
            error: (error: any) => {
                console.error('Error loading available advances:', error);
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('applyAdvanceCredit.messages.loadFailed')
                });
                this.isLoading = false;
            }
        });
    }

    getMaxAmount(): number {
        if (!this.selectedAdvance) return 0;
        return Math.min(this.selectedAdvance.remainingAmount, this.invoiceOutstandingAmount);
    }

    canApply(): boolean {
        return !!(this.selectedAdvance && this.amountToApply > 0 && this.amountToApply <= this.getMaxAmount());
    }

    onApply() {
        if (!this.canApply() || !this.selectedAdvance) return;

        this.isApplying = true;

        const request: ApplyCustomerAdvanceCreditRequest = {
            invoiceId: this.invoiceId,
            sourceAdvancePaymentId: this.selectedAdvance.id,
            amount: this.amountToApply,
            description: this.description || `Applied from advance ${this.selectedAdvance.transactionNumber}`
        };

        this.customerPaymentService.applyAdvanceCredit(request).subscribe({
            next: (response: ApplyCustomerAdvanceCreditResponse) => {
                this.isApplying = false;
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: response.message
                });
                this.dialogRef.close(response);
            },
            error: (error: any) => {
                console.error('Error applying advance credit:', error);
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('applyAdvanceCredit.messages.applyFailed'))
                });
                this.isApplying = false;
            }
        });
    }

    onCancel() {
        this.dialogRef.close();
    }

    formatCurrency(value: number): string {
        return this.currencyService.formatCurrency(value, this.currencyService.selectedCurrency());
    }

    get currencyCode(): string {
        return this.currencyService.selectedCurrency();
    }

    get currencyLocale(): string {
        return this.currencyService.getSelectedCurrencyLocale();
    }

    formatDate(date: string): string {
        return new Date(date).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }
}
