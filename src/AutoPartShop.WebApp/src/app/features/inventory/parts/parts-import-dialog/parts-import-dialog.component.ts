import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import {
    PartImportService,
    ProductImportValidationResult,
    ProductImportCommitResult,
    ProductImportMode
} from '../../services/part-import.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

type ImportStep = 'upload' | 'review' | 'done';

@Component({
    selector: 'app-parts-import-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, DialogModule, ButtonModule, TableModule, TagModule, ProgressSpinnerModule, TooltipModule, TranslatePipe],
    templateUrl: './parts-import-dialog.component.html',
    styleUrls: ['./parts-import-dialog.component.css']
})
export class PartsImportDialogComponent {
    private readonly importService = inject(PartImportService);
    private readonly i18n = inject(I18nService);

    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    /** Emits the number of parts created or updated so the parent can refresh and notify. */
    @Output() imported = new EventEmitter<number>();

    step: ImportStep = 'upload';
    selectedFile: File | null = null;

    /** Create-only by default — updating existing parts has to be chosen deliberately. */
    mode: ProductImportMode = 'CreateOnly';

    downloading = false;
    exporting = false;
    validating = false;
    committing = false;

    validation: ProductImportValidationResult | null = null;
    commitResult: ProductImportCommitResult | null = null;
    errorMessage: string | null = null;

    get canImport(): boolean {
        return !!this.validation && this.validation.validCount > 0 && !this.committing;
    }

    /** True when the batch would auto-create master data the user should eyeball first. */
    get hasNewReferenceData(): boolean {
        const v = this.validation;
        return !!v && (v.newBrands.length > 0 || v.newCategories.length > 0 || v.newUnits.length > 0);
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
        this.mode = 'CreateOnly';
        this.validation = null;
        this.commitResult = null;
        this.errorMessage = null;
        this.downloading = false;
        this.exporting = false;
        this.validating = false;
        this.committing = false;
    }

    /** Switching mode invalidates the report it produced. */
    onModeChange(): void {
        this.validation = null;
    }

    downloadTemplate(): void {
        this.downloading = true;
        this.importService.downloadTemplate().subscribe({
            next: (blob) => {
                this.importService.saveBlob(blob, 'product-import-template.xlsx');
                this.downloading = false;
            },
            error: () => {
                this.errorMessage = this.i18n.t('parts.importDialog.messages.downloadTemplateFailed');
                this.downloading = false;
            }
        });
    }

    /** Current catalog in the import layout — the starting point for a bulk update. */
    downloadExport(): void {
        this.exporting = true;
        this.errorMessage = null;
        this.importService.downloadExport().subscribe({
            next: (blob) => {
                const stamp = new Date().toISOString().slice(0, 10);
                this.importService.saveBlob(blob, `parts-export-${stamp}.xlsx`);
                this.exporting = false;
                // Editing exported rows is an update — pre-select the mode that allows it.
                this.mode = 'CreateAndUpdate';
                this.onModeChange();
            },
            error: () => {
                this.errorMessage = this.i18n.t('parts.importDialog.messages.exportFailed');
                this.exporting = false;
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
            this.errorMessage = this.i18n.t('parts.importDialog.messages.chooseFileFirst');
            return;
        }
        this.validating = true;
        this.errorMessage = null;
        this.importService.validate(this.selectedFile, this.mode).subscribe({
            next: (result) => {
                this.validation = result;
                this.step = 'review';
                this.validating = false;
            },
            error: (err) => {
                this.errorMessage = err?.error?.detail || this.i18n.t('parts.importDialog.messages.validateFailed');
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
        this.importService.commit(rows, this.mode).subscribe({
            next: (result) => {
                this.commitResult = result;
                this.step = 'done';
                this.committing = false;
                this.imported.emit(result.createdCount + result.updatedCount);
            },
            error: (err) => {
                this.errorMessage = err?.error?.detail || this.i18n.t('parts.importDialog.messages.commitFailed');
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
