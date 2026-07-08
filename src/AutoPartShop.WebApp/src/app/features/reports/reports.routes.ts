import { Routes } from '@angular/router';
import { ReportsHubComponent } from './reports-hub/reports-hub.component';
import { ReportPageComponent } from './report-page/report-page.component';

/**
 * /reports — hub with category cards; /reports/:reportKey — config-driven report page
 * (unknown keys redirect back to the hub inside ReportPageComponent).
 * The whole module is gated by reports.view in app.routes.ts.
 */
export const reportsRoutes: Routes = [
    { path: '', component: ReportsHubComponent },
    { path: ':reportKey', component: ReportPageComponent }
];
