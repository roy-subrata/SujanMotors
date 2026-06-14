import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { MessageService } from 'primeng/api';
import { AppSettingsService } from '../../../shared/services/app-settings.service';
import { AppBrandingService } from '../../../shared/services/app-branding.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { forkJoin } from 'rxjs';

interface FieldDef {
  control: string;
  key: string;
  label: string;
  placeholder: string;
  hint: string;
  type?: 'text' | 'url' | 'textarea';
}

const FIELDS: FieldDef[] = [
  { control: 'name',            key: 'SHOP_NAME',           label: 'Business Name',    placeholder: 'e.g. Sujan Motors',           hint: 'Appears as the heading on all documents.',    type: 'text' },
  { control: 'tagline',         key: 'SHOP_TAGLINE',         label: 'Tagline',          placeholder: 'e.g. Your trusted parts supplier', hint: 'Optional subtitle below the business name.', type: 'text' },
  { control: 'address',         key: 'SHOP_ADDRESS',         label: 'Address',          placeholder: 'Street, City, Country',       hint: 'Full address printed on documents.',           type: 'textarea' },
  { control: 'phone',           key: 'SHOP_PHONE',           label: 'Phone',            placeholder: '+880 1XXXXXXXXX',             hint: 'Primary contact number.',                     type: 'text' },
  { control: 'email',           key: 'SHOP_EMAIL',           label: 'Email',            placeholder: 'info@yourshop.com',           hint: 'Business email shown on documents.',           type: 'text' },
  { control: 'taxNo',           key: 'SHOP_TAX_NUMBER',      label: 'Tax / VAT No.',    placeholder: 'e.g. VAT-123456789',          hint: 'Tax registration number (leave blank to hide).', type: 'text' },
  { control: 'logoUrl',         key: 'SHOP_LOGO_URL',        label: 'Logo URL',         placeholder: 'https://... or assets/logo.png', hint: 'HTTPS image URL or relative path. Leave blank or use assets/logo.png to use the default logo.', type: 'url' },
  { control: 'invoiceFooter',   key: 'INVOICE_FOOTER_TEXT',  label: 'Invoice Footer',   placeholder: 'Thank you for your business!', hint: 'Printed at the bottom of every invoice.',     type: 'textarea' },
  { control: 'challanFooter',   key: 'CHALLAN_FOOTER_TEXT',  label: 'Challan Footer',   placeholder: 'Goods once dispatched…',      hint: 'Printed at the bottom of every delivery challan.', type: 'textarea' },
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
    PageContainerComponent,
    PageHeaderComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>

    <app-page-container>
      <app-page-header
        title="Company Profile"
        subtitle="Printed on every invoice, delivery challan, and account statement"
        [breadcrumb]="[{ label: 'Admin' }, { label: 'Company Profile' }]">
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
              <h2 class="text-lg font-semibold text-gray-700 m-0">Application Branding</h2>
            </div>
          </ng-template>

          <div class="flex flex-col gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">Application Name</label>
              <input pInputText formControlName="appName"
                placeholder="e.g. Auto Part Shop" class="w-full" />
              <small class="text-gray-400">Shown in the browser tab, sidebar, login page, and storefront. Independent of the business name on documents.</small>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">Application Logo URL</label>
              <input pInputText formControlName="appLogoUrl"
                placeholder="https://... or assets/logo.png" class="w-full" />
              <small class="text-gray-400">HTTPS image URL or relative path. Leave blank or use <code>assets/logo.png</code> for the built-in icon.</small>

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
              <h2 class="text-lg font-semibold text-gray-700 m-0">Business Identity</h2>
            </div>
          </ng-template>

          <div class="flex flex-col gap-4">
            <ng-container *ngFor="let f of identityFields">
              <div>
                <label class="block text-sm font-medium text-gray-600 mb-1">{{ f.label }}</label>
                <textarea *ngIf="f.type === 'textarea'"
                  pTextarea
                  [formControlName]="f.control"
                  [placeholder]="f.placeholder"
                  rows="2"
                  class="w-full">
                </textarea>
                <input *ngIf="f.type !== 'textarea'"
                  pInputText
                  [formControlName]="f.control"
                  [placeholder]="f.placeholder"
                  [type]="f.type === 'url' ? 'url' : 'text'"
                  class="w-full" />
                <small class="text-gray-400">{{ f.hint }}</small>
              </div>
            </ng-container>
          </div>
        </p-card>

        <!-- Logo -->
        <p-card styleClass="mb-4">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-image text-purple-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">Logo</h2>
            </div>
          </ng-template>

          <div>
            <label class="block text-sm font-medium text-gray-600 mb-1">Logo URL</label>
            <input
              pInputText
              formControlName="logoUrl"
              placeholder="https://... or assets/logo.png"
              class="w-full" />
            <small class="text-gray-400">
              Paste an HTTPS image URL. Leave blank or use <code>assets/logo.png</code> to use the built-in default.
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
              <h2 class="text-lg font-semibold text-gray-700 m-0">Document Footers</h2>
            </div>
          </ng-template>

          <div class="flex flex-col gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">Invoice Footer</label>
              <textarea pTextarea formControlName="invoiceFooter"
                placeholder="Thank you for your business!"
                rows="2" class="w-full">
              </textarea>
              <small class="text-gray-400">Printed at the bottom of every invoice.</small>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-1">Delivery Challan Footer</label>
              <textarea pTextarea formControlName="challanFooter"
                placeholder="Goods once dispatched will not be accepted back without prior notice."
                rows="2" class="w-full">
              </textarea>
              <small class="text-gray-400">Printed at the bottom of every delivery challan.</small>
            </div>
          </div>
        </p-card>

        <!-- Live preview strip -->
        <div class="bg-white border border-gray-200 rounded-lg p-5 mb-6 shadow-sm">
          <p class="text-xs text-gray-400 uppercase font-semibold mb-3 tracking-wide">Document header preview</p>
          <div class="flex items-start gap-4">
            <img *ngIf="form.get('logoUrl')?.value && !form.get('logoUrl')?.value?.startsWith('assets')"
                 [src]="form.get('logoUrl')?.value" alt=""
                 class="max-h-12 object-contain">
            <div>
              <div class="text-xl font-bold text-gray-800">{{ form.get('name')?.value || 'Your Company Name' }}</div>
              <div class="text-sm text-gray-500 italic" *ngIf="form.get('tagline')?.value">{{ form.get('tagline')?.value }}</div>
              <div class="text-xs text-gray-400 mt-1">{{ form.get('address')?.value }}</div>
              <div class="text-xs text-gray-400">{{ form.get('phone')?.value }}  {{ form.get('email')?.value }}</div>
              <div class="text-xs text-gray-400" *ngIf="form.get('taxNo')?.value">Tax Reg: {{ form.get('taxNo')?.value }}</div>
            </div>
          </div>
        </div>

        <div class="flex justify-end">
          <button
            pButton
            type="submit"
            label="Save Profile"
            icon="pi pi-save"
            [loading]="saving()"
            [disabled]="form.invalid || saving()"
            class="p-button-success">
          </button>
        </div>

      </form>
      </div>
    </app-page-container>
  `
})
export class CompanyProfileComponent implements OnInit {
  private readonly settingsService = inject(AppSettingsService);
  private readonly branding        = inject(AppBrandingService);
  private readonly messageService  = inject(MessageService);
  private readonly fb              = inject(FormBuilder);

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
  });

  ngOnInit(): void {
    this.settingsService.getShopProfile().subscribe({
      next: p => {
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
        });
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load company profile' });
        this.loading.set(false);
      }
    });
  }

  save(): void {
    if (this.form.invalid) return;
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
    };

    const updates = Object.entries(keyMap).map(([ctrl, { key, category }]) =>
      this.settingsService.update(key, { value: v[ctrl] ?? '', dataType: 'STRING', category, isSystemSetting: true })
    );

    forkJoin(updates).subscribe({
      next: () => {
        // Re-apply the live brand (tab title, sidebar, storefront) without a reload.
        this.branding.refresh();
        this.messageService.add({ severity: 'success', summary: 'Saved', detail: 'Company profile updated. All documents will reflect the new information immediately.' });
        this.saving.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to save one or more settings' });
        this.saving.set(false);
      }
    });
  }

  onLogoError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }
}
