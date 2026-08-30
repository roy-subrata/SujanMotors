import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

/**
 * Generic "more filters" modal for list pages whose filter set is too large
 * to fit inline in the filter bar. Unlike `MoreFiltersDialogComponent` (a
 * single hardcoded radio-button list with no content projection), this
 * component owns only the dialog chrome — callers project arbitrary filter
 * controls via `<ng-content>` and remain fully responsible for how those
 * controls apply/clear. There is no pending/staging state: projected
 * controls apply live exactly as they would inline, so "Done" is a plain
 * close action, not an "Apply".
 *
 * Usage:
 *   <app-extensive-filter-dialog
 *     [(visible)]="extensiveFiltersVisible"
 *     [title]="'parts.attributeFiltersDialogTitle' | translate"
 *     [activeCount]="activeAttributeFilterCount"
 *     (clear)="clearAttributeFilters()">
 *     <div filters class="filter-group" *ngFor="let ctrl of attributeFilterControls"> ... </div>
 *   </app-extensive-filter-dialog>
 */
@Component({
    selector: 'app-extensive-filter-dialog',
    standalone: true,
    imports: [CommonModule, DialogModule, TranslatePipe],
    templateUrl: './extensive-filter-dialog.component.html',
    styleUrls: ['./extensive-filter-dialog.component.css']
})
export class ExtensiveFilterDialogComponent {
    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    /** Caller passes an already-translated title — this component does no i18n of its own content. */
    @Input() title = '';
    /** Informational pass-through only (e.g. for a caller-defined header hint) — not rendered here. */
    @Input() activeCount = 0;
    @Output() clear = new EventEmitter<void>();

    close(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }
}
