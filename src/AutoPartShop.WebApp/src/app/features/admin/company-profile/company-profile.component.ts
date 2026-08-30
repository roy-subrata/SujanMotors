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
  templateUrl: './company-profile.component.html',
  styleUrl: './company-profile.component.scss',
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
