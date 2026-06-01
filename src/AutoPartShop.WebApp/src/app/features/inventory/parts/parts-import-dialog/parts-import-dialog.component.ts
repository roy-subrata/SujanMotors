import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import {
    PartImportService,
    ProductImportValidationResult,
    ProductImportCommitResult
} from '../../services/part-import.service';

type ImportStep = 'upload' | 'review' | 'done';

@Component({
    selector: 'app-parts-import-dialog',
    standalone: true,
    imports: [CommonModule, DialogModule, ButtonModule, TableModule, TagModule, ProgressSpinnerModule],
    templateUrl: './parts-import-dialog.component.html',
    styleUrls: ['./parts-import-dialog.component.css']
})
export class PartsImportDialogComponent {
    private readonly importService = inject(PartImportService);

    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    /** Emits the number of parts created so the parent can refresh and notify. */
    @Output() imported = new EventEmitter<number>();

    step: ImportStep = 'upload';
    selectedFile: File | null = null;

    downloading = false;
    validating = false;
    committing = false;

    validation: ProductImportValidationResult | null = null;
    commitResult: ProductImportCommitResult | null = null;
    errorMessage: string | null = null;

    get canImport(): boolean {
        return !!this.validation && this.validation.validCount > 0 && !this.committing;
    }

    onShow(): void {
        this.reset();
    }

    onHide(): void {
        this.visible = false;
        this.visibleChange.emit(false);
        this.reset();
    }

    close(): void {
        this.onHide();
    }

    private reset(): void {
        this.step = 'upload';
        this.selectedFile = null;
        this.validation = null;
        this.commitResult = null;
        this.errorMessage = null;
        this.downloading = false;
        this.validating = false;
        this.committing = false;
    }

    downloadTemplate(): void {
        this.downloading = true;
        this.importService.downloadTemplate().subscribe({
            next: (blob) => {
                this.importService.saveBlob(blob, 'product-import-template.xlsx');
                this.downloading = false;
            },
            error: () => {
                this.errorMessage = 'Failed to download the template. Please try again.';
                this.downloading = false;
            }
        });
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        this.errorMessage = null;
        this.selectedFile = input.files && input.files.length > 0 ? input.files[0] : null;
    }

    runValidate(): void {
        if (!this.selectedFile) {
            this.errorMessage = 'Please choose an .xlsx file first.';
            return;
        }
        this.validating = true;
        this.errorMessage = null;
        this.importService.validate(this.selectedFile).subscribe({
            next: (result) => {
                this.validation = result;
                this.step = 'review';
                this.validating = false;
            },
            error: (err) => {
                this.errorMessage = err?.error?.detail || 'The file could not be validated. Make sure it matches the template.';
                this.validating = false;
            }
        });
    }

    runCommit(): void {
        if (!this.validation) return;
        const rows = this.validation.rows
            .filter(r => r.isValid && r.row)
            .map(r => r.row!);
        if (rows.length === 0) return;

        this.committing = true;
        this.errorMessage = null;
        this.importService.commit(rows).subscribe({
            next: (result) => {
                this.commitResult = result;
                this.step = 'done';
                this.committing = false;
                this.imported.emit(result.createdCount);
            },
            error: (err) => {
                this.errorMessage = err?.error?.detail || 'The import failed. Please try again.';
                this.committing = false;
            }
        });
    }

    backToUpload(): void {
        this.step = 'upload';
        this.validation = null;
        this.errorMessage = null;
    }
}
