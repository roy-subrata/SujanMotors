import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { MessageService } from 'primeng/api';
import { AppSettingsService } from '../../../shared/services/app-settings.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

interface PolicyKeys {
  SHOP_FREE_SHIPPING_ENABLED: string;
  SHOP_FREE_SHIPPING_THRESHOLD: string;
  SHOP_FREE_SHIPPING_CURRENCY: string;
  SHOP_RETURN_POLICY_DAYS: string;
  SHOP_RETURN_POLICY_TEXT: string;
}

@Component({
  selector: 'app-shop-policies',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    ToggleSwitchModule,
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
        title="Shop Policies"
        subtitle="Configure storefront policies shown to customers on product pages">
      </app-page-header>

      <div class="w-full px-4 py-6">

      <div *ngIf="loading()" class="flex justify-center py-16">
        <i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i>
      </div>

      <form *ngIf="!loading()" [formGroup]="form" (ngSubmit)="save()">

        <!-- Free Shipping -->
        <p-card styleClass="mb-4">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-truck text-blue-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">Free Shipping</h2>
            </div>
          </ng-template>

          <div class="flex items-center justify-between mb-5">
            <div>
              <p class="font-medium text-gray-700 m-0">Enable free shipping badge</p>
              <p class="text-sm text-gray-400 mt-1 m-0">Show the free shipping offer on product detail pages</p>
            </div>
            <p-toggleswitch formControlName="freeShippingEnabled"></p-toggleswitch>
          </div>

          <p-divider></p-divider>

          <div class="grid grid-cols-2 gap-4 mt-4" [class.opacity-40]="!form.get('freeShippingEnabled')?.value" [class.pointer-events-none]="!form.get('freeShippingEnabled')?.value">
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-2"><span class="required">*</span> Minimum order threshold</label>
              <p-inputNumber
                formControlName="freeShippingThreshold"
                [min]="0"
                [maxFractionDigits]="0"
                [showButtons]="false"
                styleClass="w-full"
                inputStyleClass="w-full">
              </p-inputNumber>
              <small class="text-gray-400">Orders above this amount qualify for free shipping</small>
              <small class="p-error block mt-1" *ngIf="form.get('freeShippingThreshold')?.invalid && form.get('freeShippingThreshold')?.touched">
                Enter a threshold of 0 or more
              </small>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-2"><span class="required">*</span> Currency code</label>
              <input
                pInputText
                formControlName="freeShippingCurrency"
                placeholder="e.g. BDT"
                maxlength="5"
                class="w-full"
                style="text-transform:uppercase" />
              <small class="text-gray-400">Currency label displayed next to the threshold</small>
              <small class="p-error block mt-1" *ngIf="form.get('freeShippingCurrency')?.invalid && form.get('freeShippingCurrency')?.touched">
                Currency code is required
              </small>
            </div>
          </div>
        </p-card>

        <!-- Return Policy -->
        <p-card styleClass="mb-6">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-replay text-green-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">Return Policy</h2>
            </div>
          </ng-template>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-2"><span class="required">*</span> Return window (days)</label>
              <p-inputNumber
                formControlName="returnPolicyDays"
                [min]="1"
                [max]="365"
                [showButtons]="true"
                buttonLayout="horizontal"
                spinnerMode="horizontal"
                incrementButtonIcon="pi pi-plus"
                decrementButtonIcon="pi pi-minus"
                styleClass="w-full"
                inputStyleClass="w-full text-center">
              </p-inputNumber>
              <small class="p-error block mt-1" *ngIf="form.get('returnPolicyDays')?.invalid && form.get('returnPolicyDays')?.touched">
                Enter a value between 1 and 365
              </small>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-600 mb-2"><span class="required">*</span> Display label</label>
              <input
                pInputText
                formControlName="returnPolicyText"
                placeholder="e.g. 30-day return policy"
                class="w-full" />
              <small class="text-gray-400">Text shown on the product page badge</small>
              <small class="p-error block mt-1" *ngIf="form.get('returnPolicyText')?.invalid && form.get('returnPolicyText')?.touched">
                Display label is required
              </small>
            </div>
          </div>
        </p-card>

        <!-- Preview -->
        <div class="bg-gray-50 border border-gray-200 rounded-lg p-4 mb-6">
          <p class="text-xs text-gray-400 uppercase font-semibold mb-3">Preview — product page badges</p>
          <div class="flex flex-wrap gap-4">
            <div class="flex items-center gap-2 text-sm text-gray-600" *ngIf="form.get('freeShippingEnabled')?.value">
              <i class="pi pi-truck text-blue-500"></i>
              <span>Free shipping on orders over {{ form.get('freeShippingCurrency')?.value || 'BDT' }} {{ form.get('freeShippingThreshold')?.value | number:'1.0-0' }}</span>
            </div>
            <div class="flex items-center gap-2 text-sm text-gray-600">
              <i class="pi pi-replay text-green-500"></i>
              <span>{{ form.get('returnPolicyText')?.value || '30-day return policy' }}</span>
            </div>
          </div>
        </div>

        <!-- Save -->
        <div class="flex justify-end">
          <button
            pButton
            type="submit"
            label="Save Policies"
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
       the page stays readable in dark mode. */
    :host ::ng-deep {
      .text-gray-400 { color: var(--color-text-muted) !important; }
      .text-gray-600 { color: var(--color-text-secondary) !important; }
      .text-gray-700 { color: var(--color-text-primary) !important; }
      .bg-gray-50 { background-color: var(--color-bg-secondary) !important; }
      .border-gray-200 { border-color: var(--color-border) !important; }
    }
  `]
})
export class ShopPoliciesComponent implements OnInit {
  private readonly settingsService = inject(AppSettingsService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);

  loading = signal(true);
  saving = signal(false);

  form: FormGroup = this.fb.group({
    freeShippingEnabled:   [true],
    freeShippingThreshold: [5000, [Validators.required, Validators.min(0)]],
    freeShippingCurrency:  ['BDT', Validators.required],
    returnPolicyDays:      [30,   [Validators.required, Validators.min(1), Validators.max(365)]],
    returnPolicyText:      ['30-day return policy', Validators.required],
  });

  ngOnInit(): void {
    this.settingsService.getByCategory('SHOP').subscribe({
      next: settings => {
        const get = (key: string) => settings.find(s => s.key === key)?.value;

        this.form.patchValue({
          freeShippingEnabled:   get('SHOP_FREE_SHIPPING_ENABLED')   === 'True' || get('SHOP_FREE_SHIPPING_ENABLED') === 'true',
          freeShippingThreshold: parseFloat(get('SHOP_FREE_SHIPPING_THRESHOLD') ?? '5000'),
          freeShippingCurrency:  get('SHOP_FREE_SHIPPING_CURRENCY')  ?? 'BDT',
          returnPolicyDays:      parseInt(get('SHOP_RETURN_POLICY_DAYS') ?? '30', 10),
          returnPolicyText:      get('SHOP_RETURN_POLICY_TEXT')      ?? '30-day return policy',
        });
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load shop policies' });
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

    const updates = [
      this.settingsService.update('SHOP_FREE_SHIPPING_ENABLED',   { value: String(v.freeShippingEnabled),  dataType: 'BOOL',    category: 'SHOP', isSystemSetting: true }),
      this.settingsService.update('SHOP_FREE_SHIPPING_THRESHOLD',  { value: String(v.freeShippingThreshold), dataType: 'DECIMAL', category: 'SHOP', isSystemSetting: true }),
      this.settingsService.update('SHOP_FREE_SHIPPING_CURRENCY',   { value: (v.freeShippingCurrency as string).toUpperCase(), dataType: 'STRING', category: 'SHOP', isSystemSetting: true }),
      this.settingsService.update('SHOP_RETURN_POLICY_DAYS',       { value: String(v.returnPolicyDays),     dataType: 'INT',     category: 'SHOP', isSystemSetting: true }),
      this.settingsService.update('SHOP_RETURN_POLICY_TEXT',       { value: v.returnPolicyText,             dataType: 'STRING',  category: 'SHOP', isSystemSetting: true }),
    ];

    // Save all in sequence so one failure doesn't mask the others
    let completed = 0;
    let failed = false;

    for (const update$ of updates) {
      update$.subscribe({
        next: () => {
          completed++;
          if (completed === updates.length && !failed) {
            this.messageService.add({ severity: 'success', summary: 'Saved', detail: 'Shop policies updated successfully' });
            this.saving.set(false);
          }
        },
        error: () => {
          if (!failed) {
            failed = true;
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to save one or more settings' });
            this.saving.set(false);
          }
        }
      });
    }
  }
}
