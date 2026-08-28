import { Component, OnInit, OnDestroy, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { PanelModule } from 'primeng/panel';
import { Select } from 'primeng/select';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import {
  ProductAttributeService,
  ProductAttributeGroup,
  ProductAttribute,
  AttributeOption
} from '../../services/product-attribute.service';
import { Router } from '@angular/router';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { StatusPillFilterComponent } from '@/shared/components/status-pill-filter/status-pill-filter.component';
import { MoreFiltersDialogComponent } from '@/shared/components/more-filters-dialog/more-filters-dialog.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

/** Attribute names/units are literal (persisted as the record's actual data on seed); only the button `label`/`icon` are translated UI chrome. */
type StarterTemplate = { label: string; icon: string; group: string; attrs: { name: string; code: string; dataType: string; unit: string }[] };

@Component({
  selector: 'app-attribute-group-manager',
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
    PanelModule,
    Select,
    ConfirmDialogModule,
    PageContainerComponent,
    PageHeaderComponent,
    FilterBarComponent,
    DataPaginationComponent,
    StatusPillFilterComponent,
    MoreFiltersDialogComponent,
    TranslatePipe
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './attribute-group-manager.component.html',
  styleUrls: ['./attribute-group-manager.component.css']
})
export class AttributeGroupManagerComponent implements OnInit, OnDestroy {
  private readonly service = inject(ProductAttributeService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  // Data
  groups: ProductAttributeGroup[] = [];
  isLoading = false;

  // Pagination
  totalRecords = 0;
  totalPages = 0;
  currentPage = 1;
  rows = 10;
  readonly Math = Math;

  get first(): number { return (this.currentPage - 1) * this.rows; }

  // Filter state
  searchTerm = '';
  filterStatus: boolean | null = null;
  moreFiltersVisible = false;

  /** String-keyed for <app-status-pill-filter>/<app-more-filters-dialog>; mapped to/from the boolean|null filterStatus. */
  statusOptions: { label: string; value: string }[] = [];

  get filterStatusValue(): string {
    return this.filterStatus === true ? 'true' : this.filterStatus === false ? 'false' : '';
  }

  onStatusFilterChange(value: string): void {
    this.filterStatus = value === 'true' ? true : value === 'false' ? false : null;
    this.onFilterChange();
  }

  // Group form
  groupForm!: FormGroup;
  editingGroupId: string | null = null;
  showGroupForm = false;

  // Attribute form
  attrForm!: FormGroup;
  editingAttrId: string | null = null;
  attrTargetGroupId: string | null = null;
  showAttrForm = false;

  // Option form
  optionValue = '';
  optionSortOrder = 0;
  optionTargetGroupId: string | null = null;
  optionTargetAttrId: string | null = null;
  showOptionInput = false;

  dataTypeOptions: { label: string; value: string }[] = [];
  scopeOptions: { label: string; value: string }[] = [];

  starterTemplates: StarterTemplate[] = [];

  isSeedingTemplate = false;

  private buildStatusOptions(): void {
    this.statusOptions = [
      { label: this.i18n.t('common.status.all'),      value: '' },
      { label: this.i18n.t('common.status.active'),   value: 'true' },
      { label: this.i18n.t('common.status.inactive'), value: 'false' }
    ];
  }

  private buildDataTypeOptions(): void {
    this.dataTypeOptions = [
      { label: this.i18n.t('parts.attributeGroupManager.dataTypeOption'), value: 'option' },
      { label: this.i18n.t('parts.attributeGroupManager.dataTypeText'), value: 'text' },
      { label: this.i18n.t('parts.attributeGroupManager.dataTypeNumber'), value: 'number' },
      { label: this.i18n.t('parts.attributeGroupManager.dataTypeBoolean'), value: 'boolean' }
    ];
  }

  private buildScopeOptions(): void {
    this.scopeOptions = [
      { label: this.i18n.t('parts.attributeGroupManager.scopeProduct'), value: 'product' },
      { label: this.i18n.t('parts.attributeGroupManager.scopeVariant'), value: 'variant' }
    ];
  }

  private buildStarterTemplates(): void {
    this.starterTemplates = [
      {
        label: this.i18n.t('parts.attributeGroupManager.templatePhysicalSpecs'), icon: 'pi-box',
        group: 'Physical Specs',
        attrs: [
          { name: 'Weight', code: 'WEIGHT', dataType: 'number', unit: 'kg' },
          { name: 'Width',  code: 'WIDTH',  dataType: 'number', unit: 'cm' },
          { name: 'Height', code: 'HEIGHT', dataType: 'number', unit: 'cm' },
          { name: 'Depth',  code: 'DEPTH',  dataType: 'number', unit: 'cm' }
        ]
      },
      {
        label: this.i18n.t('parts.attributeGroupManager.templateColorSize'), icon: 'pi-palette',
        group: 'Appearance',
        attrs: [
          { name: 'Color', code: 'COLOR', dataType: 'option', unit: '' },
          { name: 'Size',  code: 'SIZE',  dataType: 'option', unit: '' }
        ]
      },
      {
        label: this.i18n.t('parts.attributeGroupManager.templateVehicleFit'), icon: 'pi-car',
        group: 'Vehicle Compatibility',
        attrs: [
          { name: 'Compatible Make',  code: 'VEH_MAKE',  dataType: 'text', unit: '' },
          { name: 'Compatible Model', code: 'VEH_MODEL', dataType: 'text', unit: '' },
          { name: 'Compatible Year',  code: 'VEH_YEAR',  dataType: 'number', unit: '' }
        ]
      },
      {
        label: this.i18n.t('parts.attributeGroupManager.templateElectronics'), icon: 'pi-desktop',
        group: 'Technical Specs',
        attrs: [
          { name: 'RAM',     code: 'RAM',     dataType: 'number', unit: 'GB' },
          { name: 'Storage', code: 'STORAGE', dataType: 'number', unit: 'GB' },
          { name: 'Display', code: 'DISPLAY', dataType: 'number', unit: 'inch' }
        ]
      }
    ];
  }

  ngOnInit(): void {
    this.initForms();
    this.buildStatusOptions();
    this.buildDataTypeOptions();
    this.buildScopeOptions();
    this.buildStarterTemplates();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.buildStatusOptions();
      this.buildDataTypeOptions();
      this.buildScopeOptions();
      this.buildStarterTemplates();
    });
    // Debounce search input — fires API call 400ms after user stops typing
    this.search$.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadGroups();
    });
    this.loadGroups();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadGroups(): void {
    this.isLoading = true;
    this.service.getGroupsPaged({
      search: this.searchTerm.trim(),
      isActive: this.filterStatus,
      pageNumber: this.currentPage,
      pageSize: this.rows
    }).subscribe({
      next: (result) => {
        this.groups = result.data;
        this.totalRecords = result.pagination.totalCount;
        this.totalPages = result.pagination.totalPages;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  refreshData(): void {
    this.currentPage = 1;
    this.loadGroups();
  }

  onSearchInput(): void {
    this.search$.next(this.searchTerm);
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadGroups();
  }

  hasActiveFilters(): boolean {
    return !!this.searchTerm.trim() || this.filterStatus !== null;
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.filterStatus = null;
    this.currentPage = 1;
    this.loadGroups();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadGroups();
  }

  onPageSizeChange(size: number): void {
    this.rows = size;
    this.currentPage = 1;
    this.loadGroups();
  }

  // ── Templates ────────────────────────────────────────────────────────────

  seedTemplate(template: typeof this.starterTemplates[0]): void {
    this.isSeedingTemplate = true;
    this.service.createGroup({ name: template.group, sortOrder: this.totalRecords, isActive: true }).subscribe({
      next: (group) => {
        const attrRequests = template.attrs.map(a =>
          this.service.addAttribute(group.id, { name: a.name, code: a.code, dataType: a.dataType, unit: a.unit, isActive: true })
        );
        let completed = 0;
        attrRequests.forEach(req => req.subscribe({
          next: () => {
            completed++;
            if (completed === attrRequests.length) {
              this.isSeedingTemplate = false;
              this.messageService.add({
                severity: 'success',
                summary: this.i18n.t('parts.attributeGroupManager.messages.groupCreatedSummary'),
                detail: this.i18n.t('parts.attributeGroupManager.messages.groupCreatedDetail', { group: template.group, count: String(template.attrs.length) })
              });
              this.loadGroups();
            }
          },
          error: () => {
            completed++;
            if (completed === attrRequests.length) { this.isSeedingTemplate = false; this.loadGroups(); }
          }
        }));
      },
      error: (err) => {
        this.isSeedingTemplate = false;
        this.showError(err, this.i18n.t('parts.attributeGroupManager.messages.createGroupFailed'));
      }
    });
  }

  // ── Group ────────────────────────────────────────────────────────────────

  startAddGroup(): void {
    this.editingGroupId = null;
    this.groupForm.reset({ name: '', sortOrder: 0, isActive: true });
    this.showGroupForm = true;
  }

  startEditGroup(g: ProductAttributeGroup): void {
    this.editingGroupId = g.id;
    this.groupForm.patchValue({ name: g.name, sortOrder: g.sortOrder, isActive: g.isActive });
    this.showGroupForm = true;
  }

  saveGroup(): void {
    if (this.groupForm.invalid) { this.groupForm.markAllAsTouched(); return; }
    const v = this.groupForm.value;
    const op$ = this.editingGroupId
      ? this.service.updateGroup(this.editingGroupId, v)
      : this.service.createGroup(v);

    op$.subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.attributeGroupManager.messages.groupSavedDetail') });
        this.showGroupForm = false;
        this.loadGroups();
      },
      error: (err) => this.showError(err, this.i18n.t('parts.attributeGroupManager.messages.saveGroupFailed'))
    });
  }

  deleteGroup(g: ProductAttributeGroup): void {
    this.confirmationService.confirm({
      header: this.i18n.t('parts.attributeGroupManager.messages.deleteGroupHeader'),
      message: this.i18n.t('parts.attributeGroupManager.messages.deleteGroupMessage', { name: g.name }),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.service.deleteGroup(g.id).subscribe({
        next: () => { this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.attributeGroupManager.messages.groupDeletedDetail') }); this.loadGroups(); },
        error: (err) => this.showError(err, this.i18n.t('parts.attributeGroupManager.messages.deleteGroupFailed'))
      })
    });
  }

  // ── Attribute ─────────────────────────────────────────────────────────────

  startAddAttr(groupId: string): void {
    this.editingAttrId = null;
    this.attrTargetGroupId = groupId;
    this.attrForm.reset({ name: '', code: '', dataType: 'option', unit: '', isActive: true, scope: 'variant' });
    this.showAttrForm = true;
  }

  startEditAttr(groupId: string, attr: ProductAttribute): void {
    this.editingAttrId = attr.id;
    this.attrTargetGroupId = groupId;
    this.attrForm.patchValue({ name: attr.name, code: attr.code, dataType: attr.dataType, unit: attr.unit, isActive: attr.isActive, scope: attr.scope || 'variant' });
    this.showAttrForm = true;
  }

  saveAttr(): void {
    if (this.attrForm.invalid || !this.attrTargetGroupId) { this.attrForm.markAllAsTouched(); return; }
    const v = this.attrForm.value;
    const op$ = this.editingAttrId
      ? this.service.updateAttribute(this.attrTargetGroupId, this.editingAttrId, v)
      : this.service.addAttribute(this.attrTargetGroupId, v);

    op$.subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.attributeGroupManager.messages.attrSavedDetail') });
        this.showAttrForm = false;
        this.loadGroups();
      },
      error: (err) => this.showError(err, this.i18n.t('parts.attributeGroupManager.messages.saveAttrFailed'))
    });
  }

  deleteAttr(groupId: string, attr: ProductAttribute): void {
    this.confirmationService.confirm({
      header: this.i18n.t('parts.attributeGroupManager.messages.deleteAttrHeader'),
      message: this.i18n.t('parts.attributeGroupManager.messages.deleteAttrMessage', { name: attr.name }),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.service.deleteAttribute(groupId, attr.id).subscribe({
        next: () => { this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.attributeGroupManager.messages.attrDeletedDetail') }); this.loadGroups(); },
        error: (err) => this.showError(err, this.i18n.t('parts.attributeGroupManager.messages.deleteAttrFailed'))
      })
    });
  }

  // ── Options ───────────────────────────────────────────────────────────────

  startAddOption(groupId: string, attrId: string): void {
    this.optionTargetGroupId = groupId;
    this.optionTargetAttrId = attrId;
    this.optionValue = '';
    this.optionSortOrder = 0;
    this.showOptionInput = true;
  }

  saveOption(): void {
    if (!this.optionValue.trim() || !this.optionTargetGroupId || !this.optionTargetAttrId) return;
    this.service.addOption(this.optionTargetGroupId, this.optionTargetAttrId, { value: this.optionValue, sortOrder: this.optionSortOrder }).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('parts.attributeGroupManager.messages.optionAddedSummary'),
          detail: this.i18n.t('parts.attributeGroupManager.messages.optionAddedDetail', { value: this.optionValue })
        });
        this.showOptionInput = false;
        this.loadGroups();
      },
      error: (err) => this.showError(err, this.i18n.t('parts.attributeGroupManager.messages.addOptionFailed'))
    });
  }

  deleteOption(groupId: string, attrId: string, opt: AttributeOption): void {
    this.confirmationService.confirm({
      header: this.i18n.t('parts.attributeGroupManager.messages.deleteOptionHeader'),
      message: this.i18n.t('parts.attributeGroupManager.messages.deleteOptionMessage', { value: opt.value }),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.service.deleteOption(groupId, attrId, opt.id).subscribe({
        next: () => { this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.attributeGroupManager.messages.optionDeletedDetail') }); this.loadGroups(); },
        error: (err) => this.showError(err, this.i18n.t('parts.attributeGroupManager.messages.deleteOptionFailed'))
      })
    });
  }

  goBack(): void {
    this.router.navigate(['/inventory/parts']);
  }

  private initForms(): void {
    this.groupForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      sortOrder: [0],
      isActive: [true]
    });
    this.attrForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      code: ['', [Validators.required, Validators.maxLength(50)]],
      dataType: ['option', Validators.required],
      unit: [''],
      isActive: [true],
      scope: ['variant', Validators.required]
    });
  }

  private showError(err: any, fallback: string): void {
    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.message || fallback });
  }
}
