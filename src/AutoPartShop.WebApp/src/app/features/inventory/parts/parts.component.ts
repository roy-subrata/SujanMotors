import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { PartsHeaderComponent } from './parts-header/parts-header.component';
import { PartsTableComponent } from './parts-table/parts-table.component';
import { PartsFormDialogComponent } from './parts-form-dialog/parts-form-dialog.component';
import { BarcodeDialogComponent } from './barcode-dialog/barcode-dialog.component';
import { PartService, PartResponse } from '../services/part.service';

@Component({
    selector: 'app-parts',
    standalone: true,
    imports: [CommonModule, ToastModule, ConfirmDialogModule, PartsHeaderComponent, PartsTableComponent, PartsFormDialogComponent],
    providers: [MessageService, ConfirmationService, DialogService],
    templateUrl: './parts.component.html',
    styleUrls: ['./parts.component.css']
})
export class PartsComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly messageService = inject(MessageService);
    private readonly dialogService = inject(DialogService);

    parts: PartResponse[] = [];
    displayCreateDialog = false;
    displayUpdateDialog = false;
    selectedPart: PartResponse | null = null;
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
        this.partService.getParts(pageNumber, pageSize, searchTerm).subscribe({
            next: (response) => {
                this.parts = response.items;
                this.totalRecords = response.totalCount;
                this.rows = response.pageSize;
                this.currentPage = response.pageNumber;
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
        this.displayCreateDialog = true;
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
        this.selectedPart = part;
        this.displayUpdateDialog = true;
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
     * Handle part created
     */
    onPartCreated(part: PartResponse): void {
        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Part '${part.name}' created successfully`
        });
        this.loadParts(1, this.rows, this.searchTerm);
    }

    /**
     * Handle part updated
     */
    onPartUpdated(part: PartResponse): void {
        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Part '${part.name}' updated successfully`
        });
        this.loadParts(this.currentPage, this.rows, this.searchTerm);
    }

    /**
     * Handle part deleted
     */
    onPartDeleted(): void {
        this.loadParts(this.currentPage, this.rows, this.searchTerm);
    }
}
