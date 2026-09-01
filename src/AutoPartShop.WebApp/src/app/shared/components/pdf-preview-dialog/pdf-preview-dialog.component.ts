import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer } from '@angular/platform-browser';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { PdfPreviewService } from '@/shared/services/pdf-preview.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

/**
 * App-wide PDF preview dialog. Mounted once in AppComponent and driven entirely by
 * PdfPreviewService, so any service or component can trigger it just by calling
 * PdfPreviewService.open(blob, filename) — no per-feature dialog wiring needed.
 */
@Component({
  selector: 'app-pdf-preview-dialog',
  standalone: true,
  imports: [CommonModule, DialogModule, ButtonModule, TranslatePipe],
  templateUrl: './pdf-preview-dialog.component.html',
  styleUrl: './pdf-preview-dialog.component.scss',
})
export class PdfPreviewDialogComponent {
  private readonly preview = inject(PdfPreviewService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly state = this.preview.state;

  get safeUrl() {
    const current = this.state();
    return current ? this.sanitizer.bypassSecurityTrustResourceUrl(current.blobUrl) : null;
  }

  download(): void {
    const current = this.state();
    if (!current) return;
    const a = document.createElement('a');
    a.href = current.blobUrl;
    a.download = current.filename;
    a.click();
  }

  close(): void {
    this.preview.close();
  }
}
