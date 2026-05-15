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
    selectedConversions: UnitConversionResponse[] = [];
    loading = false;
    searchTerm = '';
    pageNumber = 1;
    pageSize = 10;
    totalRecords = 0;
    pageSizeOptions = [10, 20, 50];

    ngOnInit(): void {
        this.loadConversions();
    }

    /**
     * Load conversions with pagination and search
     */
    loadConversions(pageNum: number = 1): void {
        this.loading = true;
        this.pageNumber = pageNum;

        this.conversionService.getListConversions(this.pageNumber, this.pageSize, this.searchTerm).subscribe({
            next: (response) => {
                this.conversions = response.data;
                this.totalRecords = response.pagination?.totalCount || 0;
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
     * Handle pagination change
     */
    onPageChange(event: any): void {
        if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
            return;
        }
        const pageNum = Math.floor(event.first / event.rows) + 1;
        this.pageSize = event.rows;
        this.loadConversions(pageNum);
    }

    /**
     * Search conversions
     */
    search(query: string): void {
        this.searchTerm = query;
        this.pageNumber = 1;
        this.loadConversions(1);
    }

    /**
     * Clear search
     */
    clearSearch(): void {
        this.searchTerm = '';
        this.pageNumber = 1;
        this.loadConversions(1);
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
        this.loadConversions(1);
    }

    /**
     * Format conversion display
     */
    getConversionDisplay(conversion: UnitConversionResponse): string {
        return `1 ${conversion.fromUnitCode} = ${conversion.conversionFactor} ${conversion.toUnitCode}`;
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
}
