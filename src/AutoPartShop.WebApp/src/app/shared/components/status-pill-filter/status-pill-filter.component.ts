import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface StatusPillOption {
    label: string;
    value: string;
}

/**
 * Data-page design system: inline row of status filter pills, replacing a
 * status `p-select` dropdown where a page wants the "Sujan Motors theme"
 * list pattern. Active pill reuses the same orange tint tokens as the
 * existing `.filter-chip` active-filter recap chips, so it matches
 * without introducing new colors.
 *
 * Usage:
 *   <app-status-pill-filter
 *     [options]="statusOptions"
 *     [value]="filterStatus"
 *     (valueChange)="onStatusFilterChange($event)">
 *   </app-status-pill-filter>
 */
@Component({
    selector: 'app-status-pill-filter',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './status-pill-filter.component.html',
    styleUrl: './status-pill-filter.component.scss',
})
export class StatusPillFilterComponent {
    @Input() options: StatusPillOption[] = [];
    @Input() value = '';
    @Output() valueChange = new EventEmitter<string>();

    select(value: string): void {
        if (value === this.value) return;
        this.valueChange.emit(value);
    }
}
