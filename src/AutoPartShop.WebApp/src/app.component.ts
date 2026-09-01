import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PdfPreviewDialogComponent } from '@/shared/components/pdf-preview-dialog/pdf-preview-dialog.component';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule, ConfirmDialogModule, PdfPreviewDialogComponent],
    // The root confirm dialog is driven by the root ConfirmationService — used by the auth
    // interceptor to offer a reload when the API returns a 409 concurrency conflict. The custom
    // key keeps it from colliding with page-level <p-confirmDialog> instances.
    // The PDF preview dialog is driven by PdfPreviewService, so any PDF-producing service or
    // component can show it just by calling PdfPreviewService.open(blob, filename).
    templateUrl: './app.component.html',
})
export class AppComponent {}
