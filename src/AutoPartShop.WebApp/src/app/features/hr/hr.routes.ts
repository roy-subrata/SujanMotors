import { Routes } from '@angular/router';
import { EmployeesComponent } from './employees/employees.component';
import { EmployeesListComponent } from './employees/employees-list/employees-list.component';
import { EmployeeFormComponent } from './employees/employee-form/employee-form.component';
import { AttendanceDailyComponent } from './attendance/attendance-daily.component';
import { LeaveRequestsComponent } from './leave-requests/leave-requests.component';
import { HolidaysComponent } from './holidays/holidays.component';
import { PayrollRunsComponent } from './payroll/payroll-runs/payroll-runs.component';
import { PayrollRunDetailComponent } from './payroll/payroll-run-detail/payroll-run-detail.component';
import { ShiftsComponent } from './shifts/shifts.component';
import { SalaryAdvancesComponent } from './advances/salary-advances.component';

export const hrRoutes: Routes = [
    { path: '', redirectTo: 'employees', pathMatch: 'full' },

    // Employees
    {
        path: 'employees',
        component: EmployeesComponent,
        children: [
            { path: '', component: EmployeesListComponent },
            { path: 'create', component: EmployeeFormComponent },
            { path: 'edit', component: EmployeeFormComponent },
            { path: 'view', component: EmployeeFormComponent }
        ]
    },

    // Attendance
    { path: 'attendance', component: AttendanceDailyComponent },

    // Leave Requests
    { path: 'leave-requests', component: LeaveRequestsComponent },

    // Holidays
    { path: 'holidays', component: HolidaysComponent },

    // Payroll
    { path: 'payroll', component: PayrollRunsComponent },
    { path: 'payroll/view', component: PayrollRunDetailComponent },

    // Shifts
    { path: 'shifts', component: ShiftsComponent },

    // Salary Advances
    { path: 'advances', component: SalaryAdvancesComponent }
];
