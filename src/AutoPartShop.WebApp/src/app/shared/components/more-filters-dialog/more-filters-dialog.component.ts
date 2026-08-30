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
    templateUrl: './more-filters-dialog.component.html',
    styleUrl: './more-filters-dialog.component.scss',
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
