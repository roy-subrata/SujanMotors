import { Routes } from '@angular/router';
import { ReportsHubComponent } from './reports-hub/reports-hub.component';
import { ReportPageComponent } from './report-page/report-page.component';
import { ProfitLossReportComponent } from './profit-loss/profit-loss-report.component';

/**
 * /reports — hub with category cards; /reports/profit-loss — bespoke P&L statement page;
 * /reports/:reportKey — config-driven report page (unknown keys redirect back to the hub
 * inside ReportPageComponent). 'profit-loss' must stay listed before the ':reportKey'
 * wildcard so it wins the route match. The whole module is gated by reports.view in app.routes.ts.
 */
export const reportsRoutes: Routes = [
    { path: '', component: ReportsHubComponent },
    { path: 'profit-loss', component: ProfitLossReportComponent },
    { path: ':reportKey', component: ReportPageComponent }
];
