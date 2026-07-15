import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

// ── Models ────────────────────────────────────────────────────────────────────

export interface BackupRecord {
  id: string;
  fileName: string;
  sizeBytes: number;
  status: 'Pending' | 'Running' | 'Succeeded' | 'UploadFailed' | 'Failed';
  triggerType: 'Manual' | 'Scheduled' | 'PreRestore';
  uploadedToDrive: boolean;
  localFileExists: boolean;
  startedAt: string;
  completedAt: string | null;
  errorMessage: string | null;
  createdBy: string;
}

export interface BackupPaginationMeta {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PagedBackupResponse {
  data: BackupRecord[];
  pagination: BackupPaginationMeta;
}

export interface DriveStatus {
  configured: boolean;
  ok: boolean;
  serviceAccountEmail: string | null;
  error: string | null;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class BackupService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/backups`;

  /** Paged backup history, newest first. */
  getHistory(page: number, pageSize: number): Observable<PagedBackupResponse> {
    const params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    return this.http.get<PagedBackupResponse>(this.apiUrl, { params });
  }

  /** Trigger a manual backup. Returns 202 with the new record id; poll history for completion. */
  runBackup(): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.apiUrl}/run`, {});
  }

  /** Restore the database from a backup. confirmation must be the literal string "RESTORE". */
  restore(id: string, confirmation: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/restore`, { confirmation });
  }

  /** Download the .bak file as a blob (server re-fetches from Google Drive if needed). */
  download(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/download`, { responseType: 'blob' });
  }

  /** Validate the Google Drive service account + configured folder. */
  getDriveStatus(): Observable<DriveStatus> {
    return this.http.get<DriveStatus>(`${this.apiUrl}/drive-status`);
  }

  /** Currently running operation ("backup" / "restore") or null when idle. */
  getStatus(): Observable<{ currentOperation: string | null }> {
    return this.http.get<{ currentOperation: string | null }>(`${this.apiUrl}/status`);
  }
}
