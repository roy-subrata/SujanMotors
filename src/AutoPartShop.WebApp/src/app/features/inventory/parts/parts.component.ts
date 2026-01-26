import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { PartsHeaderComponent } from './parts-header/parts-header.component';
import { PartsTableComponent } from './parts-table/parts-table.component';
import { BarcodeDialogComponent } from './barcode-dialog/barcode-dialog.component';
import { PartService, PartResponse } from '../services/part.service';

@Component({
    selector: 'app-parts',
    standalone: true,
    imports: [CommonModule, ToastModule, ConfirmDialogModule, PartsHeaderComponent, PartsTableComponent],
    providers: [MessageService, ConfirmationService, DialogService],
    templateUrl: './parts.component.html',
    styleUrls: ['./parts.component.css']
})
export class PartsComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly messageService = inject(MessageService);
    private readonly dialogService = inject(DialogService);
    private readonly router = inject(Router);

    parts: PartResponse[] = [];
    loading = false;
    totalRecords = 0;
    rows = 10;
    currentPage = 1;
    searchTerm = '';

    constructor() {}

    ngOnInit(): void {
        this.loadParts();
    }

    /**
     * Load parts from API
     */
    loadParts(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): void {
        // Validate page number
        if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
            pageNumber = 1;
        }
        if (!pageSize || isNaN(pageSize) || pageSize < 1) {
            pageSize = 10;
        }

        this.loading = true;
        this.partService.getParts({
            search: searchTerm,
            pageNumber: pageNumber,
            pageSize: pageNumber,
            isActive: true
        }).subscribe({
            next: (response) => {
                this.parts = response.data;
                this.totalRecords = response.pagination.totalCount;
                this.rows = response.pagination.pageSize;
                this.currentPage = response.pagination.totalPages;
                this.loading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load parts'
                });
                console.error('Error loading parts:', error);
                this.loading = false;
            }
        });
    }

    /**
     * Handle create button click
     */
    onCreateClick(): void {
        this.router.navigate(['/inventory/parts/create']);
    }

    /**
     * Handle search
     */
    onSearch(query: string): void {
        this.searchTerm = query;
        this.loadParts(1, this.rows, query);
    }

    /**
     * Handle search clear
     */
    onSearchClear(): void {
        this.searchTerm = '';
        this.loadParts(1, this.rows);
    }

    /**
     * Handle edit click
     */
    onEditClick(part: PartResponse): void {
        this.router.navigate(['/inventory/parts/edit'], {
            queryParams: { id: part.id, mode: 'edit' }
        });
    }

    /**
     * Handle show barcode click
     */
    onShowBarcodeClick(part: PartResponse): void {
        const dialogRef = this.dialogService.open(BarcodeDialogComponent, {
          data: { part: part },
            header: 'Part Barcode Generator',
            width: '600px',
            modal: true,
            closable: true
        });

        // Pass the part data to the dialog
        // dialogRef.componentInstance.part = part;
    }

    /**
     * Handle page change
     */
    onPageChange(event: { page: number; rows: number }): void {
        this.loadParts(event.page, event.rows, this.searchTerm);
    }

    /**
     * Handle part deleted
     */
    onPartDeleted(): void {
        this.loadParts(this.currentPage, this.rows, this.searchTerm);
    }
}
