import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DatePickerModule } from 'primeng/datepicker';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { environment } from 'src/environments/environment';

import { PageContainerComponent } from '../../../shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { extractApiError } from '../../../shared/utils/api-error.util';
import { REPORT_REGISTRY } from '../report-configs';
import { ReportExportFormat, ReportQuery, ReportsService } from '../services/reports.service';

/** Backend FinancialSummaryResponse (see Application/DTOs/DashboardDtos/FinancialSummaryDto.cs). */
interface FinancialSummaryResponse {
    totalSales: number;
    cashSales: number;
    creditSales: number;
    customerPaymentsReceived: number;
    totalPurchases: number;
    supplierPaymentsMade: number;
    dailyExpenses: number;
    totalExpenses: number;
    grossProfit: number;
    netProfit: number;
    profitMargin: number;
    customerDueAmount: number;
    customerOverdueAmount: number;
    supplierDueAmount: number;
    supplierOverdueAmount: number;
    inventoryValue: number;
    cashInflow: number;
    cashOutflow: number;
    closingBalance: number;
}

/**
 * Bespoke Profit & Loss statement page. Reuses IFinancialSummaryService's figures via
 * api/v1/reports/financial/profit-loss (gated by reports.view/export, unlike the plain
 * dashboard endpoint) so the numbers here always match the main dashboard.
 */
@Component({
    selector: 'app-profit-loss-report',
    standalone: true,
    imports: [
        CommonModule, FormsModule, DatePickerModule, ToastModule, TooltipModule,
        PageContainerComponent, PageHeaderComponent, HasPermissionDirective
    ],
    providers: [MessageService],
    templateUrl: './profit-loss-report.component.html',
    styleUrls: ['./profit-loss-report.component.scss']
})
export class ProfitLossReportComponent implements OnInit {
    private readonly http = inject(HttpClient);
    private readonly reportsService = inject(ReportsService);
    private readonly messageService = inject(MessageService);

    dateRange: Date[] | null = null;
    summary: FinancialSummaryResponse | null = null;
    loading = false;
    exporting: ReportExportFormat | null = null;

    private readonly config = REPORT_REGISTRY.get('profit-loss')!;

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
        this.http.post<FinancialSummaryResponse>(
            `${environment.apiUrl}/v1/reports/financial/profit-loss`,
            this.buildQuery()
        ).subscribe({
            next: summary => {
                this.summary = summary;
                this.loading = false;
            },
            error: err => {
                this.loading = false;
                this.summary = null;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Report failed',
                    detail: extractApiError(err, 'Could not load the profit & loss statement.')
                });
            }
        });
    }

    export(format: ReportExportFormat): void {
        if (this.exporting) return;
        this.exporting = format;

        this.reportsService.export(this.config, this.buildQuery(), format).subscribe({
            next: blob => {
                const url = URL.createObjectURL(blob);
                const anchor = document.createElement('a');
                anchor.href = url;
                anchor.download = `profit-loss-${formatDate(new Date(), 'yyyyMMdd', 'en-US')}.${format}`;
                anchor.click();
                URL.revokeObjectURL(url);
                this.exporting = null;
            },
            error: () => {
                this.exporting = null;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Export failed',
                    detail: 'Could not export the statement. Please try again.'
                });
            }
        });
    }

    private buildQuery(): ReportQuery {
        return {
            fromDate: formatDate(this.dateRange![0], 'yyyy-MM-dd', 'en-US'),
            toDate: formatDate(this.dateRange![1], 'yyyy-MM-dd', 'en-US')
        };
    }
}
