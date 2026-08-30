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
    templateUrl: './stat-strip.component.html',
    styleUrl: './stat-strip.component.scss',
})
export class StatStripComponent {
    @Input() stats: StatStripItem[] = [];
}
