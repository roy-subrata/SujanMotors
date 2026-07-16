import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ShiftService, ShiftResponse } from '../services/shift.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
    selector: 'app-shifts',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, InputNumberModule,
        DialogModule, TooltipModule, ToastModule, ConfirmDialogModule,
        PageContainerComponent, PageHeaderComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './shifts.component.html',
    styleUrls: ['./shifts.component.css']
})
export class ShiftsComponent implements OnInit {
    private readonly shiftService = inject(ShiftService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    shifts: ShiftResponse[] = [];
    loading = false;

    dialogVisible = false;
    saving = false;
    editingId: string | null = null;
    form = { name: '', startTime: '09:00', endTime: '18:00', graceMinutes: 10, notes: '' };

    ngOnInit(): void {
        this.loadShifts();
    }

    loadShifts(): void {
        this.loading = true;
        this.shiftService.getShifts().subscribe({
            next: (shifts) => {
                this.shifts = shifts;
                this.loading = false;
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to load shifts'
                });
                console.error('Error loading shifts:', err);
                this.loading = false;
            }
        });
    }

    formatTime(value: string): string {
        return value ? value.substring(0, 5) : '';
    }

    openCreate(): void {
        this.editingId = null;
        this.form = { name: '', startTime: '09:00', endTime: '18:00', graceMinutes: 10, notes: '' };
        this.dialogVisible = true;
    }

    openEdit(shift: ShiftResponse): void {
        this.editingId = shift.id;
        this.form = {
            name: shift.name,
            startTime: this.formatTime(shift.startTime),
            endTime: this.formatTime(shift.endTime),
            graceMinutes: shift.graceMinutes,
            notes: shift.notes
        };
        this.dialogVisible = true;
    }

    saveShift(): void {
        if (!this.form.name.trim() || !this.form.startTime || !this.form.endTime) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name, start and end times are required'
            });
            return;
        }

        this.saving = true;
        const payload = {
            name: this.form.name.trim(),
            startTime: `${this.form.startTime}:00`,
            endTime: `${this.form.endTime}:00`,
            graceMinutes: this.form.graceMinutes ?? 10,
            notes: this.form.notes || ''
        };

        const action = this.editingId
            ? this.shiftService.updateShift(this.editingId, payload)
            : this.shiftService.createShift(payload);

        action.subscribe({
            next: () => {
                this.saving = false;
                this.dialogVisible = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.editingId ? 'Shift updated' : 'Shift added'
                });
                this.loadShifts();
            },
            error: (err) => {
                this.saving = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to save shift'
                });
                console.error('Error saving shift:', err);
            }
        });
    }

    deleteShift(shift: ShiftResponse): void {
        this.confirmationService.confirm({
            message: `Delete shift "${shift.name}"?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.shiftService.deleteShift(shift.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Shift deleted' });
                        this.loadShifts();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message || 'Failed to delete shift'
                        });
                    }
                });
            }
        });
    }
}
