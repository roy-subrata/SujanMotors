import { Component, Output, EventEmitter, signal, inject, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { map } from 'rxjs/operators';
import { of } from 'rxjs';

import {
  DiscountResponse,
  DiscountService,
  CreateDiscountRequest,
  UpdateDiscountRequest
} from '../../services/discount.service';
import { PublicPartService, PublicPartResponse } from '@/features/sales/services/public-part.service';
import { ProductVariantService, ProductVariantResponse } from '../../services/product-variant.service';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '@/shared/components/lazy-autocomplete';

@Component({
  selector: 'app-discount-form-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    DialogModule, ButtonModule, InputTextModule, TextareaModule,
    InputNumberModule, SelectModule, DatePickerModule, ToggleSwitchModule,
    ToastModule, LazyAutocompleteComponent
  ],
  templateUrl: './discount-form-dialog.component.html',
  styleUrls: ['./discount-form-dialog.component.css'],
  providers: [MessageService]
})
export class DiscountFormDialogComponent implements OnChanges {
  @Input() displayCreateDialog = false;
  @Input() displayUpdateDialog = false;
  @Input() selectedDiscount: DiscountResponse | null = null;

  @Output() displayCreateDialogChange = new EventEmitter<boolean>();
  @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
  @Output() createSuccess = new EventEmitter<void>();
  @Output() updateSuccess = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly discountService = inject(DiscountService);
  private readonly partService = inject(PublicPartService);
  private readonly variantService = inject(ProductVariantService);
  private readonly messageService = inject(MessageService);

  isCreating = signal(false);
  isUpdating = signal(false);

  // Autocomplete selections
  selectedPart = signal<PublicPartResponse | null>(null);
  selectedVariant = signal<ProductVariantResponse | null>(null);
  variants = signal<ProductVariantResponse[]>([]);
  loadingVariants = signal(false);

  typeOptions = [
    { label: 'Percentage (%)', value: 'PERCENTAGE' },
    { label: 'Fixed Amount (৳)', value: 'FIXED' }
  ];

  // ── Lazy fetch for parts autocomplete ─────────────────────────────────────
  fetchPartsLazy = (req: LazyRequest) =>
    this.partService.getParts({
      search: req.search,
      pageNumber: req.pageNumber,
      pageSize: req.pageSize,
      isActive: true
    }).pipe(
      map(res => ({
        items: res.data,
        totalCount: res.pagination.totalCount
      } as LazyResponse<PublicPartResponse>))
    );

  // ── Forms ──────────────────────────────────────────────────────────────────
  createForm = this.fb.group({
    name:              ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    description:       [''],
    type:              ['PERCENTAGE' as 'PERCENTAGE' | 'FIXED', [Validators.required]],
    value:             [null as number | null, [Validators.required, Validators.min(0.01)]],
    promoCode:         [''],
    minimumCartAmount: [null as number | null],
    startDate:         [null as Date | null, [Validators.required]],
    endDate:           [null as Date | null]
  });

  updateForm = this.fb.group({
    id:                [''],
    name:              ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    description:       [''],
    type:              ['PERCENTAGE' as 'PERCENTAGE' | 'FIXED', [Validators.required]],
    value:             [null as number | null, [Validators.required, Validators.min(0.01)]],
    promoCode:         [''],
    minimumCartAmount: [null as number | null],
    startDate:         [null as Date | null, [Validators.required]],
    endDate:           [null as Date | null],
    isActive:          [true]
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedDiscount'] && this.selectedDiscount && this.displayUpdateDialog)
      this.patchUpdateForm();
  }

  // ── Computed getters ───────────────────────────────────────────────────────
  get createType(): 'PERCENTAGE' | 'FIXED' {
    return this.createForm.get('type')?.value as 'PERCENTAGE' | 'FIXED' || 'PERCENTAGE';
  }
  get updateType(): 'PERCENTAGE' | 'FIXED' {
    return this.updateForm.get('type')?.value as 'PERCENTAGE' | 'FIXED' || 'PERCENTAGE';
  }
  get createScope(): 'VARIANT' | 'PRODUCT' | 'CART' {
    const part = this.selectedPart();
    const variant = this.selectedVariant();
    if (part && variant) return 'VARIANT';
    if (part) return 'PRODUCT';
    return 'CART';
  }
  get isCreateCartLevel(): boolean { return this.createScope === 'CART'; }
  get createValueMax(): number { return this.createType === 'PERCENTAGE' ? 100 : 9999999; }
  get updateValueMax(): number { return this.updateType === 'PERCENTAGE' ? 100 : 9999999; }

  // ── Part / Variant selection handlers ─────────────────────────────────────
  onPartSelected(part: PublicPartResponse): void {
    this.selectedPart.set(part);
    this.selectedVariant.set(null);
    this.variants.set([]);
    this.loadVariants(part.id);
  }

  onPartCleared(): void {
    this.selectedPart.set(null);
    this.selectedVariant.set(null);
    this.variants.set([]);
  }

  private loadVariants(partId: string): void {
    this.loadingVariants.set(true);
    this.variantService.getVariants(partId).subscribe({
      next: (v) => {
        this.variants.set(v.filter(x => x.isActive));
        this.loadingVariants.set(false);
      },
      error: () => this.loadingVariants.set(false)
    });
  }

  onVariantSelected(variant: ProductVariantResponse): void {
    this.selectedVariant.set(variant);
  }

  onVariantCleared(): void {
    this.selectedVariant.set(null);
  }

  // ── Dialog lifecycle ───────────────────────────────────────────────────────
  onCreateDialogShow(): void {
    this.createForm.reset({ type: 'PERCENTAGE' });
    this.selectedPart.set(null);
    this.selectedVariant.set(null);
    this.variants.set([]);
  }

  onCreateDialogHide(): void {
    this.displayCreateDialogChange.emit(false);
    this.createForm.reset({ type: 'PERCENTAGE' });
    this.selectedPart.set(null);
    this.selectedVariant.set(null);
    this.variants.set([]);
  }

  onUpdateDialogShow(): void { this.patchUpdateForm(); }

  onUpdateDialogHide(): void {
    this.displayUpdateDialogChange.emit(false);
    this.updateForm.reset();
  }

  private patchUpdateForm(): void {
    const d = this.selectedDiscount;
    if (!d) return;
    this.updateForm.patchValue({
      id:               d.id,
      name:             d.name,
      description:      d.description || '',
      type:             d.type,
      value:            d.value,
      promoCode:        d.promoCode || '',
      minimumCartAmount: d.minimumCartAmount ?? null,
      startDate:        d.startDate ? new Date(d.startDate) : null,
      endDate:          d.endDate ? new Date(d.endDate) : null,
      isActive:         d.isActive
    });
  }

  // ── Create ─────────────────────────────────────────────────────────────────
  onCreateSubmit(): void {
    if (!this.createForm.valid) {
      this.createForm.markAllAsTouched();
      this.messageService.add({ severity: 'error', summary: 'Validation Error', detail: 'Please fill all required fields correctly' });
      return;
    }

    const v = this.createForm.getRawValue();

    if (v.type === 'PERCENTAGE' && (v.value ?? 0) > 100) {
      this.messageService.add({ severity: 'error', summary: 'Validation Error', detail: 'Percentage value cannot exceed 100' });
      return;
    }

    if (this.isCreateCartLevel && v.promoCode && v.minimumCartAmount) {
      this.messageService.add({ severity: 'error', summary: 'Validation Error', detail: 'Provide either a promo code or a minimum cart amount, not both' });
      return;
    }

    const isCart = this.isCreateCartLevel;
    const request: CreateDiscountRequest = {
      name:              v.name || '',
      description:       v.description || undefined,
      type:              v.type as 'PERCENTAGE' | 'FIXED',
      value:             v.value ?? 0,
      partId:            this.selectedPart()?.id || undefined,
      productVariantId:  this.selectedVariant()?.id || undefined,
      promoCode:         isCart && v.promoCode ? v.promoCode : undefined,
      minimumCartAmount: isCart && v.minimumCartAmount ? v.minimumCartAmount : undefined,
      startDate:         v.startDate ? (v.startDate as Date).toISOString() : '',
      endDate:           v.endDate ? (v.endDate as Date).toISOString() : undefined
    };

    this.isCreating.set(true);
    this.discountService.createDiscount(request).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Discount created successfully' });
        this.isCreating.set(false);
        this.onCreateDialogHide();
        this.createSuccess.emit();
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to create discount' });
        this.isCreating.set(false);
      }
    });
  }

  // ── Update ─────────────────────────────────────────────────────────────────
  onUpdateSubmit(): void {
    if (!this.updateForm.valid || !this.selectedDiscount) {
      this.updateForm.markAllAsTouched();
      this.messageService.add({ severity: 'error', summary: 'Validation Error', detail: 'Please fill all required fields correctly' });
      return;
    }

    const v = this.updateForm.getRawValue();
    const isCart = this.selectedDiscount.scope === 'CART';

    if (v.type === 'PERCENTAGE' && (v.value ?? 0) > 100) {
      this.messageService.add({ severity: 'error', summary: 'Validation Error', detail: 'Percentage value cannot exceed 100' });
      return;
    }

    if (isCart && v.promoCode && v.minimumCartAmount) {
      this.messageService.add({ severity: 'error', summary: 'Validation Error', detail: 'Provide either a promo code or a minimum cart amount, not both' });
      return;
    }

    this.isUpdating.set(true);
    const request: UpdateDiscountRequest = {
      id:               this.selectedDiscount.id,
      name:             v.name || '',
      description:      v.description || undefined,
      type:             v.type as 'PERCENTAGE' | 'FIXED',
      value:            v.value ?? 0,
      promoCode:        isCart && v.promoCode ? v.promoCode : undefined,
      minimumCartAmount: isCart && v.minimumCartAmount ? v.minimumCartAmount : undefined,
      startDate:        v.startDate ? (v.startDate as Date).toISOString() : '',
      endDate:          v.endDate ? (v.endDate as Date).toISOString() : undefined,
      isActive:         v.isActive ?? true
    };

    this.discountService.updateDiscount(this.selectedDiscount.id, request).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Discount updated successfully' });
        this.isUpdating.set(false);
        this.onUpdateDialogHide();
        this.updateSuccess.emit();
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to update discount' });
        this.isUpdating.set(false);
      }
    });
  }

  isInvalid(form: 'create' | 'update', controlName: string): boolean {
    const ctrl = form === 'create' ? this.createForm.get(controlName) : this.updateForm.get(controlName);
    return !!(ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched));
  }
}
