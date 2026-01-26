import { Component, EventEmitter, inject, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { BadgeModule } from 'primeng/badge';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { UnitConversionService, UnitConversionResponse } from '../../services/unit-conversion.service';

@Component({
    selector: 'app-conversions-list',
    standalone: true,
    imports: [CommonModule, TableModule, ButtonModule, InputTextModule, TooltipModule, BadgeModule, TagModule, FormsModule],
    templateUrl: './conversions-list.component.html',
    styleUrls: ['./conversions-list.component.css']
})
export class ConversionsListComponent implements OnInit {
    @Output() editConversion = new EventEmitter<UnitConversionResponse>();
    @Output() deleteConversion = new EventEmitter<UnitConversionResponse>();
    @Output() toggleStatus = new EventEmitter<UnitConversionResponse>();

    private readonly conversionService = inject(UnitConversionService);
    private readonly messageService = inject(MessageService);

    conversions: UnitConversionResponse[] = [];
    selectedConversions: UnitConversionResponse[] = [];
    loading = false;
    searchTerm = '';

    ngOnInit(): void {
        this.loadConversions();
    }

    /**
     * Load all conversions
     */
    loadConversions(): void {
        this.loading = true;

        this.conversionService.getAllConversions().subscribe({
            next: (response) => {
                this.conversions = response;
                this.loading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to load conversions'
                });
                this.loading = false;
            }
        });
    }

    /**
     * Search conversions by unit names
     */
    search(query: string): void {
        this.searchTerm = query.toLowerCase();
    }

    /**
     * Get filtered conversions based on search
     */
    getFilteredConversions(): UnitConversionResponse[] {
        if (!this.searchTerm) {
            return this.conversions;
        }

        return this.conversions.filter(
            (conversion) =>
                conversion.fromUnitName.toLowerCase().includes(this.searchTerm) ||
                conversion.fromUnitCode.toLowerCase().includes(this.searchTerm) ||
                conversion.toUnitName.toLowerCase().includes(this.searchTerm) ||
                conversion.toUnitCode.toLowerCase().includes(this.searchTerm)
        );
    }

    /**
     * Clear search
     */
    clearSearch(): void {
        this.searchTerm = '';
    }

    /**
     * Handle edit action
     */
    onEdit(conversion: UnitConversionResponse): void {
        this.editConversion.emit(conversion);
    }

    /**
     * Handle delete action
     */
    onDelete(conversion: UnitConversionResponse): void {
        this.deleteConversion.emit(conversion);
    }

    /**
     * Handle toggle status action
     */
    onToggleStatus(conversion: UnitConversionResponse): void {
        this.toggleStatus.emit(conversion);
    }

    /**
     * Reload conversions list
     */
    reload(): void {
        this.searchTerm = '';
        this.selectedConversions = [];
        this.loadConversions();
    }

    /**
     * Format conversion display
     */
    getConversionDisplay(conversion: UnitConversionResponse): string {
        return `1 ${conversion.fromUnitCode} = ${conversion.conversionFactor} ${conversion.toUnitCode}`;
    }
}
