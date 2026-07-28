import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';

import {
    ProductSpecification,
    ProductSpecificationService,
    SaveSpecificationItem
} from '../../services/product-specification.service';

/** One editable row. Suggestions are held per-row so overlays don't share state. */
interface SpecRow {
    label: string;
    value: string;
    labelSuggestions: string[];
    valueSuggestions: string[];
}

/**
 * Editor for a product's simple Label/Value specs — the product-scoped specs
 * shown on the storefront's Specifications tab, not the variant attribute EAV
 * (that lives under Inventory → Attribute Groups).
 *
 * Saving is a full replace and display order follows row order, matching the
 * API and the mobile editor.
 */
@Component({
    selector: 'app-product-specs-manager',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, AutoCompleteModule, ToastModule, TooltipModule],
    providers: [MessageService],
    templateUrl: './product-specs-manager.component.html',
    styleUrls: ['./product-specs-manager.component.css']
})
export class ProductSpecsManagerComponent implements OnInit {
    @Input() partId!: string;

    private readonly specService = inject(ProductSpecificationService);
    private readonly messageService = inject(MessageService);

    rows: SpecRow[] = [];
    loading = false;
    saving = false;

    /** Serialized snapshot of the last saved state, used for the dirty check. */
    private savedSnapshot = '';

    ngOnInit(): void {
        this.load();
    }

    private load(): void {
        if (!this.partId) return;
        this.loading = true;
        this.specService.getByPart(this.partId).subscribe({
            next: (specs) => {
                this.setRows(specs);
                this.loading = false;
            },
            error: (err) => {
                this.loading = false;
                this.toastError(err, 'Failed to load specifications');
            }
        });
    }

    private setRows(specs: ProductSpecification[]): void {
        this.rows = specs.map((s) => this.newRow(s.label, s.value));
        this.savedSnapshot = this.snapshot();
    }

    private newRow(label = '', value = ''): SpecRow {
        return { label, value, labelSuggestions: [], valueSuggestions: [] };
    }

    // ── Row editing ───────────────────────────────────────────────────────────

    addRow(): void {
        this.rows = [...this.rows, this.newRow()];
    }

    removeRow(index: number): void {
        this.rows = this.rows.filter((_, i) => i !== index);
    }

    moveUp(index: number): void {
        if (index === 0) return;
        const next = [...this.rows];
        [next[index - 1], next[index]] = [next[index], next[index - 1]];
        this.rows = next;
    }

    moveDown(index: number): void {
        if (index === this.rows.length - 1) return;
        const next = [...this.rows];
        [next[index], next[index + 1]] = [next[index + 1], next[index]];
        this.rows = next;
    }

    // ── Typeahead ─────────────────────────────────────────────────────────────

    searchLabels(row: SpecRow, event: AutoCompleteCompleteEvent): void {
        this.specService.suggestions('label', event.query).subscribe({
            next: (options) => (row.labelSuggestions = options),
            error: () => (row.labelSuggestions = [])
        });
    }

    /** Value suggestions are scoped to the row's label so they stay relevant. */
    searchValues(row: SpecRow, event: AutoCompleteCompleteEvent): void {
        this.specService.suggestions('value', event.query, (row.label || '').trim() || undefined).subscribe({
            next: (options) => (row.valueSuggestions = options),
            error: () => (row.valueSuggestions = [])
        });
    }

    // ── Validation helpers ────────────────────────────────────────────────────

    /**
     * Mirrors the server's key normalization so the duplicate warning matches the
     * facet key rows would actually collapse to. "Front Axle" -> "front-axle".
     */
    private normalize(label: string): string {
        return (label ?? '')
            .trim()
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '');
    }

    /** True when another row uses the same normalized label — a likely mistake, not an error. */
    isDuplicate(index: number): boolean {
        const key = this.normalize(this.rows[index].label);
        if (!key) return false;
        return this.rows.some((r, i) => i !== index && this.normalize(r.label) === key);
    }

    /** Rows with a blank label are dropped on save (the API ignores them too). */
    isIgnored(row: SpecRow): boolean {
        return !row.label?.trim() && !!row.value?.trim();
    }

    get filledCount(): number {
        return this.rows.filter((r) => r.label?.trim()).length;
    }

    private snapshot(): string {
        return JSON.stringify(
            this.rows.filter((r) => r.label?.trim()).map((r) => [r.label.trim(), (r.value ?? '').trim()])
        );
    }

    get isDirty(): boolean {
        return this.snapshot() !== this.savedSnapshot;
    }

    // ── Save / reset ──────────────────────────────────────────────────────────

    save(): void {
        if (!this.partId || this.saving) return;

        const payload: SaveSpecificationItem[] = this.rows
            .filter((r) => r.label?.trim())
            .map((r) => ({ label: r.label.trim(), value: (r.value ?? '').trim() }));

        this.saving = true;
        this.specService.save(this.partId, payload).subscribe({
            next: (specs) => {
                this.setRows(specs);
                this.saving = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Saved',
                    detail: specs.length ? `${specs.length} specification(s) saved` : 'Specifications cleared'
                });
            },
            error: (err) => {
                this.saving = false;
                this.toastError(err, 'Failed to save specifications');
            }
        });
    }

    reset(): void {
        this.load();
    }

    private toastError(err: unknown, fallback: string): void {
        const detail = (err as { error?: { message?: string } })?.error?.message || fallback;
        this.messageService.add({ severity: 'error', summary: 'Error', detail });
    }
}
