import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { environment } from 'src/environments/environment';

import { PageContainerComponent } from '../../../shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { PdfDownloadService } from '../../../shared/services/pdf-download.service';
import { WarehouseService } from '../../inventory/services/warehouse.service';
import { ReportQuery } from '../services/reports.service';

interface WarehouseOption {
    label: string;
    value: string;
}

/**
 * Bespoke Daily Sales (Z) Report page. There is no JSON preview endpoint for this report — it's
 * PDF-generation-only (api/v1/reports/sales/daily-z-report/pdf), composed server-side from
 * several sub-reports for a single business day. This page is just a filter form (business
 * date + optional warehouse) that triggers the branded PDF download via PdfDownloadService.
 */
@Component({
    selector: 'app-daily-z-report',
    standalone: true,
    imports: [
        CommonModule, FormsModule, DatePickerModule, SelectModule, ToastModule, TooltipModule,
        PageContainerComponent, PageHeaderComponent, HasPermissionDirective
    ],
    providers: [MessageService],
    templateUrl: './daily-z-report.component.html',
    styleUrls: ['./daily-z-report.component.scss']
})
export class DailyZReportComponent implements OnInit {
    private readonly messageService = inject(MessageService);
    private readonly pdfDownloadService = inject(PdfDownloadService);
    private readonly warehouseService = inject(WarehouseService);

    businessDate: Date = new Date();
    warehouseId: string | null = null;
    warehouseOptions: WarehouseOption[] = [];
    downloading = false;

    ngOnInit(): void {
        this.warehouseService.getAllWarehouses().subscribe({
            next: list => {
                this.warehouseOptions = list.map(w => ({ label: w.name, value: w.id }));
            },
            error: () => {
                // Non-fatal — the report simply runs across all warehouses if this fails.
            }
        });
    }

    downloadPdf(): void {
        if (this.downloading || !this.businessDate) return;
        this.downloading = true;

        const dayStr = formatDate(this.businessDate, 'yyyy-MM-dd', 'en-US');
        const query: ReportQuery = {
            fromDate: dayStr,
            toDate: dayStr,
            warehouseId: this.warehouseId ?? undefined
        };
        const filename = `daily-sales-report-${formatDate(this.businessDate, 'yyyyMMdd', 'en-US')}.pdf`;

        this.pdfDownloadService.downloadPost(
            `${environment.apiUrl}/v1/reports/sales/daily-z-report/pdf`,
            query,
            filename
        ).subscribe({
            next: () => { this.downloading = false; },
            error: () => {
                this.downloading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Download failed',
                    detail: 'Could not generate the Daily Sales (Z) Report PDF. Please try again.'
                });
            }
        });
    }
}
