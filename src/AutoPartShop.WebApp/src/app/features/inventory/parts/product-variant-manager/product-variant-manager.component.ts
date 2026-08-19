import { Component, Input, OnInit, OnChanges, SimpleChanges, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import {
  ProductVariantService,
  ProductVariantResponse,
  CreateVariantRequest,
  VariantAttributeValueRequest
} from '../../services/product-variant.service';
import {
  ProductAttributeService,
  ProductAttributeGroup,
  ProductAttribute
} from '../../services/product-attribute.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { MoneyFormatPipe } from '@/shared/pipes/money-format.pipe';

interface AttrRow {
  attributeId: string;
  label: string;
  groupName: string;
  dataType: string;
  unit: string;
  options: { id: string; value: string }[];
}

@Component({
  selector: 'app-product-variant-manager',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    CheckboxModule,
    TableModule,
    TagModule,
    ToastModule,
    TooltipModule,
    DialogModule,
    Select,
    ConfirmDialogModule,
    TranslatePipe,
    MoneyFormatPipe
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './product-variant-manager.component.html'
})
export class ProductVariantManagerComponent implements OnInit, OnChanges {
  @Input() partId!: string;
  @Input() isViewMode = false;

  private readonly variantService = inject(ProductVariantService);
  private readonly attributeService = inject(ProductAttributeService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  variants: ProductVariantResponse[] = [];
  attributeGroups: ProductAttributeGroup[] = [];
  allAttributes: ProductAttribute[] = [];

  // The attribute rows currently added to the dialog form
  selectedAttrRows: AttrRow[] = [];
  // Controls which attribute the user is about to add
  attrToAdd: string | null = null;

  dialogVisible = false;
  isEditing = false;
  editingVariantId: string | null = null;
  isSubmitting = false;
  isLoading = false;

  variantForm!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    if (this.partId) {
      this.loadVariants();
      this.loadAttributeGroups();
    }
  }

  // Fires when partId changes from '' → real ID (after product is created)
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['partId'] && !changes['partId'].firstChange && changes['partId'].currentValue) {
      this.loadVariants();
      if (this.allAttributes.length === 0) this.loadAttributeGroups();
    }
  }

  get attrValuesArray(): FormArray {
    return this.variantForm.get('attributeValues') as FormArray;
  }

  /** Attributes NOT yet added to this variant form */
  get availableAttributes(): { label: string; value: string }[] {
    const alreadyAdded = new Set(this.selectedAttrRows.map(r => r.attributeId));
    return this.allAttributes
      .filter(a => a.isActive && !alreadyAdded.has(a.id))
      .map(a => ({ label: `${a.name} (${a.dataType})`, value: a.id }));
  }

  loadVariants(): void {
    this.isLoading = true;
    this.variantService.getVariants(this.partId).subscribe({
      next: (v) => { this.variants = v; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  loadAttributeGroups(): void {
    this.attributeService.getAllGroups().subscribe({
      next: (groups) => {
        this.attributeGroups = groups.filter(g => g.isActive);
        this.allAttributes = groups.flatMap(g => g.attributes.filter(a => a.isActive));
      }
    });
  }

  openAddDialog(): void {
    this.isEditing = false;
    this.editingVariantId = null;
    this.selectedAttrRows = [];
    this.attrToAdd = null;
    this.initForm();
    this.dialogVisible = true;
  }

  openEditDialog(variant: ProductVariantResponse): void {
    this.isEditing = true;
    this.editingVariantId = variant.id;
    this.attrToAdd = null;

    this.initForm();
    this.variantForm.patchValue({
      name: variant.name,
      code: variant.code,
      partNumber: variant.partNumber || '',
      oemNumber: variant.oemNumber || '',
      sku: variant.sku || '',
      barcode: variant.barcode || '',
      sellingPrice: variant.sellingPrice ?? null,
      currency: variant.currency || 'BDT',
      isActive: variant.isActive
    });

    // Pre-populate only the attributes this variant already has values for
    this.selectedAttrRows = [];
    (variant.attributeValues ?? []).forEach(av => {
      const attr = this.allAttributes.find(a => a.id === av.attributeId);
      if (!attr) return;
      this.addAttrRowForAttribute(attr);
      // Patch value into the form control
      const ctrl = this.attrValuesArray.at(this.attrValuesArray.length - 1);
      if (attr.dataType === 'option') ctrl.patchValue({ optionId: av.optionId });
      else if (attr.dataType === 'number') ctrl.patchValue({ valueNumber: av.valueNumber });
      else if (attr.dataType === 'boolean') ctrl.patchValue({ valueBool: av.valueBool ?? false });
      else ctrl.patchValue({ valueText: av.valueText || '' });
    });

    this.dialogVisible = true;
  }

  addSelectedAttribute(): void {
    if (!this.attrToAdd) return;
    const attr = this.allAttributes.find(a => a.id === this.attrToAdd);
    if (!attr) return;
    this.addAttrRowForAttribute(attr);
    this.attrToAdd = null;
  }

  removeAttrRow(index: number): void {
    this.selectedAttrRows.splice(index, 1);
    this.attrValuesArray.removeAt(index);
  }

  closeDialog(): void {
    this.dialogVisible = false;
  }

  onSubmit(): void {
    if (this.variantForm.invalid) {
      this.variantForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const v = this.variantForm.value;

    const attributeValues: VariantAttributeValueRequest[] = this.attrValuesArray.controls
      .map((ctrl, i) => {
        const row = this.selectedAttrRows[i];
        if (!row) return null;
        const req: VariantAttributeValueRequest = { attributeId: row.attributeId };
        if (row.dataType === 'option') req.optionId = ctrl.get('optionId')?.value || null;
        else if (row.dataType === 'number') req.valueNumber = ctrl.get('valueNumber')?.value ?? null;
        else if (row.dataType === 'boolean') req.valueBool = ctrl.get('valueBool')?.value ?? null;
        else req.valueText = ctrl.get('valueText')?.value?.trim() || null;
        return req;
      })
      .filter((r): r is VariantAttributeValueRequest => r !== null);

    const req: CreateVariantRequest = {
      name: v.name.trim(),
      code: v.code.trim(),
      partNumber: v.partNumber?.trim() || null,
      oemNumber: v.oemNumber?.trim() || null,
      sku: v.sku?.trim() || null,
      barcode: v.barcode?.trim() || null,
      costPrice: v.costPrice ?? 0,
      sellingPrice: v.sellingPrice ?? 0,
      currency: v.currency || 'BDT',
      isActive: v.isActive,
      attributeValues
    };

    const op$ = this.isEditing && this.editingVariantId
      ? this.variantService.updateVariant(this.partId, this.editingVariantId, req)
      : this.variantService.createVariant(this.partId, req);

    op$.subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t(this.isEditing ? 'parts.variantManager.messages.variantUpdatedSummary' : 'parts.variantManager.messages.variantAddedSummary'),
          detail: this.i18n.t('parts.variantManager.messages.variantSavedDetail', { name: req.name })
        });
        this.isSubmitting = false;
        this.dialogVisible = false;
        this.loadVariants();
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: err?.error?.message || this.i18n.t('parts.variantManager.messages.saveFailed')
        });
        this.isSubmitting = false;
      }
    });
  }

  confirmDelete(variant: ProductVariantResponse): void {
    this.confirmationService.confirm({
      header: this.i18n.t('parts.variantManager.messages.deleteVariantHeader'),
      message: this.i18n.t('parts.variantManager.messages.deleteVariantMessage', { name: variant.name }),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.variantService.deleteVariant(this.partId, variant.id).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.variantManager.messages.deletedDetail', { name: variant.name }) });
            this.loadVariants();
          },
          error: (err) => {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.message || this.i18n.t('parts.variantManager.messages.deleteFailed') });
          }
        });
      }
    });
  }

  goToAttributeManager(): void {
    this.router.navigate(['/inventory/attribute-groups']);
  }

  private initForm(): void {
    this.variantForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      code: ['', [Validators.required, Validators.maxLength(50)]],
      partNumber: [''],
      oemNumber: [''],
      sku: [''],
      barcode: [''],
      costPrice: [null],
      sellingPrice: [null],
      currency: ['BDT'],
      isActive: [true],
      attributeValues: this.fb.array([])
    });
  }

  private addAttrRowForAttribute(attr: ProductAttribute): void {
    const group = this.attributeGroups.find(g => g.id === attr.attributeGroupId);
    this.selectedAttrRows.push({
      attributeId: attr.id,
      label: attr.name,
      groupName: group?.name ?? '',
      dataType: attr.dataType,
      unit: attr.unit,
      options: attr.options.map(o => ({ id: o.id, value: o.value }))
    });
    this.attrValuesArray.push(this.fb.group({
      optionId: [null],
      valueText: [''],
      valueNumber: [null],
      valueBool: [false]
    }));
  }
}
