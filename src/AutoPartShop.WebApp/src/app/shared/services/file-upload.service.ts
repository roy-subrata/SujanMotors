import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

/** Mirrors the API StoredFileDto. */
export interface StoredFile {
    id: string;
    /** Relative URL (e.g. /api/v1/files/{id}/content) — resolve with resolveFileUrl() for display. */
    url: string;
    fileName: string;
    contentType: string;
    sizeBytes: number;
    kind: 'IMAGE' | 'VIDEO' | 'DOCUMENT';
    isPublic: boolean;
    ownerType: string;
    ownerId: string | null;
    createdAt: string;
}

/** Client-side mirror of the server's upload limits, for instant feedback before the round-trip. */
export const UPLOAD_LIMITS = {
    image: { maxBytes: 5 * 1024 * 1024, accept: '.jpg,.jpeg,.png,.gif,.webp', label: '5 MB' },
    video: { maxBytes: 100 * 1024 * 1024, accept: '.mp4,.mov,.webm', label: '100 MB' },
    document: { maxBytes: 10 * 1024 * 1024, accept: '.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt', label: '10 MB' }
} as const;

/**
 * Turns the relative file URLs stored in the database into absolute ones for
 * <img>/<video> tags. Stored URLs stay relative so they survive domain changes;
 * only display resolves against the API origin.
 */
export function resolveFileUrl(url: string | null | undefined): string {
    if (!url) return '';
    if (/^https?:\/\//i.test(url)) return url; // external URL — leave as is
    const apiOrigin = environment.apiUrl.replace(/\/api\/?$/, '');
    return `${apiOrigin}${url.startsWith('/') ? '' : '/'}${url}`;
}

/**
 * Uploads binaries to the API file store (POST /api/v1/files).
 * Images/videos come back with public URLs usable directly in media tags;
 * documents are auth-protected and must be fetched as blobs.
 */
@Injectable({ providedIn: 'root' })
export class FileUploadService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/files`;

    /** Upload a file, optionally tagged with the owning record (e.g. PRODUCT / EMPLOYEE + id). */
    upload(file: File, ownerType?: string, ownerId?: string): Observable<StoredFile> {
        const form = new FormData();
        form.append('file', file);
        if (ownerType) form.append('ownerType', ownerType);
        if (ownerId) form.append('ownerId', ownerId);
        return this.http.post<{ data: StoredFile }>(this.apiUrl, form).pipe(map((r) => r.data));
    }

    /** Files attached to a record (e.g. an employee's documents). */
    getByOwner(ownerType: string, ownerId: string): Observable<StoredFile[]> {
        return this.http.get<{ data: StoredFile[] }>(this.apiUrl, { params: { ownerType, ownerId } }).pipe(map((r) => r.data));
    }

    /** Admin/Manager only: removes the record and the stored blob. */
    delete(fileId: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${fileId}`);
    }

    /** Download a protected document (JWT attached by the interceptor) as a blob. */
    downloadContent(fileId: string): Observable<Blob> {
        return this.http.get(`${this.apiUrl}/${fileId}/content`, { responseType: 'blob' });
    }
}
