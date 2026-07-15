import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { EmployeeService, EmployeeRequest, LinkableUser } from '../../services/employee.service';
import { ShiftService, ShiftResponse } from '../../services/shift.service';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';
import { FileUploadService, UPLOAD_LIMITS, resolveFileUrl } from '@/shared/services/file-upload.service';

@Component({
    selector: 'app-employee-form',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, ButtonModule, InputTextModule, InputNumberModule, DatePickerModule, SelectModule, CardModule, ToastModule, TextareaModule, TooltipModule],
    providers: [MessageService],
    templateUrl: './employee-form.component.html',
    styleUrls: ['./employee-form.component.css']
})
export class EmployeeFormComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly employeeService = inject(EmployeeService);
    private readonly shiftService = inject(ShiftService);
    private readonly messageService = inject(MessageService);
    private readonly codeGenerationService = inject(CodeGenerationService);
    private readonly fileUploadService = inject(FileUploadService);

    employeeForm!: FormGroup;
    loading = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);
    mode = signal<'create' | 'edit' | 'view'>('create');
    employeeId = signal<string | null>(null);
    generatingCode = signal(false);

    /** Relative uploaded-photo URL (as stored server-side); resolved for display via photoDisplayUrl(). */
    photoUrl = signal<string | null>(null);
    photoUploading = signal(false);
    readonly photoAccept = UPLOAD_LIMITS.image.accept;

    linkableUsers: LinkableUser[] = [];
    shifts: ShiftResponse[] = [];
    maxDob = new Date();

    genderOptions = [
        { label: 'Male', value: 'MALE' },
        { label: 'Female', value: 'FEMALE' },
        { label: 'Other', value: 'OTHER' }
    ];

    departmentOptions = [
        { label: 'Sales', value: 'SALES' },
        { label: 'Warehouse', value: 'WAREHOUSE' },
        { label: 'Accounts', value: 'ACCOUNTS' },
        { label: 'Admin', value: 'ADMIN' }
    ];

    employmentTypeOptions = [
        { label: 'Full Time', value: 'FULL_TIME' },
        { label: 'Part Time', value: 'PART_TIME' },
        { label: 'Contract', value: 'CONTRACT' }
    ];

    ngOnInit(): void {
        this.initializeForm();

        this.route.queryParams.subscribe((params) => {
            const id = params['id'];
            const mode = params['mode'];

            if (id) {
                this.employeeId.set(id);
                this.mode.set(mode === 'view' ? 'view' : 'edit');
                this.loadEmployee(id);
            } else {
                this.generateEmployeeCode();
            }
            this.loadLinkableUsers();
            this.loadShifts();
        });

        if (this.mode() === 'view') {
            this.employeeForm.disable();
        }
    }

    generateEmployeeCode(): void {
        this.generatingCode.set(true);
        this.codeGenerationService.getCode('employee').subscribe({
            next: (code) => {
                this.employeeForm.patchValue({ employeeCode: code });
                this.generatingCode.set(false);
            },
            error: (err) => {
                console.error('Failed to generate employee code:', err);
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Warning',
                    detail: 'Could not auto-generate code. It will be assigned on save.'
                });
                this.generatingCode.set(false);
            }
        });
    }

    loadShifts(): void {
        if (this.shifts.length > 0) return;
        this.shiftService.getShifts().subscribe({
            next: (shifts) => (this.shifts = shifts),
            error: (err) => console.error('Failed to load shifts:', err)
        });
    }

    loadLinkableUsers(): void {
        this.employeeService.getLinkableUsers(this.employeeId() ?? undefined).subscribe({
            next: (users) => (this.linkableUsers = users),
            error: (err) => console.error('Failed to load linkable users:', err)
        });
    }

    initializeForm(): void {
        this.employeeForm = this.fb.group({
            employeeCode: [{ value: '', disabled: true }],
            name: ['', [Validators.required, Validators.minLength(2)]],
            phone: ['', [Validators.required]],
            email: ['', [Validators.email]],
            nidNumber: [''],
            dateOfBirth: [null],
            gender: [''],
            address: [''],
            city: [''],
            designation: ['', [Validators.required]],
            department: ['', [Validators.required]],
            joinDate: [new Date(), [Validators.required]],
            employmentType: ['FULL_TIME', [Validators.required]],
            monthlySalary: [0, [Validators.required, Validators.min(0)]],
            shiftId: [null],
            monthlyTaxDeduction: [0, [Validators.min(0)]],
            commissionRate: [0, [Validators.min(0), Validators.max(100)]],
            emergencyContactName: [''],
            emergencyContactPhone: [''],
            notes: [''],
            userId: [null]
        });
    }

    loadEmployee(id: string): void {
        this.loading.set(true);
        this.error.set(null);

        this.employeeService.getEmployeeById(id).subscribe({
            next: (employee) => {
                this.employeeForm.patchValue({
                    employeeCode: employee.employeeCode,
                    name: employee.name,
                    phone: employee.phone,
                    email: employee.email,
                    nidNumber: employee.nidNumber,
                    dateOfBirth: employee.dateOfBirth ? new Date(employee.dateOfBirth) : null,
                    gender: employee.gender,
                    address: employee.address,
                    city: employee.city,
                    designation: employee.designation,
                    department: employee.department,
                    joinDate: employee.joinDate ? new Date(employee.joinDate) : null,
                    employmentType: employee.employmentType,
                    monthlySalary: employee.monthlySalary,
                    shiftId: employee.shiftId,
                    monthlyTaxDeduction: employee.monthlyTaxDeduction,
                    commissionRate: employee.commissionRate,
                    emergencyContactName: employee.emergencyContactName,
                    emergencyContactPhone: employee.emergencyContactPhone,
                    notes: employee.notes,
                    userId: employee.userId
                });
                this.photoUrl.set(employee.photoUrl);
                this.loading.set(false);

                if (this.mode() === 'view') {
                    this.employeeForm.disable();
                }
            },
            error: (err: any) => {
                this.error.set('Failed to load employee');
                this.loading.set(false);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to load employee'
                });
                console.error('Error loading employee:', err);
            }
        });
    }

    private toDateOnly(value: Date | null): string | null {
        if (!value) return null;
        const d = new Date(value);
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${d.getFullYear()}-${month}-${day}`;
    }

    onSubmit(): void {
        if (this.employeeForm.invalid) {
            Object.keys(this.employeeForm.controls).forEach((key) => {
                const control = this.employeeForm.get(key);
                if (control?.invalid) {
                    control.markAsTouched();
                }
            });
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Please fill all required fields correctly'
            });
            return;
        }

        this.saving.set(true);
        this.error.set(null);

        const formValue = this.employeeForm.getRawValue();

        const request: EmployeeRequest = {
            name: formValue.name,
            phone: formValue.phone,
            email: formValue.email || '',
            nidNumber: formValue.nidNumber || '',
            dateOfBirth: this.toDateOnly(formValue.dateOfBirth),
            gender: formValue.gender || '',
            address: formValue.address || '',
            city: formValue.city || '',
            designation: formValue.designation || '',
            department: formValue.department || '',
            joinDate: this.toDateOnly(formValue.joinDate)!,
            employmentType: formValue.employmentType || 'FULL_TIME',
            monthlySalary: formValue.monthlySalary ?? 0,
            shiftId: formValue.shiftId || null,
            monthlyTaxDeduction: formValue.monthlyTaxDeduction ?? 0,
            commissionRate: formValue.commissionRate ?? 0,
            emergencyContactName: formValue.emergencyContactName || '',
            emergencyContactPhone: formValue.emergencyContactPhone || '',
            notes: formValue.notes || '',
            userId: formValue.userId || null
        };

        const action = this.mode() === 'create' ? this.employeeService.createEmployee(request) : this.employeeService.updateEmployee(this.employeeId()!, request);

        action.subscribe({
            next: (employee) => {
                // Photo chosen before the employee existed (create mode): link it now.
                if (this.mode() === 'create' && this.photoUrl()) {
                    this.employeeService.setPhoto(employee.id, this.photoUrl()).subscribe({
                        error: (err: any) => console.error('Employee created but photo could not be linked:', err)
                    });
                }
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.mode() === 'create' ? 'Employee created successfully!' : 'Employee updated successfully!'
                });
                setTimeout(() => {
                    this.router.navigate(['/hr/employees']);
                }, 1000);
            },
            error: (err: any) => {
                const fallback = this.mode() === 'create' ? 'Failed to create employee' : 'Failed to update employee';
                this.error.set(fallback);
                this.saving.set(false);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || fallback
                });
                console.error('Error saving employee:', err);
            }
        });
    }

    // ── Profile photo ─────────────────────────────────────────────────────────

    photoDisplayUrl(): string {
        return resolveFileUrl(this.photoUrl());
    }

    onPhotoSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0] ?? null;
        input.value = '';
        if (!file) return;

        if (file.size > UPLOAD_LIMITS.image.maxBytes) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Too large',
                detail: `Photo must be under ${UPLOAD_LIMITS.image.label}`
            });
            return;
        }

        this.photoUploading.set(true);
        this.fileUploadService.upload(file, 'EMPLOYEE', this.employeeId() ?? undefined).subscribe({
            next: (stored) => {
                // Existing employee: persist immediately. New employee: hold the URL and
                // link it right after create succeeds (see onSubmit).
                if (this.employeeId()) {
                    this.employeeService.setPhoto(this.employeeId()!, stored.url).subscribe({
                        next: () => {
                            this.photoUrl.set(stored.url);
                            this.photoUploading.set(false);
                            this.messageService.add({ severity: 'success', summary: 'Photo updated', detail: file.name });
                        },
                        error: (err: any) => {
                            this.photoUploading.set(false);
                            this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to save photo' });
                        }
                    });
                } else {
                    this.photoUrl.set(stored.url);
                    this.photoUploading.set(false);
                }
            },
            error: (err: any) => {
                this.photoUploading.set(false);
                this.messageService.add({ severity: 'error', summary: 'Upload failed', detail: err?.error?.message || 'Could not upload the photo' });
            }
        });
    }

    removePhoto(): void {
        if (this.employeeId()) {
            this.employeeService.setPhoto(this.employeeId()!, null).subscribe({
                next: () => {
                    this.photoUrl.set(null);
                    this.messageService.add({ severity: 'info', summary: 'Photo removed' });
                },
                error: (err: any) => {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to remove photo' });
                }
            });
        } else {
            this.photoUrl.set(null);
        }
    }

    cancel(): void {
        this.router.navigate(['/hr/employees']);
    }

    getPageTitle(): string {
        switch (this.mode()) {
            case 'create':
                return 'Add New Employee';
            case 'edit':
                return 'Edit Employee';
            case 'view':
                return 'Employee Details';
            default:
                return 'Employee';
        }
    }

    getPageSubtitle(): string {
        switch (this.mode()) {
            case 'create':
                return 'Fill in the details to register a new employee';
            case 'edit':
                return 'Update employee information';
            case 'view':
                return 'View employee details and information';
            default:
                return '';
        }
    }
}
