import { Routes } from '@angular/router';
import { EmployeesComponent } from './employees/employees.component';
import { EmployeesListComponent } from './employees/employees-list/employees-list.component';
import { EmployeeFormComponent } from './employees/employee-form/employee-form.component';
import { AttendanceDailyComponent } from './attendance/attendance-daily.component';
import { LeaveRequestsComponent } from './leave-requests/leave-requests.component';
import { HolidaysComponent } from './holidays/holidays.component';

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
    { path: 'holidays', component: HolidaysComponent }
];
