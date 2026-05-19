import { Component, Output, EventEmitter, signal, inject, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputGroupModule } from 'primeng/inputgroup';
import { BrandResponse, BrandService, CreateBrandRequest, UpdateBrandRequest } from '../../services/brand.service';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';

@Component({
  selector: 'app-brands-form-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    DialogModule, ButtonModule, InputTextModule, TextareaModule,
    CheckboxModule, InputNumberModule, InputGroupModule, ToastModule
  ],
  templateUrl: './brands-form-dialog.component.html',
  styleUrls: ['./brands-form-dialog.component.css'],
  providers: [MessageService]
})
export class BrandsFormDialogComponent implements OnChanges {
  @Input() displayCreateDialog = false;
  @Input() displayUpdateDialog = false;
  @Input() selectedBrand: BrandResponse | null = null;

  @Output() displayCreateDialogChange = new EventEmitter<boolean>();
  @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
  @Output() createSuccess = new EventEmitter<void>();
  @Output() updateSuccess = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly brandService = inject(BrandService);
  private readonly messageService = inject(MessageService);
  private readonly codeGenerationService = inject(CodeGenerationService);

  generatingCode = false;
  isCreating = signal(false);
  isUpdating = signal(false);

  // ── Create form ─────────────────────────────────────────────────────────

  createForm = this.fb.group({
    name:         ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    code:         [{ value: '', disabled: true }, [Validators.required, Validators.maxLength(20)]],
    description:  [''],
    logoUrl:      [''],
    website:      ['', Validators.pattern(/^(https?:\/\/.+)?$/)],
    country:      [''],
    contactEmail: ['', Validators.email],
    contactPhone: [''],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    isActive:     [true]
  });

  // ── Update form ─────────────────────────────────────────────────────────

  updateForm = this.fb.group({
    name:         ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    code:         ['', [Validators.required, Validators.maxLength(20)]],
    description:  [''],
    logoUrl:      [''],
    website:      ['', Validators.pattern(/^(https?:\/\/.+)?$/)],
    country:      [''],
    contactEmail: ['', Validators.email],
    contactPhone: [''],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    isActive:     [true]
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedBrand'] && this.selectedBrand) {
      this.updateForm.patchValue({
        name:         this.selectedBrand.name,
        code:         this.selectedBrand.code,
        description:  this.selectedBrand.description ?? '',
        logoUrl:      this.selectedBrand.logoUrl ?? '',
        website:      this.selectedBrand.website ?? '',
        country:      this.selectedBrand.country ?? '',
        contactEmail: this.selectedBrand.contactEmail ?? '',
        contactPhone: this.selectedBrand.contactPhone ?? '',
        displayOrder: this.selectedBrand.displayOrder,
        isActive:     this.selectedBrand.isActive
      });
    }
  }

  // ── Create dialog hooks ─────────────────────────────────────────────────

  onCreateDialogShow(): void {
    this.createForm.reset({ displayOrder: 0, isActive: true });
    this.generateBrandCode();
  }

  onCreateDialogHide(): void {
    this.displayCreateDialogChange.emit(false);
    this.createForm.reset({ displayOrder: 0, isActive: true });
  }

  generateBrandCode(): void {
    this.generatingCode = true;
    this.codeGenerationService.generateBrandCode().subscribe({
      next: (code) => {
        this.createForm.patchValue({ code });
        this.generatingCode = false;
      },
      error: () => {
        this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Could not auto-generate code. Please enter manually.' });
        this.generatingCode = false;
      }
    });
  }

  // ── Update dialog hooks ─────────────────────────────────────────────────

  onUpdateDialogShow(): void {
    // Populated via ngOnChanges when selectedBrand changes
  }

  onUpdateDialogHide(): void {
    this.displayUpdateDialogChange.emit(false);
    this.updateForm.reset();
  }

  // ── Submit handlers ─────────────────────────────────────────────────────

  onCreateSubmit(): void {
    if (!this.createForm.valid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isCreating.set(true);
    const v = this.createForm.getRawValue();

    const request: CreateBrandRequest = {
      name:         v.name!,
      code:         v.code!,
      description:  v.description || null,
      logoUrl:      v.logoUrl || null,
      website:      v.website || null,
      country:      v.country || null,
      contactEmail: v.contactEmail || null,
      contactPhone: v.contactPhone || null,
      displayOrder: v.displayOrder ?? 0,
      isActive:     v.isActive ?? true
    };

    this.brandService.createBrand(request).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Created', detail: `Brand "${request.name}" created` });
        this.isCreating.set(false);
        this.onCreateDialogHide();
        this.createSuccess.emit();
      },
      error: (err) => {
        const detail = err.error?.detail ?? err.error?.message ?? 'Failed to create brand';
        const isConflict = err.status === 409;
        this.messageService.add({
          severity: 'error',
          summary: isConflict ? 'Duplicate Code' : 'Error',
          detail
        });
        this.isCreating.set(false);
      }
    });
  }

  onUpdateSubmit(): void {
    if (!this.updateForm.valid || !this.selectedBrand) {
      this.updateForm.markAllAsTouched();
      return;
    }

    this.isUpdating.set(true);
    const v = this.updateForm.value;

    const request: UpdateBrandRequest = {
      name:         v.name!,
      code:         v.code!,
      description:  v.description || null,
      logoUrl:      v.logoUrl || null,
      website:      v.website || null,
      country:      v.country || null,
      contactEmail: v.contactEmail || null,
      contactPhone: v.contactPhone || null,
      displayOrder: v.displayOrder ?? 0,
      isActive:     v.isActive ?? true
    };

    this.brandService.updateBrand(this.selectedBrand.id, request).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Updated', detail: `Brand "${request.name}" updated` });
        this.isUpdating.set(false);
        this.onUpdateDialogHide();
        this.updateSuccess.emit();
      },
      error: (err) => {
        const detail = err.error?.detail ?? err.error?.message ?? 'Failed to update brand';
        const isConflict = err.status === 409;
        this.messageService.add({
          severity: 'error',
          summary: isConflict ? 'Duplicate Code' : 'Error',
          detail
        });
        this.isUpdating.set(false);
      }
    });
  }
}
