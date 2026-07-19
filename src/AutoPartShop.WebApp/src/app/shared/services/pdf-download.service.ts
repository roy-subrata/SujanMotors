import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';

/**
 * Downloads a server-rendered PDF from any endpoint that returns a `application/pdf` blob, and
 * triggers the browser's save dialog for it.
 *
 * Extracted from InvoicePdfService.downloadServerPdf() — that was the only blob-download
 * implementation in the app before this, and it was a one-off method rather than a shared
 * utility. Every new "Download PDF" button (Sales Order, Purchase Order, Credit Note, Quotation,
 * Proforma, Debit Note, VAT/Z reports, Shift Report, ...) should use this instead of
 * reimplementing the blob -> object URL -> anchor-click -> revoke sequence.
 */
@Injectable({ providedIn: 'root' })
export class PdfDownloadService {
  private readonly http = inject(HttpClient);

  /**
   * GET a PDF and save it as `filename`. Use for endpoints keyed by a resource id
   * (`GET .../{id}/pdf`) that need no request body.
   */
  downloadGet(url: string, filename: string, params?: HttpParams): Observable<void> {
    return this.http.get(url, { params, responseType: 'blob' }).pipe(
      map(blob => this.saveBlob(blob, filename))
    );
  }

  /**
   * POST a filter/query body and save the returned PDF as `filename`. Use for report endpoints
   * that take a ReportQuery (date range, warehouse, etc.) rather than a single resource id.
   */
  downloadPost(url: string, body: unknown, filename: string): Observable<void> {
    return this.http.post(url, body, { responseType: 'blob' }).pipe(
      map(blob => this.saveBlob(blob, filename))
    );
  }

  private saveBlob(blob: Blob, filename: string): void {
    const objectUrl = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = objectUrl;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(objectUrl);
  }
}
