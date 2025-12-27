import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { TechnicianService, CreateTechnicianRequest, UpdateTechnicianRequest } from '../../services/technician.service';

@Component({
  selector: 'app-technician-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './technician-form.component.html',
  styleUrls: ['./technician-form.component.css']
})
export class TechnicianFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly technicianService = inject(TechnicianService);

  technicianForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'edit' | 'view'>('create');
  technicianId = signal<string | null>(null);

  ngOnInit(): void {
    this.initializeForm();

    this.route.queryParams.subscribe(params => {
      const id = params['id'];
      const mode = params['mode'];

      if (id) {
        this.technicianId.set(id);
        this.mode.set(mode === 'view' ? 'view' : 'edit');
        this.loadTechnician(id);
      }
    });

    if (this.mode() === 'view') {
      this.technicianForm.disable();
    }
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
      },
      error: (err: any) => {
        this.error.set('Failed to load technician');
        this.loading.set(false);
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
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const formValue = this.technicianForm.value;

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
          alert('Technician created successfully!');
          this.router.navigate(['/sales/technicians']);
        },
        error: (err: any) => {
          this.error.set('Failed to create technician');
          this.saving.set(false);
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
          alert('Technician updated successfully!');
          this.router.navigate(['/sales/technicians']);
        },
        error: (err: any) => {
          this.error.set('Failed to update technician');
          this.saving.set(false);
          console.error('Error updating technician:', err);
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/sales/technicians']);
  }
}
