import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { PdfPreviewService } from './pdf-preview.service';

/**
 * Fetches a server-rendered PDF from any endpoint that returns an `application/pdf` blob and
 * shows it in the app-wide preview dialog (see PdfPreviewService / PdfPreviewDialogComponent),
 * where the user can view it and download it from there.
 *
 * Extracted from InvoicePdfService.downloadServerPdf() — that was the only blob-fetch
 * implementation in the app before this, and it was a one-off method rather than a shared
 * utility. Every "Download PDF" action (Sales Order, Purchase Order, Credit Note, Quotation,
 * Proforma, Debit Note, VAT/Z reports, Shift Report, ...) should use this instead of
 * reimplementing the blob-fetch sequence.
 */
@Injectable({ providedIn: 'root' })
export class PdfDownloadService {
  private readonly http = inject(HttpClient);
  private readonly preview = inject(PdfPreviewService);

  /**
   * GET a PDF and show it in the preview dialog as `filename`. Use for endpoints keyed by a
   * resource id (`GET .../{id}/pdf`) that need no request body.
   */
  previewGet(url: string, filename: string, params?: HttpParams): Observable<void> {
    return this.http.get(url, { params, responseType: 'blob' }).pipe(
      map(blob => this.preview.open(blob, filename))
    );
  }

  /**
   * POST a filter/query body and show the returned PDF in the preview dialog as `filename`. Use
   * for report endpoints that take a ReportQuery (date range, warehouse, etc.) rather than a
   * single resource id.
   */
  previewPost(url: string, body: unknown, filename: string): Observable<void> {
    return this.http.post(url, body, { responseType: 'blob' }).pipe(
      map(blob => this.preview.open(blob, filename))
    );
  }
}
