import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';

import { OrderService } from '../../../ecommerce/services/order.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { I18nService } from '@/shared/services/i18n.service';
import { SalesOrderResponse } from '../../services/sales-order.service';

@Component({
    selector: 'app-collect-cod-dialog',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        DialogModule,
        ButtonModule,
        InputNumberModule,
        InputTextModule
    ],
    templateUrl: './collect-cod-dialog.component.html',
    styleUrls: ['./collect-cod-dialog.component.css']
})
export class CollectCodDialogComponent {
    private readonly orderService = inject(OrderService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    readonly i18n = inject(I18nService);

    @Input() visible = false;
    @Input() order: SalesOrderResponse | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() collected = new EventEmitter<void>();

    amount: number = 0;
    paymentReference = '';
    saving = false;
    error: string | null = null;

    onShow(): void {
        this.amount = this.order?.outstandingAmount ?? 0;
        this.paymentReference = '';
        this.error = null;
        this.saving = false;
    }

    onHide(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    formatCurrency(amount: number): string {
        return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
    }

    submit(): void {
        if (!this.order || this.amount <= 0) {
            this.error = this.i18n.t('salesOrders.codCollection.invalidAmount');
            return;
        }

        this.saving = true;
        this.error = null;

        this.orderService.collectCod(this.order.soNumber, this.amount, this.paymentReference).subscribe({
            next: () => {
                this.saving = false;
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('salesOrders.codCollection.collectSuccess', { amount: this.formatCurrency(this.amount) })
                });
                this.collected.emit();
                this.onHide();
            },
            error: (err) => {
                this.saving = false;
                this.error = err?.error?.message ?? this.i18n.t('salesOrders.codCollection.collectFailed');
            }
        });
    }
}
