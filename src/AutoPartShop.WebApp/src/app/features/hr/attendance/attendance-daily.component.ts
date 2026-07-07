import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttendanceService, DailyAttendanceRow, MarkAttendanceEntry } from '../services/attendance.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';

interface AttendanceRowVm extends DailyAttendanceRow {
    checkIn: string;   // "HH:mm" for <input type="time">
    checkOut: string;
    dirty: boolean;
}

@Component({
    selector: 'app-attendance-daily',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, Select, DatePickerModule, TooltipModule, ToastModule,
        PageContainerComponent, PageHeaderComponent, FilterBarComponent],
    providers: [MessageService],
    templateUrl: './attendance-daily.component.html',
    styleUrls: ['./attendance-daily.component.css']
})
export class AttendanceDailyComponent implements OnInit {
    private readonly attendanceService = inject(AttendanceService);
    private readonly messageService = inject(MessageService);

    selectedDate: Date = new Date();
    maxDate: Date = new Date();
    rows: AttendanceRowVm[] = [];
    loading = false;
    saving = false;

    statusOptions = [
        { label: 'Present', value: 'PRESENT' },
        { label: 'Late', value: 'LATE' },
        { label: 'Half Day', value: 'HALF_DAY' },
        { label: 'Absent', value: 'ABSENT' },
        { label: 'Leave', value: 'LEAVE' },
        { label: 'Holiday', value: 'HOLIDAY' }
    ];

    get markedCount(): number {
        return this.rows.filter(r => r.status).length;
    }

    ngOnInit(): void {
        this.loadSheet();
    }

    private toDateOnly(value: Date): string {
        const month = String(value.getMonth() + 1).padStart(2, '0');
        const day = String(value.getDate()).padStart(2, '0');
        return `${value.getFullYear()}-${month}-${day}`;
    }

    private toInputTime(value: string | null): string {
        // "09:05:00" → "09:05"
        return value ? value.substring(0, 5) : '';
    }

    private toApiTime(value: string): string | null {
        // "09:05" → "09:05:00"
        return value ? `${value}:00` : null;
    }

    loadSheet(): void {
        this.loading = true;
        this.attendanceService.getDailySheet(this.toDateOnly(this.selectedDate)).subscribe({
            next: (rows) => {
                this.rows = rows.map(r => ({
                    ...r,
                    checkIn: this.toInputTime(r.checkInTime),
                    checkOut: this.toInputTime(r.checkOutTime),
                    dirty: false
                }));
                this.loading = false;
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to load attendance sheet'
                });
                console.error('Error loading attendance sheet:', err);
                this.loading = false;
            }
        });
    }

    onDateChange(): void {
        this.loadSheet();
    }

    markRow(row: AttendanceRowVm): void {
        row.dirty = true;
    }

    markAll(status: string): void {
        this.rows.forEach(row => {
            row.status = status;
            row.dirty = true;
        });
    }

    saveAll(): void {
        const entries: MarkAttendanceEntry[] = this.rows
            .filter(r => r.status)
            .map(r => ({
                employeeId: r.employeeId,
                status: r.status,
                checkInTime: this.toApiTime(r.checkIn),
                checkOutTime: this.toApiTime(r.checkOut),
                notes: r.notes || ''
            }));

        if (entries.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Nothing to save',
                detail: 'Mark at least one employee first'
            });
            return;
        }

        this.saving = true;
        this.attendanceService.markDaily({ date: this.toDateOnly(this.selectedDate), entries }).subscribe({
            next: (rows) => {
                this.rows = rows.map(r => ({
                    ...r,
                    checkIn: this.toInputTime(r.checkInTime),
                    checkOut: this.toInputTime(r.checkOutTime),
                    dirty: false
                }));
                this.saving = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Attendance saved for ${entries.length} employee(s)`
                });
            },
            error: (err) => {
                this.saving = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to save attendance'
                });
                console.error('Error saving attendance:', err);
            }
        });
    }
}
