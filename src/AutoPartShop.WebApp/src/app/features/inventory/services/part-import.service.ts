import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

/**
 * How a run may affect existing parts.
 * - CreateOnly: only new parts; a row carrying a SKU is rejected (the safe default).
 * - CreateAndUpdate: a row's SKU updates that part; blank SKU still creates.
 */
export type ProductImportMode = 'CreateOnly' | 'CreateAndUpdate';

/** What the import will do with a row. */
export type ProductImportAction = 'Create' | 'Update';

/** A single parsed/edited row destined for import. Mirrors the API ProductImportRow. */
export interface ProductImportRow {
    rowNumber: number;
    sku?: string | null;            // import key: blank = create, filled = update that part
    name?: string | null;
    localName?: string | null;      // local-language name (e.g. Bengali) shown to staff
    partNumber?: string | null;
    category?: string | null;       // supports "Parent > Child > GrandChild" hierarchy
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
    variantName?: string | null;
    variantCode?: string | null;
    variantPartNumber?: string | null;
    variantOemNumber?: string | null;
    variantBarcode?: string | null;
    variantCostPrice?: number | null;
    variantSellingPrice?: number | null;
}

export interface ProductImportRowResult {
    rowNumber: number;
    name?: string | null;
    partNumber?: string | null;
    sku?: string | null;
    action: ProductImportAction;
    isValid: boolean;
    errors: string[];
    row?: ProductImportRow | null;
}

export interface ProductImportValidationResult {
    totalRows: number;
    validCount: number;
    errorCount: number;
    createCount: number;            // distinct parts that would be created
    updateCount: number;            // distinct parts that would be updated
    newBrands: string[];            // master data that would be auto-created
    newCategories: string[];
    newUnits: string[];
    rows: ProductImportRowResult[];
}

export interface ProductImportCommitResult {
    createdCount: number;
    updatedCount: number;
    failedCount: number;
    createdBrandsCount: number;
    createdCategoriesCount: number;
    createdUnitsCount: number;
    createdVariantsCount: number;
    updatedVariantsCount: number;
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

    /**
     * Download the current catalog in the import layout, SKUs filled in.
     * Edit and re-upload in create-and-update mode to apply changes in bulk.
     */
    downloadExport(): Observable<Blob> {
        return this.http.get(`${this.apiUrl}/export`, { responseType: 'blob' });
    }

    /**
     * Upload a filled workbook for a dry-run validation report.
     *
     * `allowNewReferenceData` is off by default: brand/category/unit names that do not already
     * exist come back as row errors rather than being created on the fly, so a spreadsheet typo
     * cannot silently become permanent master data.
     */
    validate(file: File, mode: ProductImportMode = 'CreateOnly', allowNewReferenceData = false): Observable<ProductImportValidationResult> {
        const form = new FormData();
        form.append('file', file);
        return this.http.post<{ data: ProductImportValidationResult }>(
            `${this.apiUrl}/validate?mode=${mode}&allowNewReferenceData=${allowNewReferenceData}`, form)
            .pipe(map(r => r.data));
    }

    /** Commit the confirmed rows. Mode and allowNewReferenceData must match the validate call. */
    commit(rows: ProductImportRow[], mode: ProductImportMode = 'CreateOnly', allowNewReferenceData = false): Observable<ProductImportCommitResult> {
        return this.http.post<{ data: ProductImportCommitResult }>(
            `${this.apiUrl}/commit`, { rows, mode, allowNewReferenceData })
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
