import { Routes } from '@angular/router';
import { AuditDashboardComponent } from './audit-dashboard/audit-dashboard.component';
import { AuditLogsComponent } from './audit-logs/audit-logs.component';

export const auditRoutes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: AuditDashboardComponent },
  { path: 'logs', component: AuditLogsComponent }
];
