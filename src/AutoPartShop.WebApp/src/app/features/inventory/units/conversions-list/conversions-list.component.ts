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
import { Select } from 'primeng/select';

@Component({
    selector: 'app-conversions-list',
    standalone: true,
    imports: [CommonModule, TableModule, ButtonModule, InputTextModule, TooltipModule, BadgeModule, TagModule, FormsModule, Select],
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
    filteredConversions: UnitConversionResponse[] = [];
    pagedConversions: UnitConversionResponse[] = [];
    selectedConversions: UnitConversionResponse[] = [];
    loading = false;
    searchTerm = '';
    pageNumber = 1;
    pageSize = 10;
    totalRecords = 0;

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
                this.applyFilters();
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
        this.pageNumber = 1;
        this.applyFilters();
    }

    /**
     * Clear search
     */
    clearSearch(): void {
        this.searchTerm = '';
        this.pageNumber = 1;
        this.applyFilters();
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

    onPageChange(event: any): void {
        if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
            return;
        }
        const pageNum = Math.floor(event.first / event.rows) + 1;
        this.pageSize = event.rows;
        this.pageNumber = pageNum;
        this.applyPagination();
    }

    get first(): number {
        return Math.max(0, (this.pageNumber - 1) * this.pageSize);
    }

    get pageNumberDisplay(): number {
        if (!this.totalRecords) {
            return 0;
        }
        return Math.floor(this.first / this.pageSize) + 1;
    }

    get totalPages(): number {
        if (!this.totalRecords) {
            return 0;
        }
        return Math.ceil(this.totalRecords / this.pageSize);
    }

    private applyFilters(): void {
        if (!this.searchTerm) {
            this.filteredConversions = [...this.conversions];
        } else {
            this.filteredConversions = this.conversions.filter(
                (conversion) =>
                    conversion.fromUnitName.toLowerCase().includes(this.searchTerm) ||
                    conversion.fromUnitCode.toLowerCase().includes(this.searchTerm) ||
                    conversion.toUnitName.toLowerCase().includes(this.searchTerm) ||
                    conversion.toUnitCode.toLowerCase().includes(this.searchTerm)
            );
        }
        this.totalRecords = this.filteredConversions.length;
        this.applyPagination();
    }

    private applyPagination(): void {
        const startIndex = (this.pageNumber - 1) * this.pageSize;
        this.pagedConversions = this.filteredConversions.slice(startIndex, startIndex + this.pageSize);
    }
}
