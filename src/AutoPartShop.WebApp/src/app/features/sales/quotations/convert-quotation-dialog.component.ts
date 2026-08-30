import { Component, DestroyRef, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';

import { WarehouseService, WarehouseResponse } from '../../inventory/services/warehouse.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

/**
 * Asks which warehouse a converted quotation should draw stock from.
 *
 * A quotation carries no warehouse of its own, and the resulting sales order needs one before it
 * can be confirmed — without it the conversion succeeded and then every confirm failed with
 * "Warehouse is required for stock deduction", leaving an order that could never be completed.
 */
@Component({
    selector: 'app-convert-quotation-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, DialogModule, ButtonModule, SelectModule, MessageModule, TranslatePipe],
    templateUrl: './convert-quotation-dialog.component.html',
})
export class ConvertQuotationDialogComponent implements OnInit {
    private readonly warehouseService = inject(WarehouseService);
    private readonly destroyRef = inject(DestroyRef);
    readonly i18n = inject(I18nService);

    @Input() visible = false;
    @Input() quotationNumber = '';
    /** Disables the confirm button while the caller's request is in flight. */
    @Input() submitting = false;

    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() confirmed = new EventEmitter<string>();

    warehouseId: string | null = null;
    warehouses = signal<WarehouseResponse[]>([]);
    loading = signal(false);

    ngOnInit(): void {
        this.loading.set(true);
        this.warehouseService
            .getWarehouses({ search: '', pageNumber: 1, pageSize: 1000, sorts: [{ field: 'name', direction: 'asc' }] })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (res) => {
                    this.warehouses.set(res.data ?? []);
                    // One warehouse is the common case for a single-branch shop; pre-select it so
                    // the dialog is a single click.
                    if (this.warehouses().length === 1) {
                        this.warehouseId = this.warehouses()[0].id;
                    }
                    this.loading.set(false);
                },
                error: () => this.loading.set(false)
            });
    }

    onVisibleChange(value: boolean): void {
        this.visible = value;
        this.visibleChange.emit(value);
    }

    close(): void {
        this.onVisibleChange(false);
    }

    submit(): void {
        if (!this.warehouseId) return;
        this.confirmed.emit(this.warehouseId);
    }
}
