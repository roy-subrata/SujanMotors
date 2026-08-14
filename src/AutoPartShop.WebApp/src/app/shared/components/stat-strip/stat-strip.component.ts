import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface StatStripItem {
    label: string;
    value: string;
    caption?: string;
}

/**
 * Data-page design system: a strip of KPI cells below the page header
 * (label, big number, optional caption). Renders as many cells as are
 * passed — reads as 4-up on desktop widths but wraps gracefully, so
 * callers with fewer real numbers to show don't need to pad with
 * fabricated stats.
 *
 * Usage:
 *   <app-stat-strip [stats]="[
 *     { label: 'Total Parts', value: '49' },
 *     { label: 'Active', value: '45' },
 *     { label: 'Inactive', value: '4' }
 *   ]"></app-stat-strip>
 */
@Component({
    selector: 'app-stat-strip',
    standalone: true,
    imports: [CommonModule],
    template: `
        @if (stats.length) {
            <div class="stat-strip">
                @for (stat of stats; track stat.label) {
                    <div class="stat-cell">
                        <span class="stat-label">{{ stat.label }}</span>
                        <span class="stat-value">{{ stat.value }}</span>
                        @if (stat.caption) {
                            <span class="stat-caption">{{ stat.caption }}</span>
                        }
                    </div>
                }
            </div>
        }
    `,
    styles: [`
        .stat-strip {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
            background-color: var(--color-bg-primary);
            border: 1px solid var(--color-border);
            border-radius: var(--radius-lg);
            margin-bottom: 16px;
            overflow: hidden;
        }

        .stat-cell {
            display: flex;
            flex-direction: column;
            gap: 3px;
            padding: 14px 16px;
            border-right: 1px solid var(--color-border-light);
            border-bottom: 1px solid var(--color-border-light);
            min-width: 0;
        }

        .stat-cell:last-child {
            border-right: none;
        }

        .stat-label {
            font-size: 11px;
            font-weight: 600;
            color: var(--color-text-muted);
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .stat-value {
            font-size: 19px;
            font-weight: 800;
            color: var(--color-text-primary);
            line-height: 1.2;
        }

        .stat-caption {
            font-size: 11px;
            color: var(--color-text-secondary);
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        @media (max-width: 640px) {
            .stat-strip {
                grid-template-columns: repeat(2, 1fr);
            }
        }
    `]
})
export class StatStripComponent {
    @Input() stats: StatStripItem[] = [];
}
