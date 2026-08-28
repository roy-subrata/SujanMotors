import { Component, Input, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { Select } from 'primeng/select';
import { MessageService } from 'primeng/api';
import {
  PartService,
  ProductAttributeValueRequest,
  ProductAttributeValueResponse
} from '../../services/part.service';
import {
  ProductAttributeService,
  ProductAttributeGroup,
  ProductAttribute
} from '../../services/product-attribute.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

interface AttrRow {
  attributeId: string;
  label: string;
  groupName: string;
  dataType: string;
  unit: string;
  options: { id: string; value: string }[];
}

/**
 * Inline editor for a product's own (scope === 'product') attribute values —
 * e.g. Material/Brand-style attributes that apply to the whole product, as
 * opposed to per-variant attributes (see product-variant-manager). Saving is
 * a full replace via PUT /products/{id}/attribute-values, independent from
 * the surrounding part-form's own submit.
 */
@Component({
  selector: 'app-product-attribute-values-manager',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    CheckboxModule,
    TooltipModule,
    ToastModule,
    Select,
    TranslatePipe
  ],
  providers: [MessageService],
  templateUrl: './product-attribute-values-manager.component.html',
  styleUrls: ['./product-attribute-values-manager.component.scss']
})
export class ProductAttributeValuesManagerComponent implements OnInit, OnChanges {
  @Input() partId!: string;
  @Input() isViewMode = false;
  /** Prefetched from the product's own `attributeValues` field — part-form already loads the
   *  full product, so this avoids a redundant GET just to seed this editor. */
  @Input() initialAttributeValues: ProductAttributeValueResponse[] | null = null;

  private readonly partService = inject(PartService);
  private readonly attributeService = inject(ProductAttributeService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);

  attributeGroups: ProductAttributeGroup[] = [];
  allAttributes: ProductAttribute[] = [];

  // Attribute rows currently in the form
  selectedAttrRows: AttrRow[] = [];
  // The attribute the user is about to add
  attrToAdd: string | null = null;

  form!: FormGroup;
  isLoadingAttrs = false;
  isSaving = false;

  private savedSnapshot = '';

  ngOnInit(): void {
    this.initForm();
    this.loadAttributeGroups();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Attribute definitions may already be loaded by the time a later value arrives
    // (e.g. the parent re-binds initialAttributeValues after this component initializes).
    if (changes['initialAttributeValues'] && !changes['initialAttributeValues'].firstChange && this.allAttributes.length > 0) {
      this.populateFromValues(this.initialAttributeValues ?? []);
    }
  }

  get valuesArray(): FormArray {
    return this.form.get('attributeValues') as FormArray;
  }

  /** Product-scoped attributes NOT yet added to this form */
  get availableAttributes(): { label: string; value: string }[] {
    const alreadyAdded = new Set(this.selectedAttrRows.map(r => r.attributeId));
    return this.allAttributes
      .filter(a => a.isActive && !alreadyAdded.has(a.id))
      .map(a => ({ label: `${a.name} (${a.dataType})`, value: a.id }));
  }

  get isDirty(): boolean {
    return this.snapshot() !== this.savedSnapshot;
  }

  loadAttributeGroups(): void {
    this.isLoadingAttrs = true;
    this.attributeService.getAllGroups().subscribe({
      next: (groups) => {
        this.attributeGroups = groups.filter(g => g.isActive);
        // Only product-scoped attributes can be attached here — variant-scoped attributes
        // belong on <app-product-variant-manager> instead.
        this.allAttributes = groups.flatMap(g => g.attributes.filter(a => a.isActive && a.scope === 'product'));
        this.isLoadingAttrs = false;
        this.populateFromValues(this.initialAttributeValues ?? []);
      },
      error: () => { this.isLoadingAttrs = false; }
    });
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
    this.valuesArray.removeAt(index);
  }

  save(): void {
    if (!this.partId || this.isSaving) return;
    this.isSaving = true;
    this.partService.saveAttributeValues(this.partId, this.buildRequest()).subscribe({
      next: () => {
        this.isSaving = false;
        this.savedSnapshot = this.snapshot();
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('parts.productAttributeValuesManager.messages.savedDetail')
        });
      },
      error: (err) => {
        this.isSaving = false;
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: err?.error?.message || this.i18n.t('parts.productAttributeValuesManager.messages.saveFailed')
        });
      }
    });
  }

  reset(): void {
    this.populateFromValues(this.initialAttributeValues ?? []);
  }

  goToAttributeManager(): void {
    this.router.navigate(['/inventory/attribute-groups']);
  }

  /**
   * Rows currently entered but not yet saved — used by the parent part-form when creating a
   * brand-new product (no partId yet, so this component's own save() has nothing to PUT to).
   * The parent reads this right before/after createPart() succeeds and saves it itself.
   */
  getPendingRequest(): ProductAttributeValueRequest[] {
    return this.buildRequest();
  }

  private initForm(): void {
    this.form = this.fb.group({ attributeValues: this.fb.array([]) });
  }

  private populateFromValues(values: ProductAttributeValueResponse[]): void {
    this.selectedAttrRows = [];
    this.valuesArray.clear();
    values.forEach(v => {
      const attr = this.allAttributes.find(a => a.id === v.attributeId);
      if (!attr) return;
      this.addAttrRowForAttribute(attr);
      const ctrl = this.valuesArray.at(this.valuesArray.length - 1);
      if (attr.dataType === 'option') ctrl.patchValue({ optionId: v.optionId });
      else if (attr.dataType === 'number') ctrl.patchValue({ valueNumber: v.valueNumber });
      else if (attr.dataType === 'boolean') ctrl.patchValue({ valueBool: v.valueBool ?? false });
      else ctrl.patchValue({ valueText: v.valueText || '' });
    });
    this.savedSnapshot = this.snapshot();
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
    this.valuesArray.push(this.fb.group({
      optionId: [null],
      valueText: [''],
      valueNumber: [null],
      valueBool: [false]
    }));
  }

  private buildRequest(): ProductAttributeValueRequest[] {
    return this.valuesArray.controls
      .map((ctrl, i) => {
        const row = this.selectedAttrRows[i];
        if (!row) return null;
        const req: ProductAttributeValueRequest = { attributeId: row.attributeId };
        if (row.dataType === 'option') req.optionId = ctrl.get('optionId')?.value || null;
        else if (row.dataType === 'number') req.valueNumber = ctrl.get('valueNumber')?.value ?? null;
        else if (row.dataType === 'boolean') req.valueBool = ctrl.get('valueBool')?.value ?? null;
        else req.valueText = ctrl.get('valueText')?.value?.trim() || null;
        return req;
      })
      .filter((r): r is ProductAttributeValueRequest => r !== null);
  }

  private snapshot(): string {
    return JSON.stringify(this.buildRequest());
  }
}
