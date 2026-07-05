import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { VehicleService, VehicleResponse, PartCompatibilityResponse, CreatePartCompatibilityRequest } from '../services/vehicle.service';
import { PartService, PartResponse } from '../services/part.service';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';
import { map } from 'rxjs';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
  selector: 'app-vehicle-compatibility',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ButtonModule,
    CardModule,
    TableModule,
    TextareaModule,
    CheckboxModule,
    ConfirmDialogModule,
    ToastModule,
    TagModule,
    LazyAutocompleteComponent,
    PageContainerComponent,
    PageHeaderComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './vehicle-compatibility.component.html',
  styleUrls: ['./vehicle-compatibility.component.css']
})
export class VehicleCompatibilityComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly vehicleService = inject(VehicleService);
  private readonly partService = inject(PartService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  vehicleId: string | null = null;
  vehicle: VehicleResponse | null = null;
  compatibilities: PartCompatibilityResponse[] = [];
  loading = false;
  isSubmitting = false;

  // Add compatibility form
  addForm: FormGroup;
  selectedPart: PartResponse | null = null;

  fetchPartsLazy = (req: LazyRequest) =>
    this.partService.getParts({
      search: req.search || '',
      pageNumber: req.pageNumber,
      pageSize: req.pageSize,
      isActive: true,
      flattenVariants: true
    }).pipe(map(res => ({
      items: res.data ?? [],
      totalCount: res.pagination?.totalCount ?? 0
    } as LazyResponse<PartResponse>)));

  constructor() {
    this.addForm = this.createForm();
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['vehicleId']) {
        this.vehicleId = params['vehicleId'];
        this.loadVehicle();
        this.loadCompatibilities();
      } else {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Vehicle ID is required'
        });
        this.router.navigate(['/inventory/vehicles']);
      }
    });
  }

  private createForm(): FormGroup {
    return this.fb.group({
      part: [null, Validators.required],
      isCompatible: [true],
      notes: ['', Validators.maxLength(500)]
    });
  }

  private loadVehicle(): void {
    if (!this.vehicleId) return;

    this.vehicleService.getVehicleById(this.vehicleId).subscribe({
      next: (vehicle: VehicleResponse) => {
        this.vehicle = vehicle;
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load vehicle details'
        });
        console.error('Error loading vehicle:', error);
      }
    });
  }

  private loadCompatibilities(): void {
    if (!this.vehicleId) return;

    this.loading = true;
    this.vehicleService.getVehicleCompatibilities(this.vehicleId).subscribe({
      next: (compatibilities: PartCompatibilityResponse[]) => {
        this.compatibilities = compatibilities;
        this.loading = false;
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load compatibilities'
        });
        console.error('Error loading compatibilities:', error);
        this.loading = false;
      }
    });
  }

  onPartSelected(part: PartResponse): void {
    this.selectedPart = part;
    this.addForm.patchValue({ part });
  }

  onPartCleared(): void {
    this.selectedPart = null;
    this.addForm.patchValue({ part: null });
  }

  addCompatibility(): void {
    if (!this.addForm.valid || !this.vehicleId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Please select a part'
      });
      Object.keys(this.addForm.controls).forEach(key => {
        this.addForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;
    const selectedPart = this.addForm.value.part;

    const request: CreatePartCompatibilityRequest = {
      isCompatible: this.addForm.value.isCompatible,
      notes: this.addForm.value.notes || ''
    };

    this.vehicleService.addPartCompatibility(this.vehicleId, selectedPart.id, request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Part '${selectedPart.name}' added to compatibility list`
        });
        this.loadCompatibilities();
        this.resetForm();
        this.isSubmitting = false;
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to add compatibility'
        });
        console.error('Error adding compatibility:', error);
        this.isSubmitting = false;
      }
    });
  }

  removeCompatibility(compatibility: PartCompatibilityResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to remove compatibility with "${compatibility.partName}"?`,
      header: 'Confirm Removal',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.vehicleService.removeCompatibility(compatibility.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Compatibility removed successfully'
            });
            this.loadCompatibilities();
          },
          error: (error: any) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to remove compatibility'
            });
            console.error('Error removing compatibility:', error);
          }
        });
      }
    });
  }

  private resetForm(): void {
    this.addForm.reset({
      part: null,
      isCompatible: true,
      notes: ''
    });
    this.selectedPart = null;
  }

  goBack(): void {
    this.router.navigate(['/inventory/vehicles']);
  }

  get vehicleSubtitle(): string {
    if (!this.vehicle) return '';
    return `${this.vehicle.make} ${this.vehicle.model} ${this.vehicle.year} · ${this.vehicle.engineType}`;
  }

  hasError(fieldName: string): boolean {
    const field = this.addForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getCompatibilitySeverity(isCompatible: boolean): 'success' | 'warn' {
    return isCompatible ? 'success' : 'warn';
  }

  getCompatibilityLabel(isCompatible: boolean): string {
    return isCompatible ? 'Compatible' : 'Not Compatible';
  }
}
