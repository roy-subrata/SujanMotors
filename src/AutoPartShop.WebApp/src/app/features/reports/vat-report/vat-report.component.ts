import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { environment } from 'src/environments/environment';

import { PageContainerComponent } from '../../../shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { extractApiError } from '../../../shared/utils/api-error.util';
import { PdfDownloadService } from '../../../shared/services/pdf-download.service';
import { ReportQuery } from '../services/reports.service';

/** Backend VatReportDto (see Application/DTOs/ReportDtos/FinancialReportDtos.cs). */
interface VatReportResponse {
    salesTaxableValue: number;
    salesVatAmount: number;
    salesInvoiceCount: number;
    creditTaxableValue: number;
    creditVatAmount: number;
    purchaseTaxableValue: number;
    purchaseVatAmount: number;
    purchaseOrderCount: number;
    netVatPayable: number;
}

/**
 * Bespoke VAT reconciliation report page. Renders api/v1/reports/financial/vat as an
 * output-vs-input VAT reconciliation, and offers the branded handoff PDF from
 * api/v1/reports/financial/vat/pdf via PdfDownloadService (not the generic tabular export).
 */
@Component({
    selector: 'app-vat-report',
    standalone: true,
    imports: [
        CommonModule, FormsModule, DatePickerModule, InputNumberModule, ToastModule, TooltipModule,
        PageContainerComponent, PageHeaderComponent, HasPermissionDirective
    ],
    providers: [MessageService],
    templateUrl: './vat-report.component.html',
    styleUrls: ['./vat-report.component.scss']
})
export class VatReportComponent implements OnInit {
    private readonly http = inject(HttpClient);
    private readonly messageService = inject(MessageService);
    private readonly pdfDownloadService = inject(PdfDownloadService);

    dateRange: Date[] | null = null;
    vatRatePercent = 15;
    report: VatReportResponse | null = null;
    loading = false;
    downloading = false;

    ngOnInit(): void {
        const today = new Date();
        this.dateRange = [new Date(today.getFullYear(), today.getMonth(), 1), today];
        this.load();
    }

    onDateRangeSelect(): void {
        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            this.load();
        }
    }

    load(): void {
        if (!this.dateRange || this.dateRange.length < 2 || !this.dateRange[0] || !this.dateRange[1]) return;

        this.loading = true;
        this.http.post<VatReportResponse>(
            `${environment.apiUrl}/v1/reports/financial/vat`,
            this.buildQuery()
        ).subscribe({
            next: report => {
                this.report = report;
                this.loading = false;
            },
            error: err => {
                this.loading = false;
                this.report = null;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Report failed',
                    detail: extractApiError(err, 'Could not load the VAT report.')
                });
            }
        });
    }

    downloadPdf(): void {
        if (this.downloading || !this.dateRange || !this.dateRange[0] || !this.dateRange[1]) return;
        this.downloading = true;

        const query = this.buildQuery();
        const filename = `vat-report-${formatDate(this.dateRange![0], 'yyyyMMdd', 'en-US')}-${formatDate(this.dateRange![1], 'yyyyMMdd', 'en-US')}.pdf`;

        this.pdfDownloadService.downloadPost(
            `${environment.apiUrl}/v1/reports/financial/vat/pdf`,
            query,
            filename
        ).subscribe({
            next: () => { this.downloading = false; },
            error: () => {
                this.downloading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Download failed',
                    detail: 'Could not generate the VAT report PDF. Please try again.'
                });
            }
        });
    }

    private buildQuery(): ReportQuery {
        return {
            fromDate: formatDate(this.dateRange![0], 'yyyy-MM-dd', 'en-US'),
            toDate: formatDate(this.dateRange![1], 'yyyy-MM-dd', 'en-US'),
            vatRatePercent: this.vatRatePercent
        };
    }
}
