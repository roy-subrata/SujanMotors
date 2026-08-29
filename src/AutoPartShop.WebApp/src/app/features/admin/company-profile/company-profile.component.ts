import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService } from 'primeng/api';
import { AppSettingsService } from '../../../shared/services/app-settings.service';
import { AppBrandingService } from '../../../shared/services/app-branding.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { I18nService } from '@/shared/services/i18n.service';
import { forkJoin } from 'rxjs';

interface FieldDef {
  control: string;
  key: string;
  label: string;
  placeholder: string;
  hint: string;
  type?: 'text' | 'url' | 'textarea';
}

// label/placeholder/hint are i18n keys, resolved at render time via i18n.t() so they
// react to language switches (see identityFields loop in the template below).
const FIELDS: FieldDef[] = [
  { control: 'name',            key: 'SHOP_NAME',           label: 'companyProfile.identity.name',    placeholder: 'companyProfile.identity.namePlaceholder',    hint: 'companyProfile.identity.nameHint',    type: 'text' },
  { control: 'tagline',         key: 'SHOP_TAGLINE',         label: 'companyProfile.identity.tagline',  placeholder: 'companyProfile.identity.taglinePlaceholder',  hint: 'companyProfile.identity.taglineHint', type: 'text' },
  { control: 'address',         key: 'SHOP_ADDRESS',         label: 'companyProfile.identity.address',  placeholder: 'companyProfile.identity.addressPlaceholder',  hint: 'companyProfile.identity.addressHint', type: 'textarea' },
  { control: 'phone',           key: 'SHOP_PHONE',           label: 'companyProfile.identity.phone',    placeholder: 'companyProfile.identity.phonePlaceholder',    hint: 'companyProfile.identity.phoneHint',   type: 'text' },
  { control: 'email',           key: 'SHOP_EMAIL',           label: 'companyProfile.identity.email',    placeholder: 'companyProfile.identity.emailPlaceholder',    hint: 'companyProfile.identity.emailHint',   type: 'text' },
  { control: 'taxNo',           key: 'SHOP_TAX_NUMBER',      label: 'companyProfile.identity.taxNo',    placeholder: 'companyProfile.identity.taxNoPlaceholder',    hint: 'companyProfile.identity.taxNoHint',   type: 'text' },
  { control: 'logoUrl',         key: 'SHOP_LOGO_URL',        label: 'companyProfile.logo.url',          placeholder: 'companyProfile.logo.url',                     hint: 'companyProfile.logo.hint',            type: 'url' },
  { control: 'invoiceFooter',   key: 'INVOICE_FOOTER_TEXT',  label: 'companyProfile.footers.invoiceFooter', placeholder: 'companyProfile.footers.invoiceFooterPlaceholder', hint: 'companyProfile.footers.invoiceFooterHint', type: 'textarea' },
  { control: 'challanFooter',   key: 'CHALLAN_FOOTER_TEXT',  label: 'companyProfile.footers.challanFooter', placeholder: 'companyProfile.footers.challanFooterPlaceholder', hint: 'companyProfile.footers.challanFooterHint', type: 'textarea' },
];

@Component({
  selector: 'app-company-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    ToastModule,
    CardModule,
    DividerModule,
    CheckboxModule,
    PageContainerComponent,
    PageHeaderComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>

    <app-page-container>
      <app-page-header
        [title]="i18n.t('companyProfile.title')"
        [subtitle]="i18n.t('companyProfile.subtitle')">
      </app-page-header>

      <div class="w-full px-4 py-6">

      <div *ngIf="loading()" class="flex justify-center py-16">
        <i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i>
      </div>

      <form *ngIf="!loading()" [formGroup]="form" (ngSubmit)="save()">

        <!-- Application branding (white-label) -->
        <p-card styleClass="mb-4">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-desktop text-indigo-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">{{ i18n.t('companyProfile.branding.title') }}</h2>
            </div>
          </ng-template>

          <div class="flex flex-col gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1"><span class="required">*</span> {{ i18n.t('companyProfile.branding.appName') }}</label>
              <input pInputText formControlName="appName"
                [placeholder]="i18n.t('companyProfile.branding.appNamePlaceholder')" class="w-full" />
              <small class="text-gray-400">{{ i18n.t('companyProfile.branding.appNameHint') }}</small>
              <small class="p-error block mt-1" *ngIf="form.get('appName')?.invalid && form.get('appName')?.touched">
                {{ i18n.t('companyProfile.branding.appNameRequired') }}
              </small>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.branding.appLogoUrl') }}</label>
              <input pInputText formControlName="appLogoUrl"
                placeholder="https://... or assets/logo.png" class="w-full" />
              <small class="text-gray-400">{{ i18n.t('companyProfile.branding.appLogoUrlHint') }}</small>

              <div *ngIf="form.get('appLogoUrl')?.value && !form.get('appLogoUrl')?.value?.startsWith('assets')"
                   class="mt-3 p-3 bg-gray-50 border border-gray-200 rounded-lg inline-block">
                <img [src]="form.get('appLogoUrl')?.value" alt="preview"
                     class="max-h-16 object-contain" (error)="onLogoError($event)">
              </div>
            </div>
          </div>
        </p-card>

        <!-- Identity -->
        <p-card styleClass="mb-4">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-building text-blue-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">{{ i18n.t('companyProfile.identity.title') }}</h2>
            </div>
          </ng-template>

          <div class="flex flex-col gap-4">
            <ng-container *ngFor="let f of identityFields">
              <div>
                <label class="block text-sm font-medium text-gray-600 mb-1">
                  <span class="required" *ngIf="f.control === 'name'">*</span> {{ i18n.t(f.label) }}
                </label>
                <textarea *ngIf="f.type === 'textarea'"
                  pTextarea
                  [formControlName]="f.control"
                  [placeholder]="i18n.t(f.placeholder)"
                  rows="2"
                  class="w-full">
                </textarea>
                <input *ngIf="f.type !== 'textarea'"
                  pInputText
                  [formControlName]="f.control"
                  [placeholder]="i18n.t(f.placeholder)"
                  [type]="f.type === 'url' ? 'url' : 'text'"
                  class="w-full" />
                <small class="text-gray-400">{{ i18n.t(f.hint) }}</small>
                <small class="p-error block mt-1" *ngIf="form.get(f.control)?.invalid && form.get(f.control)?.touched">
                  {{ f.control === 'email' ? i18n.t('companyProfile.identity.emailInvalid') : i18n.t('common.messages.fieldRequired', { field: i18n.t(f.label) }) }}
                </small>
              </div>
            </ng-container>
          </div>
        </p-card>

        <!-- Logo -->
        <p-card styleClass="mb-4">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-image text-purple-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">{{ i18n.t('companyProfile.logo.title') }}</h2>
            </div>
          </ng-template>

          <div>
            <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.logo.url') }}</label>
            <input
              pInputText
              formControlName="logoUrl"
              placeholder="https://... or assets/logo.png"
              class="w-full" />
            <small class="text-gray-400">
              {{ i18n.t('companyProfile.logo.hint') }}
            </small>

            <!-- Live preview -->
            <div *ngIf="form.get('logoUrl')?.value && !form.get('logoUrl')?.value?.startsWith('assets')"
                 class="mt-3 p-3 bg-gray-50 border border-gray-200 rounded-lg inline-block">
              <img [src]="form.get('logoUrl')?.value" alt="preview"
                   class="max-h-16 object-contain"
                   (error)="onLogoError($event)">
            </div>
          </div>
        </p-card>

        <!-- Document footers -->
        <p-card styleClass="mb-6">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-file-edit text-green-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">{{ i18n.t('companyProfile.footers.title') }}</h2>
            </div>
          </ng-template>

          <div class="flex flex-col gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.footers.invoiceFooter') }}</label>
              <textarea pTextarea formControlName="invoiceFooter"
                [placeholder]="i18n.t('companyProfile.footers.invoiceFooterPlaceholder')"
                rows="2" class="w-full">
              </textarea>
              <small class="text-gray-400">{{ i18n.t('companyProfile.footers.invoiceFooterHint') }}</small>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.footers.challanFooter') }}</label>
              <textarea pTextarea formControlName="challanFooter"
                [placeholder]="i18n.t('companyProfile.footers.challanFooterPlaceholder')"
                rows="2" class="w-full">
              </textarea>
              <small class="text-gray-400">{{ i18n.t('companyProfile.footers.challanFooterHint') }}</small>
            </div>
          </div>
        </p-card>

        <!-- Tax & VAT -->
        <p-card styleClass="mb-4">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-percentage text-orange-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">{{ i18n.t('companyProfile.tax.title') }}</h2>
            </div>
          </ng-template>

          <div class="flex flex-col gap-4">
            <div class="flex items-center gap-2">
              <p-checkbox formControlName="vatEnabled" [binary]="true" inputId="vatEnabled"></p-checkbox>
              <label for="vatEnabled" class="text-sm font-medium text-gray-600">{{ i18n.t('companyProfile.tax.vatEnabled') }}</label>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.tax.vatRate') }}</label>
              <input pInputText type="number" min="0" max="100" step="0.01"
                formControlName="vatRate" placeholder="15" class="w-full" />
              <small class="text-gray-400">{{ i18n.t('companyProfile.tax.vatRateHint') }}</small>
            </div>
          </div>
        </p-card>

        <!-- Document Numbering -->
        <p-card styleClass="mb-4">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-hashtag text-cyan-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">{{ i18n.t('companyProfile.numbering.title') }}</h2>
            </div>
          </ng-template>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.numbering.invoicePrefix') }}</label>
              <input pInputText formControlName="invoiceNumberPrefix" placeholder="INV" class="w-full" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.numbering.salesOrderPrefix') }}</label>
              <input pInputText formControlName="salesOrderNumberPrefix" placeholder="SO" class="w-full" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.numbering.purchaseOrderPrefix') }}</label>
              <input pInputText formControlName="purchaseOrderNumberPrefix" placeholder="PO" class="w-full" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.numbering.quotationPrefix') }}</label>
              <input pInputText formControlName="quotationNumberPrefix" placeholder="QT" class="w-full" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">{{ i18n.t('companyProfile.numbering.skuPrefix') }}</label>
              <input pInputText formControlName="skuPrefix" placeholder="SKU" class="w-full" />
            </div>
          </div>
          <small class="text-gray-400 block mt-2">{{ i18n.t('companyProfile.numbering.hint') }}</small>
        </p-card>

        <!-- Live preview strip -->
        <div class="bg-white border border-gray-200 rounded-lg p-5 mb-6 shadow-sm">
          <p class="text-xs text-gray-400 uppercase font-semibold mb-3 tracking-wide">{{ i18n.t('companyProfile.preview.label') }}</p>
          <div class="flex items-start gap-4">
            <img *ngIf="form.get('logoUrl')?.value && !form.get('logoUrl')?.value?.startsWith('assets')"
                 [src]="form.get('logoUrl')?.value" alt=""
                 class="max-h-12 object-contain">
            <div>
              <div class="text-xl font-bold text-gray-800">{{ form.get('name')?.value || i18n.t('companyProfile.preview.placeholderName') }}</div>
              <div class="text-sm text-gray-500 italic" *ngIf="form.get('tagline')?.value">{{ form.get('tagline')?.value }}</div>
              <div class="text-xs text-gray-400 mt-1">{{ form.get('address')?.value }}</div>
              <div class="text-xs text-gray-400">{{ form.get('phone')?.value }}  {{ form.get('email')?.value }}</div>
              <div class="text-xs text-gray-400" *ngIf="form.get('taxNo')?.value">{{ i18n.t('companyProfile.preview.taxRegPrefix') }} {{ form.get('taxNo')?.value }}</div>
            </div>
          </div>
        </div>

        <div class="flex justify-end">
          <button
            pButton
            type="submit"
            [label]="i18n.t('companyProfile.saveButton')"
            icon="pi pi-save"
            [loading]="saving()"
            [disabled]="form.invalid || saving()"
            class="p-button-success">
          </button>
        </div>

      </form>
      </div>
    </app-page-container>
  `,
  styles: [`
    /* This page is built with Tailwind gray-scale utility classes, which are
       static (they don't flip under the app's .app-dark class the way the
       --color-* tokens from assets/_data-page.scss do). Re-point the neutral
       text/background/border utilities actually used above at those tokens so
       the page stays readable in dark mode; the section-icon accent colors
       (indigo/blue/purple/green-500) are decorative and stay fixed.  */
    :host ::ng-deep {
      .text-gray-400 { color: var(--color-text-muted) !important; }
      .text-gray-500 { color: var(--color-text-muted) !important; }
      .text-gray-600 { color: var(--color-text-secondary) !important; }
      .text-gray-700 { color: var(--color-text-primary) !important; }
      .text-gray-800 { color: var(--color-text-primary) !important; }
      .bg-white { background-color: var(--color-bg-primary) !important; }
      .bg-gray-50 { background-color: var(--color-bg-secondary) !important; }
      .border-gray-200 { border-color: var(--color-border) !important; }
    }
  `]
})
export class CompanyProfileComponent implements OnInit {
  private readonly settingsService = inject(AppSettingsService);
  private readonly branding        = inject(AppBrandingService);
  private readonly messageService  = inject(MessageService);
  private readonly fb              = inject(FormBuilder);
  readonly i18n                    = inject(I18nService);

  loading = signal(true);
  saving  = signal(false);

  readonly identityFields: FieldDef[] = FIELDS.filter(
    f => !['logoUrl', 'invoiceFooter', 'challanFooter'].includes(f.control)
  );

  form: FormGroup = this.fb.group({
    appName:       ['', Validators.required],
    appLogoUrl:    [''],
    name:          ['', Validators.required],
    tagline:       [''],
    address:       [''],
    phone:         [''],
    email:         ['', Validators.email],
    taxNo:         [''],
    logoUrl:       [''],
    invoiceFooter: [''],
    challanFooter: [''],
    vatEnabled:              [false],
    vatRate:                 [15],
    invoiceNumberPrefix:     ['INV'],
    salesOrderNumberPrefix:  ['SO'],
    purchaseOrderNumberPrefix: ['PO'],
    quotationNumberPrefix:   ['QT'],
    skuPrefix:               ['SKU'],
  });

  ngOnInit(): void {
    forkJoin({
      profile: this.settingsService.getShopProfile(),
      tax: this.settingsService.getByCategory('TAX'),
      numbering: this.settingsService.getByCategory('NUMBERING'),
    }).subscribe({
      next: ({ profile: p, tax, numbering }) => {
        const taxMap = Object.fromEntries(tax.map(s => [s.key, s.value]));
        const numberingMap = Object.fromEntries(numbering.map(s => [s.key, s.value]));
        this.form.patchValue({
          appName:       p.appName,
          appLogoUrl:    p.appLogoUrl,
          name:          p.name,
          tagline:       p.tagline,
          address:       p.address,
          phone:         p.phone,
          email:         p.email,
          taxNo:         p.taxNo,
          logoUrl:       p.logoUrl,
          invoiceFooter: p.invoiceFooterText,
          challanFooter: p.challanFooterText,
          vatEnabled:              taxMap['VAT_ENABLED'] === 'true',
          vatRate:                 taxMap['VAT_RATE'] ? Number(taxMap['VAT_RATE']) : 15,
          invoiceNumberPrefix:     numberingMap['INVOICE_NUMBER_PREFIX'] || 'INV',
          salesOrderNumberPrefix:  numberingMap['SALES_ORDER_NUMBER_PREFIX'] || 'SO',
          purchaseOrderNumberPrefix: numberingMap['PURCHASE_ORDER_NUMBER_PREFIX'] || 'PO',
          quotationNumberPrefix:   numberingMap['QUOTATION_NUMBER_PREFIX'] || 'QT',
          skuPrefix:               numberingMap['SKU_PREFIX'] || 'SKU',
        });
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('companyProfile.messages.loadFailed') });
        this.loading.set(false);
      }
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);

    const v = this.form.value;
    // Application brand (white-label) lives in the BRANDING category; the business
    // identity used on documents stays in BUSINESS.
    const keyMap: Record<string, { key: string; category: string }> = {
      appName:       { key: 'APP_NAME',           category: 'BRANDING' },
      appLogoUrl:    { key: 'APP_LOGO_URL',        category: 'BRANDING' },
      name:          { key: 'SHOP_NAME',           category: 'BUSINESS' },
      tagline:       { key: 'SHOP_TAGLINE',        category: 'BUSINESS' },
      address:       { key: 'SHOP_ADDRESS',        category: 'BUSINESS' },
      phone:         { key: 'SHOP_PHONE',          category: 'BUSINESS' },
      email:         { key: 'SHOP_EMAIL',          category: 'BUSINESS' },
      taxNo:         { key: 'SHOP_TAX_NUMBER',      category: 'BUSINESS' },
      logoUrl:       { key: 'SHOP_LOGO_URL',        category: 'BUSINESS' },
      invoiceFooter: { key: 'INVOICE_FOOTER_TEXT',  category: 'BUSINESS' },
      challanFooter: { key: 'CHALLAN_FOOTER_TEXT',  category: 'BUSINESS' },
      vatEnabled:                { key: 'VAT_ENABLED',                category: 'TAX' },
      vatRate:                   { key: 'VAT_RATE',                   category: 'TAX' },
      invoiceNumberPrefix:       { key: 'INVOICE_NUMBER_PREFIX',       category: 'NUMBERING' },
      salesOrderNumberPrefix:    { key: 'SALES_ORDER_NUMBER_PREFIX',   category: 'NUMBERING' },
      purchaseOrderNumberPrefix: { key: 'PURCHASE_ORDER_NUMBER_PREFIX', category: 'NUMBERING' },
      quotationNumberPrefix:     { key: 'QUOTATION_NUMBER_PREFIX',     category: 'NUMBERING' },
      skuPrefix:                 { key: 'SKU_PREFIX',                 category: 'NUMBERING' },
    };

    const dataTypeMap: Record<string, string> = {
      vatEnabled: 'BOOL',
      vatRate: 'DECIMAL',
    };

    const updates = Object.entries(keyMap).map(([ctrl, { key, category }]) =>
      this.settingsService.update(key, { value: String(v[ctrl] ?? ''), dataType: dataTypeMap[ctrl] ?? 'STRING', category, isSystemSetting: true })
    );

    forkJoin(updates).subscribe({
      next: () => {
        // Re-apply the live brand (tab title, sidebar, storefront) without a reload.
        this.branding.refresh();
        this.messageService.add({ severity: 'success', summary: this.i18n.t('companyProfile.messages.savedSummary'), detail: this.i18n.t('companyProfile.messages.savedDetail') });
        this.saving.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('companyProfile.messages.saveFailed') });
        this.saving.set(false);
      }
    });
  }

  onLogoError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }
}
