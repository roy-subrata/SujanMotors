import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { TabsModule } from 'primeng/tabs';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectModule } from 'primeng/select';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';

import { PartService, PartResponse, VehicleCompatibilityResponse } from '../../services/part.service';
import { VehicleService, VehicleResponse } from '../../services/vehicle.service';
import { ProductVariantService, ProductVariantResponse } from '../../services/product-variant.service';
import { VariantPricingService, ActivePriceResponse } from '../../services/variant-pricing.service';
import { ProductLocationManagerComponent } from '../product-location-manager.component';
import { ProductVariantManagerComponent } from '../product-variant-manager/product-variant-manager.component';
import { ProductMediaManagerComponent } from '../product-media-manager/product-media-manager.component';
import { ProductSpecsManagerComponent } from '../product-specs-manager/product-specs-manager.component';
import { PriceCodeService } from '@/shared/services/price-code.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { MoneyFormatPipe } from '@/shared/pipes/money-format.pipe';

@Component({
    selector: 'app-part-details',
    standalone: true,
    imports: [
        CommonModule, FormsModule, ReactiveFormsModule,
        ButtonModule, CardModule, TableModule, TagModule,
        ToastModule, DialogModule, InputNumberModule, InputTextModule,
        TextareaModule, DatePickerModule, TooltipModule, TabsModule,
        ToggleSwitchModule, CheckboxModule, SelectModule, ConfirmDialogModule,
        ProductLocationManagerComponent, ProductVariantManagerComponent, ProductMediaManagerComponent,
        ProductSpecsManagerComponent, TranslatePipe, MoneyFormatPipe
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './part-details.component.html',
    styleUrls: ['./part-details.component.css']
})
export class PartDetailsComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly variantService = inject(ProductVariantService);
    private readonly pricingService = inject(VariantPricingService);
    private readonly vehicleService = inject(VehicleService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly fb = inject(FormBuilder);
    readonly priceCodeService = inject(PriceCodeService);
    readonly i18n = inject(I18nService);

    part: PartResponse | null = null;
    compatibleVehicles: VehicleCompatibilityResponse[] = [];
    loading = false;
    loadingVehicles = false;
    partId: string | null = null;

    // Vehicle compatibility management
    allVehicles: VehicleResponse[] = [];
    selectedVehicleId: string = '';
    addingCompatibility = false;

    get availableVehicles(): VehicleResponse[] {
        const linked = new Set(this.compatibleVehicles.map(v => v.vehicleId));
        return this.allVehicles.filter(v => v.isActive && !linked.has(v.id));
    }

    vehicleLabel(v: VehicleResponse): string {
        return `${v.make} ${v.model} ${v.year} (${v.engineType})`;
    }

    // Pricing tab
    basePrice = signal<ActivePriceResponse | null>(null);
    basePriceLoading = signal(false);
    variants = signal<ProductVariantResponse[]>([]);
    variantPrices = new Map<string, ActivePriceResponse | null>();
    variantsLoading = signal(false);

    showSetPriceDialog = signal(false);
    savingPrice = signal(false);
    setPriceTarget: { partId: string; variantId?: string; label: string } | null = null;

    priceForm = this.fb.group({
        sellingPrice: [null as number | null, [Validators.required, Validators.min(0.01)]],
        startDate:    [new Date() as Date | null, [Validators.required]],
        currency:     ['BDT'],
        reason:       ['']
    });

    ngOnInit(): void {
        this.loadAllVehicles();
        this.route.params.subscribe(params => {
            this.partId = params['id'];
            if (this.partId) {
                this.loadPartDetails();
                this.loadCompatibleVehicles();
                this.loadBasePrice();
                this.loadVariantsForPricing();
            }
        });
    }

    // ── Data loading ───────────────────────────────────────────────────────

    private loadPartDetails(): void {
        this.loading = true;
        this.partService.getPartById(this.partId!).subscribe({
            next: p => { this.part = p; this.loading = false; },
            error: () => { this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('parts.partDetails.messages.loadFailed') }); this.loading = false; }
        });
    }

    private loadCompatibleVehicles(): void {
        this.loadingVehicles = true;
        this.partService.getPartCompatibleVehicles(this.partId!).subscribe({
            next: v => { this.compatibleVehicles = v; this.loadingVehicles = false; },
            error: () => this.loadingVehicles = false
        });
    }

    private loadAllVehicles(): void {
        this.vehicleService.getAllVehicles().subscribe({
            next: v => this.allVehicles = v,
            error: () => {}
        });
    }

    onAddCompatibility(): void {
        if (!this.selectedVehicleId || !this.partId) return;
        this.addingCompatibility = true;
        this.vehicleService.addPartCompatibility(this.selectedVehicleId, this.partId, { isCompatible: true })
            .subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: this.i18n.t('parts.partDetails.messages.addedSummary'), detail: this.i18n.t('parts.partDetails.messages.compatibilityAddedDetail') });
                    this.selectedVehicleId = '';
                    this.loadCompatibleVehicles();
                    this.addingCompatibility = false;
                },
                error: err => {
                    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.message || this.i18n.t('parts.partDetails.messages.addCompatibilityFailed') });
                    this.addingCompatibility = false;
                }
            });
    }

    onRemoveCompatibility(compat: VehicleCompatibilityResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('parts.partDetails.messages.removeCompatibilityConfirm', { make: compat.vehicleMake, model: compat.vehicleModel, year: String(compat.vehicleYear) }),
            header: this.i18n.t('parts.partDetails.confirmHeader'),
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.vehicleService.removeCompatibility(compat.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: this.i18n.t('parts.partDetails.messages.removedSummary'), detail: this.i18n.t('parts.partDetails.messages.compatibilityRemovedDetail') });
                        this.loadCompatibleVehicles();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('parts.partDetails.messages.removeCompatibilityFailed') });
                    }
                });
            }
        });
    }

    private loadBasePrice(): void {
        this.basePriceLoading.set(true);
        this.pricingService.getActivePrice(this.partId!).subscribe({
            next: p => { this.basePrice.set(p); this.basePriceLoading.set(false); },
            error: () => { this.basePrice.set(null); this.basePriceLoading.set(false); }
        });
    }

    private loadVariantsForPricing(): void {
        this.variantsLoading.set(true);
        this.variantService.getVariants(this.partId!).subscribe({
            next: vList => {
                this.variants.set(vList.filter(v => v.isActive));
                this.variantsLoading.set(false);
                vList.filter(v => v.isActive).forEach(v => {
                    this.pricingService.getActivePrice(this.partId!, v.id).subscribe({
                        next: p => this.variantPrices.set(v.id, p),
                        error: () => this.variantPrices.set(v.id, null)
                    });
                });
            },
            error: () => this.variantsLoading.set(false)
        });
    }

    getVariantPrice(variantId: string): ActivePriceResponse | null | undefined {
        return this.variantPrices.get(variantId);
    }

    // ── Set Price dialog ───────────────────────────────────────────────────

    openSetPriceDialog(partId: string, label: string, variantId?: string): void {
        this.setPriceTarget = { partId, variantId, label };
        const current = variantId ? this.variantPrices.get(variantId) : this.basePrice();
        this.priceForm.reset({
            sellingPrice: current?.sellingPrice ?? null,
            startDate: new Date(),
            currency: current?.currency ?? 'BDT',
            reason: ''
        });
        this.showSetPriceDialog.set(true);
    }

    onSavePrice(): void {
        if (!this.priceForm.valid || !this.setPriceTarget) {
            this.priceForm.markAllAsTouched();
            return;
        }
        const v = this.priceForm.getRawValue();
        this.savingPrice.set(true);

        this.pricingService.setPrice(this.setPriceTarget.partId, {
            sellingPrice: v.sellingPrice!,
            // Send the picked calendar day as a local date-only string. Using toISOString()
            // would shift the day backwards in positive-offset timezones (e.g. June 15 local
            // midnight becomes June 14 in UTC+6), making the price effective a day early.
            startDate: this.toLocalDateString(v.startDate as Date),
            currency: v.currency || 'BDT',
            reason: v.reason || undefined
        }, this.setPriceTarget.variantId).subscribe({
            next: (saved) => {
                const updated: ActivePriceResponse = {
                    partId: this.setPriceTarget!.partId,
                    productVariantId: this.setPriceTarget!.variantId,
                    sellingPrice: saved.sellingPrice,
                    currency: saved.currency,
                    source: this.setPriceTarget!.variantId ? 'VARIANT_SCHEDULE' : 'PRODUCT_SCHEDULE',
                    validFrom: saved.startDate,
                    validTo: saved.endDate ?? null
                };
                if (this.setPriceTarget!.variantId) {
                    this.variantPrices.set(this.setPriceTarget!.variantId, updated);
                    this.variants.set([...this.variants()]);
                } else {
                    this.basePrice.set(updated);
                }
                this.messageService.add({ severity: 'success', summary: this.i18n.t('parts.partDetails.messages.priceSavedSummary'), detail: this.i18n.t('parts.partDetails.messages.priceSavedDetail', { label: this.setPriceTarget!.label, price: String(saved.sellingPrice), currency: saved.currency }) });
                this.savingPrice.set(false);
                this.showSetPriceDialog.set(false);
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err.error?.message || this.i18n.t('parts.partDetails.messages.savePriceFailed') });
                this.savingPrice.set(false);
            }
        });
    }

    /** Formats a Date as a local-timezone `yyyy-MM-dd` string (no UTC conversion). */
    private toLocalDateString(d: Date): string {
        const year = d.getFullYear();
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    onBack(): void { this.router.navigate(['/inventory/parts']); }

    onEdit(): void {
        if (this.part) this.router.navigate(['/inventory/parts'], { queryParams: { edit: this.part.id } });
    }

    formatPrice(price: number): string { return `${price.toFixed(2)}`; }

    formatCostPrice(price: number): string {
        const coded = this.priceCodeService.getDisplayPrice(price);
        return coded !== null ? coded : this.formatPrice(price);
    }

    getStatusSeverity(isActive: boolean): 'success' | 'danger' { return isActive ? 'success' : 'danger'; }
    getCompatibilitySeverity(isCompatible: boolean): 'success' | 'warn' { return isCompatible ? 'success' : 'warn'; }

    formatDate(d: string): string {
        return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
    }
}
