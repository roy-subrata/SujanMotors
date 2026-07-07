import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HolidayService, HolidayResponse } from '../services/holiday.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';

@Component({
    selector: 'app-holidays',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, Select, DatePickerModule,
        DialogModule, TooltipModule, ToastModule, ConfirmDialogModule,
        PageContainerComponent, PageHeaderComponent, FilterBarComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './holidays.component.html',
    styleUrls: ['./holidays.component.css']
})
export class HolidaysComponent implements OnInit {
    private readonly holidayService = inject(HolidayService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    holidays: HolidayResponse[] = [];
    loading = false;

    selectedYear = new Date().getFullYear();
    yearOptions: { label: string; value: number }[] = [];

    dialogVisible = false;
    saving = false;
    editingId: string | null = null;
    form: { date: Date | null; name: string } = { date: null, name: '' };

    ngOnInit(): void {
        const current = new Date().getFullYear();
        for (let y = current - 1; y <= current + 1; y++) {
            this.yearOptions.push({ label: String(y), value: y });
        }
        this.loadHolidays();
    }

    private toDateOnly(value: Date): string {
        const month = String(value.getMonth() + 1).padStart(2, '0');
        const day = String(value.getDate()).padStart(2, '0');
        return `${value.getFullYear()}-${month}-${day}`;
    }

    loadHolidays(): void {
        this.loading = true;
        this.holidayService.getHolidays(this.selectedYear).subscribe({
            next: (holidays) => {
                this.holidays = holidays;
                this.loading = false;
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to load holidays'
                });
                console.error('Error loading holidays:', err);
                this.loading = false;
            }
        });
    }

    openCreate(): void {
        this.editingId = null;
        this.form = { date: null, name: '' };
        this.dialogVisible = true;
    }

    openEdit(holiday: HolidayResponse): void {
        this.editingId = holiday.id;
        this.form = { date: new Date(holiday.date), name: holiday.name };
        this.dialogVisible = true;
    }

    saveHoliday(): void {
        if (!this.form.date || !this.form.name.trim()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Date and name are required'
            });
            return;
        }

        this.saving = true;
        const payload = { date: this.toDateOnly(this.form.date), name: this.form.name.trim() };

        const action = this.editingId
            ? this.holidayService.updateHoliday(this.editingId, payload)
            : this.holidayService.createHoliday(payload);

        action.subscribe({
            next: () => {
                this.saving = false;
                this.dialogVisible = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.editingId ? 'Holiday updated' : 'Holiday added'
                });
                this.loadHolidays();
            },
            error: (err) => {
                this.saving = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to save holiday'
                });
                console.error('Error saving holiday:', err);
            }
        });
    }

    deleteHoliday(holiday: HolidayResponse): void {
        this.confirmationService.confirm({
            message: `Delete holiday "${holiday.name}"?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.holidayService.deleteHoliday(holiday.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Holiday deleted' });
                        this.loadHolidays();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message || 'Failed to delete holiday'
                        });
                    }
                });
            }
        });
    }
}
