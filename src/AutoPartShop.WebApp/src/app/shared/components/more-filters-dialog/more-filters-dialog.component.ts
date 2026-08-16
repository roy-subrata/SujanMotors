import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { RadioButtonModule } from 'primeng/radiobutton';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

export interface MoreFiltersOption {
    label: string;
    value: string;
}

/**
 * Data-page design system: the "More filters" modal — a centered dialog
 * (PrimeNG's own [modal]="true" already gives the dark overlay + centered
 * layout the design calls for) offering the same option list as an
 * `<app-status-pill-filter>` row, for pages/screens where a compact dialog
 * is preferred over scanning the inline pill row. Single-select today
 * (both pilot pages filter by one status at a time) — rendered as radio
 * buttons rather than checkboxes for that reason.
 *
 * Usage:
 *   <app-more-filters-dialog
 *     [options]="statusOptions"
 *     [selected]="filterStatus"
 *     [previewCount]="totalRecords"
 *     [(visible)]="moreFiltersVisible"
 *     (apply)="onStatusFilterChange($event)">
 *   </app-more-filters-dialog>
 */
@Component({
    selector: 'app-more-filters-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, DialogModule, RadioButtonModule, TranslatePipe],
    template: `
        <p-dialog
            [header]="'common.labels.moreFilters' | translate"
            [modal]="true"
            [(visible)]="visible"
            (visibleChange)="visibleChange.emit($event)"
            (onShow)="pending = selected"
            [style]="{ width: '360px' }"
            [dismissableMask]="true">
            <div class="more-filters-options">
                @for (option of options; track option.value) {
                    <label class="more-filters-option">
                        <p-radioButton
                            name="moreFiltersStatus"
                            [value]="option.value"
                            [(ngModel)]="pending">
                        </p-radioButton>
                        <span>{{ option.label }}</span>
                    </label>
                }
            </div>
            <ng-template pTemplate="footer">
                <button type="button" class="btn-secondary" (click)="cancel()">{{ 'common.actions.cancel' | translate }}</button>
                <button type="button" class="btn-primary" (click)="show()">{{ 'common.actions.show' | translate }} {{ previewCount }}</button>
            </ng-template>
        </p-dialog>
    `,
    styles: [`
        .more-filters-options {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .more-filters-option {
            display: flex;
            align-items: center;
            gap: 10px;
            font-size: 13px;
            color: var(--color-text-primary);
            cursor: pointer;
        }
    `]
})
export class MoreFiltersDialogComponent {
    @Input() options: MoreFiltersOption[] = [];
    @Input() selected = '';
    @Input() previewCount = 0;
    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() apply = new EventEmitter<string>();

    pending = '';

    cancel(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    show(): void {
        this.visible = false;
        this.visibleChange.emit(false);
        this.apply.emit(this.pending);
    }
}
