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
    template: `
        <div class="status-pill-row">
            @for (option of options; track option.value) {
                <button
                    type="button"
                    class="status-pill-btn"
                    [class.active]="option.value === value"
                    (click)="select(option.value)">
                    {{ option.label }}
                </button>
            }
        </div>
    `,
    styles: [`
        .status-pill-row {
            display: flex;
            align-items: center;
            gap: 6px;
            flex-wrap: wrap;
        }

        .status-pill-btn {
            display: inline-flex;
            align-items: center;
            padding: 5px 12px;
            font-size: 12.5px;
            font-weight: 500;
            border-radius: var(--radius-full);
            border: 1px solid var(--color-border);
            background-color: var(--color-bg-primary);
            color: var(--color-text-secondary);
            cursor: pointer;
            transition: all var(--transition-fast);
            white-space: nowrap;
        }

        .status-pill-btn:hover {
            background-color: var(--color-bg-secondary);
            color: var(--color-text-primary);
        }

        .status-pill-btn.active {
            background-color: var(--color-primary-light);
            border-color: var(--color-primary);
            color: var(--color-primary);
            font-weight: 600;
        }
    `]
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
