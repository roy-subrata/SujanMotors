import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

/** A single parsed/edited row destined for import. Mirrors the API ProductImportRow. */
export interface ProductImportRow {
    rowNumber: number;
    name?: string | null;
    partNumber?: string | null;
    category?: string | null;
    brand?: string | null;
    unit?: string | null;
    costPrice?: number | null;
    sellingPrice?: number | null;
    minimumStock?: number | null;
    barcode?: string | null;
    oemNumber?: string | null;
    tags?: string | null;
    description?: string | null;
    productType?: string | null;
    taxCode?: string | null;
    hasWarranty?: boolean | null;
    warrantyPeriodMonths?: number | null;
    warrantyType?: string | null;
    weightKg?: number | null;
    widthCm?: number | null;
    heightCm?: number | null;
    depthCm?: number | null;
}

export interface ProductImportRowResult {
    rowNumber: number;
    name?: string | null;
    partNumber?: string | null;
    isValid: boolean;
    errors: string[];
    row?: ProductImportRow | null;
}

export interface ProductImportValidationResult {
    totalRows: number;
    validCount: number;
    errorCount: number;
    rows: ProductImportRowResult[];
}

export interface ProductImportCommitResult {
    createdCount: number;
    failedCount: number;
    failures: ProductImportRowResult[];
}

@Injectable({ providedIn: 'root' })
export class PartImportService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/products/import`;

    /** Download the .xlsx template as a blob. */
    downloadTemplate(): Observable<Blob> {
        return this.http.get(`${this.apiUrl}/template`, { responseType: 'blob' });
    }

    /** Upload a filled workbook for a dry-run validation report. */
    validate(file: File): Observable<ProductImportValidationResult> {
        const form = new FormData();
        form.append('file', file);
        return this.http.post<{ data: ProductImportValidationResult }>(`${this.apiUrl}/validate`, form)
            .pipe(map(r => r.data));
    }

    /** Commit the confirmed rows. */
    commit(rows: ProductImportRow[]): Observable<ProductImportCommitResult> {
        return this.http.post<{ data: ProductImportCommitResult }>(`${this.apiUrl}/commit`, { rows })
            .pipe(map(r => r.data));
    }

    /** Helper to trigger a browser download for a blob. */
    saveBlob(blob: Blob, fileName: string): void {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();
        window.URL.revokeObjectURL(url);
    }
}
