import { Routes } from '@angular/router';
import { ReportsHubComponent } from './reports-hub/reports-hub.component';
import { ReportPageComponent } from './report-page/report-page.component';
import { ProfitLossReportComponent } from './profit-loss/profit-loss-report.component';
import { VatReportComponent } from './vat-report/vat-report.component';
import { DailyZReportComponent } from './daily-z-report/daily-z-report.component';

/**
 * /reports — hub with category cards; /reports/profit-loss, /reports/vat, /reports/daily-z-report
 * — bespoke report pages; /reports/:reportKey — config-driven report page (unknown keys redirect
 * back to the hub inside ReportPageComponent). Bespoke keys must stay listed before the
 * ':reportKey' wildcard so they win the route match. The whole module is gated by reports.view
 * in app.routes.ts.
 */
export const reportsRoutes: Routes = [
    { path: '', component: ReportsHubComponent },
    { path: 'profit-loss', component: ProfitLossReportComponent },
    { path: 'vat', component: VatReportComponent },
    { path: 'daily-z-report', component: DailyZReportComponent },
    { path: ':reportKey', component: ReportPageComponent }
];
