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
import { CatalogEntryService, CatalogEntryResponse, UpsertCatalogEntryRequest } from '../../services/catalog-entry.service';
import { ProductLocationManagerComponent } from '../product-location-manager.component';
import { ProductVariantManagerComponent } from '../product-variant-manager/product-variant-manager.component';
import { ProductMediaManagerComponent } from '../product-media-manager/product-media-manager.component';
import { PriceCodeService } from '@/shared/services/price-code.service';

@Component({
    selector: 'app-part-details',
    standalone: true,
    imports: [
        CommonModule, FormsModule, ReactiveFormsModule,
        ButtonModule, CardModule, TableModule, TagModule,
        ToastModule, DialogModule, InputNumberModule, InputTextModule,
        TextareaModule, DatePickerModule, TooltipModule, TabsModule,
        ToggleSwitchModule, CheckboxModule, SelectModule, ConfirmDialogModule,
        ProductLocationManagerComponent, ProductVariantManagerComponent, ProductMediaManagerComponent
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './part-details.component.html',
    styleUrls: ['./part-details.component.css']
})
export class PartDetailsComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly variantService = inject(ProductVariantService);
    private readonly pricingService = inject(VariantPricingService);
    private readonly catalogEntryService = inject(CatalogEntryService);
    private readonly vehicleService = inject(VehicleService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly fb = inject(FormBuilder);
    readonly priceCodeService = inject(PriceCodeService);

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

    // Online Listing tab
    catalogEntry = signal<CatalogEntryResponse | null>(null);
    catalogEntryLoading = signal(false);
    editingOnline = signal(false);
    savingOnline = signal(false);

    onlineForm = this.fb.group({
        slug:            ['', [Validators.maxLength(200), Validators.pattern(/^[a-z0-9-]*$/)]],
        shortDescription:['', [Validators.maxLength(300)]],
        isPublished:     [true],
        isFeatured:      [false],
        featuredRank:    [0, [Validators.min(0)]],
        metaTitle:       ['', [Validators.maxLength(70)]],
        metaDescription: ['', [Validators.maxLength(160)]]
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
                this.loadCatalogEntry();
            }
        });
    }

    // ── Data loading ───────────────────────────────────────────────────────

    private loadPartDetails(): void {
        this.loading = true;
        this.partService.getPartById(this.partId!).subscribe({
            next: p => { this.part = p; this.loading = false; },
            error: () => { this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load part details' }); this.loading = false; }
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
                    this.messageService.add({ severity: 'success', summary: 'Added', detail: 'Vehicle compatibility added' });
                    this.selectedVehicleId = '';
                    this.loadCompatibleVehicles();
                    this.addingCompatibility = false;
                },
                error: err => {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to add compatibility' });
                    this.addingCompatibility = false;
                }
            });
    }

    onRemoveCompatibility(compat: VehicleCompatibilityResponse): void {
        this.confirmationService.confirm({
            message: `Remove compatibility with ${compat.vehicleMake} ${compat.vehicleModel} ${compat.vehicleYear}?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.vehicleService.removeCompatibility(compat.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Removed', detail: 'Compatibility removed' });
                        this.loadCompatibleVehicles();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to remove compatibility' });
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

    private loadCatalogEntry(): void {
        this.catalogEntryLoading.set(true);
        this.catalogEntryService.get(this.partId!).subscribe({
            next: entry => {
                this.catalogEntry.set(entry);
                if (entry) {
                    this.onlineForm.patchValue({
                        slug: entry.slug,
                        shortDescription: entry.shortDescription,
                        isPublished: entry.isPublished,
                        isFeatured: entry.isFeatured,
                        featuredRank: entry.featuredRank,
                        metaTitle: entry.metaTitle ?? '',
                        metaDescription: entry.metaDescription ?? ''
                    });
                }
                this.catalogEntryLoading.set(false);
            },
            error: () => this.catalogEntryLoading.set(false)
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
            startDate: (v.startDate as Date).toISOString(),
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
                this.messageService.add({ severity: 'success', summary: 'Price Saved', detail: `${this.setPriceTarget!.label} updated to ${saved.sellingPrice} ${saved.currency}` });
                this.savingPrice.set(false);
                this.showSetPriceDialog.set(false);
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to save price' });
                this.savingPrice.set(false);
            }
        });
    }

    // ── Online Listing tab ─────────────────────────────────────────────────

    startEditOnline(): void {
        const entry = this.catalogEntry();
        if (!entry) {
            this.onlineForm.patchValue({
                slug: this.buildSlugFromPart(),
                isPublished: true, isFeatured: false, featuredRank: 0
            });
        }
        this.editingOnline.set(true);
    }

    cancelEditOnline(): void { this.editingOnline.set(false); }

    saveOnlineListing(): void {
        if (this.onlineForm.invalid) { this.onlineForm.markAllAsTouched(); return; }
        const v = this.onlineForm.value;
        const slugVal = (v.slug || '').trim();
        if (!slugVal) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Slug is required' });
            return;
        }

        const req: UpsertCatalogEntryRequest = {
            slug: slugVal,
            shortDescription: v.shortDescription?.trim() || '',
            isPublished: v.isPublished ?? true,
            isFeatured: v.isFeatured ?? false,
            featuredRank: v.featuredRank ?? 0,
            metaTitle: v.metaTitle?.trim() || null,
            metaDescription: v.metaDescription?.trim() || null
        };

        this.savingOnline.set(true);
        this.catalogEntryService.upsert(this.partId!, req).subscribe({
            next: (saved) => {
                this.catalogEntry.set(saved);
                this.editingOnline.set(false);
                this.savingOnline.set(false);
                this.messageService.add({ severity: 'success', summary: 'Saved', detail: 'Online listing updated' });
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to save' });
                this.savingOnline.set(false);
            }
        });
    }

    private buildSlugFromPart(): string {
        if (!this.part?.name) return '';
        return this.part.name.trim().toLowerCase()
            .replace(/[^a-z0-9\s-]/g, '')
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-');
    }

    get onlineSlugCharCount(): number { return (this.onlineForm.get('slug')?.value || '').length; }
    get onlineMetaTitleCount(): number { return (this.onlineForm.get('metaTitle')?.value || '').length; }
    get onlineMetaDescCount(): number { return (this.onlineForm.get('metaDescription')?.value || '').length; }

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
