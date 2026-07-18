import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService } from 'primeng/api';
import { map } from 'rxjs';

import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '@/shared/components/lazy-autocomplete';
import { CurrencyService } from '@/shared/services/currency.service';
import { SalesOrderService, SalesOrderResponse } from '../../services/sales-order.service';
import {
    ProformaInvoiceService,
    ProformaInvoiceResponse,
    CreateProformaInvoiceRequest
} from '../../services/proforma-invoice.service';

/**
 * "Generate Proforma" dialog. Reused from two entry points:
 *  - Sales Order row action menu — the order is passed in via `salesOrder`, so the picker is
 *    hidden and the order is locked.
 *  - Proforma Invoices list page ("Generate Proforma" button) — no order is pre-selected, so a
 *    Sales Order picker is shown.
 */
@Component({
    selector: 'app-generate-proforma-dialog',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        DialogModule,
        ButtonModule,
        TextareaModule,
        DatePickerModule,
        LazyAutocompleteComponent
    ],
    templateUrl: './generate-proforma-dialog.component.html',
    styleUrls: ['./generate-proforma-dialog.component.scss']
})
export class GenerateProformaDialogComponent implements OnChanges {
    private readonly salesOrderService = inject(SalesOrderService);
    private readonly proformaInvoiceService = inject(ProformaInvoiceService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);

    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();

    /** Pre-selected order (e.g. opened from a Sales Order row action). When set, the picker is hidden. */
    @Input() salesOrder: SalesOrderResponse | null = null;

    @Output() created = new EventEmitter<ProformaInvoiceResponse>();

    selectedOrder: SalesOrderResponse | null = null;
    validUntil: Date | null = null;
    notes = '';
    saving = false;
    error: string | null = null;

    searchSalesOrders = (req: LazyRequest) => {
        return this.salesOrderService
            .getSalesOrders({ search: req.search, pageNumber: req.pageNumber, pageSize: req.pageSize })
            .pipe(
                map((response) => ({ items: response.data, totalCount: response.pagination.totalCount }) as LazyResponse<SalesOrderResponse>)
            );
    };

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['salesOrder']) {
            this.selectedOrder = this.salesOrder;
        }
    }

    onShow(): void {
        this.selectedOrder = this.salesOrder;
        this.validUntil = null;
        this.notes = '';
        this.error = null;
        this.saving = false;
    }

    onHide(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    close(): void {
        this.onHide();
    }

    onOrderSelected(order: SalesOrderResponse): void {
        this.selectedOrder = order;
    }

    onOrderCleared(): void {
        this.selectedOrder = null;
    }

    get lockedOrder(): boolean {
        return !!this.salesOrder;
    }

    formatCurrency(amount: number, currency?: string): string {
        return this.currencyService.formatCurrency(amount, currency || this.currencyService.selectedCurrency());
    }

    submit(): void {
        if (!this.selectedOrder) {
            this.error = 'Please select a sales order.';
            return;
        }

        this.saving = true;
        this.error = null;

        let validUntil: string | null = null;
        if (this.validUntil instanceof Date) {
            const d = this.validUntil;
            validUntil = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        }

        const request: CreateProformaInvoiceRequest = {
            salesOrderId: this.selectedOrder.id,
            validUntil,
            notes: this.notes
        };

        this.proformaInvoiceService.create(request).subscribe({
            next: (proforma) => {
                this.saving = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Proforma ${proforma.proformaNumber} generated.`
                });
                this.created.emit(proforma);
                this.onHide();
            },
            error: (err) => {
                this.saving = false;
                this.error = err?.error?.message ?? 'Failed to generate the proforma invoice.';
            }
        });
    }
}
