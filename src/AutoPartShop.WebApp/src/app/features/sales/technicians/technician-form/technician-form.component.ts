import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { TechnicianService, CreateTechnicianRequest, UpdateTechnicianRequest } from '../../services/technician.service';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';

@Component({
  selector: 'app-technician-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    CardModule,
    ToastModule,
    TextareaModule,
    TooltipModule
  ],
  providers: [MessageService],
  templateUrl: './technician-form.component.html',
  styleUrls: ['./technician-form.component.css']
})
export class TechnicianFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly technicianService = inject(TechnicianService);
  private readonly messageService = inject(MessageService);
  private readonly codeGenerationService = inject(CodeGenerationService);

  technicianForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'edit' | 'view'>('create');
  technicianId = signal<string | null>(null);
  generatingCode = signal(false);

  ngOnInit(): void {
    this.initializeForm();

    this.route.queryParams.subscribe(params => {
      const id = params['id'];
      const mode = params['mode'];

      if (id) {
        this.technicianId.set(id);
        this.mode.set(mode === 'view' ? 'view' : 'edit');
        this.loadTechnician(id);
      } else {
        // Create mode - generate technician code automatically
        this.generateTechnicianCode();
      }
    });

    if (this.mode() === 'view') {
      this.technicianForm.disable();
    }
  }

  generateTechnicianCode(): void {
    this.generatingCode.set(true);
    this.codeGenerationService.generateTechnicianCode().subscribe({
      next: (code) => {
        this.technicianForm.patchValue({ technicianCode: code });
        this.generatingCode.set(false);
      },
      error: (err) => {
        console.error('Failed to generate technician code:', err);
        this.messageService.add({
          severity: 'warn',
          summary: 'Warning',
          detail: 'Could not auto-generate code. Please enter manually.'
        });
        this.generatingCode.set(false);
      }
    });
  }

  initializeForm(): void {
    this.technicianForm = this.fb.group({
      technicianCode: ['', [Validators.required, Validators.minLength(2)]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      phone: ['', [Validators.required]],
      email: ['', [Validators.email]],
      shopName: [''],
      address: [''],
      city: [''],
      notes: ['']
    });
  }

  loadTechnician(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.technicianService.getTechnicianById(id).subscribe({
      next: (technician) => {
        this.technicianForm.patchValue({
          technicianCode: technician.technicianCode,
          name: technician.name,
          phone: technician.phone,
          email: technician.email,
          shopName: technician.shopName,
          address: technician.address,
          city: technician.city,
          notes: technician.notes
        });
        this.loading.set(false);

        if (this.mode() === 'view') {
          this.technicianForm.disable();
        }
      },
      error: (err: any) => {
        this.error.set('Failed to load technician');
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load technician'
        });
        console.error('Error loading technician:', err);
      }
    });
  }

  onSubmit(): void {
    if (this.technicianForm.invalid) {
      Object.keys(this.technicianForm.controls).forEach(key => {
        const control = this.technicianForm.get(key);
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

    // Use getRawValue() to include disabled fields like technicianCode
    const formValue = this.technicianForm.getRawValue();

    if (this.mode() === 'create') {
      const request: CreateTechnicianRequest = {
        technicianCode: formValue.technicianCode,
        name: formValue.name,
        phone: formValue.phone,
        email: formValue.email || '',
        shopName: formValue.shopName || '',
        address: formValue.address || '',
        city: formValue.city || '',
        notes: formValue.notes || ''
      };

      this.technicianService.createTechnician(request).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Technician created successfully!'
          });
          setTimeout(() => {
            this.router.navigate(['/sales/technicians']);
          }, 1000);
        },
        error: (err: any) => {
          this.error.set('Failed to create technician');
          this.saving.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to create technician'
          });
          console.error('Error creating technician:', err);
        }
      });
    } else if (this.mode() === 'edit') {
      const request: UpdateTechnicianRequest = {
        name: formValue.name,
        phone: formValue.phone,
        email: formValue.email || '',
        shopName: formValue.shopName || '',
        address: formValue.address || '',
        city: formValue.city || '',
        notes: formValue.notes || ''
      };

      this.technicianService.updateTechnician(this.technicianId()!, request).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Technician updated successfully!'
          });
          setTimeout(() => {
            this.router.navigate(['/sales/technicians']);
          }, 1000);
        },
        error: (err: any) => {
          this.error.set('Failed to update technician');
          this.saving.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update technician'
          });
          console.error('Error updating technician:', err);
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/sales/technicians']);
  }

  getPageTitle(): string {
    switch (this.mode()) {
      case 'create':
        return 'Add New Technician';
      case 'edit':
        return 'Edit Technician';
      case 'view':
        return 'Technician Details';
      default:
        return 'Technician';
    }
  }

  getPageSubtitle(): string {
    switch (this.mode()) {
      case 'create':
        return 'Fill in the details to register a new technician';
      case 'edit':
        return 'Update technician information';
      case 'view':
        return 'View technician details and information';
      default:
        return '';
    }
  }
}
