import { Component, input, output, effect, viewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

export interface PosCatalogChip {
    label: string;
    active: boolean;
}

export interface PosCatalogTile {
    partId: string;
    variantId: string | null;
    meta: string;
    name: string;
    priceLabel: string;
    stockLabel: string;
    /** 'red' | 'amber' | 'muted' — mapped to a CSS class in the template. */
    stockTone: 'red' | 'amber' | 'muted';
    blocked: boolean;
}

/**
 * Catalog column (design_handoff_pos_sale §1b): single-select category chips over a
 * client-filtered product grid. Unavailable tiles stay visually flagged but still clickable
 * (backend remains the authority on stock — see plan decision #2).
 */
@Component({
    selector: 'app-pos-catalog',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './pos-catalog.component.html',
    styleUrl: './pos-catalog.component.css'
})
export class PosCatalogComponent {
    chips = input<PosCatalogChip[]>([]);
    tiles = input<PosCatalogTile[]>([]);
    loading = input<boolean>(false);
    /** True while the next page is being fetched — shows a small spinner under the grid. */
    loadingMore = input<boolean>(false);

    chipToggle = output<string>();
    addToCart = output<PosCatalogTile>();
    /** Emitted once when the grid is scrolled near its bottom — the orchestrator debounces this
     *  itself via `catalogLoadingMore`/`catalogHasMore` guards, so this fires freely on every
     *  qualifying scroll event without its own throttle. */
    loadMore = output<void>();

    private gridScroll = viewChild<ElementRef<HTMLElement>>('gridScroll');

    constructor() {
        // A page of tiles may not fill (let alone overflow) a tall/wide viewport, in which case
        // scrolling can never happen and onGridScroll() below would never fire even though more
        // pages exist server-side. After every tile-list change, check whether the grid still
        // doesn't overflow its container and ask for another page if so — the orchestrator's own
        // catalogLoadingMore/catalogHasMore guards make this safe to call repeatedly/no-op once
        // the catalog is either full-height or exhausted.
        effect(() => {
            this.tiles();
            queueMicrotask(() => {
                const el = this.gridScroll()?.nativeElement;
                if (el && el.scrollHeight <= el.clientHeight + 4) this.loadMore.emit();
            });
        });
    }

    trackTile(_: number, tile: PosCatalogTile): string {
        return tile.partId + '|' + (tile.variantId ?? '');
    }

    /** Triggers loadMore once the user has scrolled within ~2 tile-rows of the bottom. */
    onGridScroll(event: Event): void {
        const el = event.target as HTMLElement;
        if (el.scrollTop + el.clientHeight >= el.scrollHeight - 300) {
            this.loadMore.emit();
        }
    }
}
