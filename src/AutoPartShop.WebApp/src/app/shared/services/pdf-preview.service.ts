import { Injectable, signal } from '@angular/core';

export interface PdfPreviewState {
  blobUrl: string;
  filename: string;
}

/**
 * Backs the single, app-wide PDF preview dialog (mounted once in AppComponent). Any PDF-producing
 * service or component calls `open()` with the fetched blob instead of saving it straight to disk,
 * so every "Download PDF" action in the app shows the document first.
 */
@Injectable({ providedIn: 'root' })
export class PdfPreviewService {
  private readonly _state = signal<PdfPreviewState | null>(null);
  readonly state = this._state.asReadonly();

  open(blob: Blob, filename: string): void {
    this.revokeCurrent();
    this._state.set({ blobUrl: URL.createObjectURL(blob), filename });
  }

  close(): void {
    this.revokeCurrent();
    this._state.set(null);
  }

  private revokeCurrent(): void {
    const current = this._state();
    if (current) {
      URL.revokeObjectURL(current.blobUrl);
    }
  }
}
