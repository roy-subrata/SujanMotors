import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import {
  VariantPricingService,
  ActivePriceResponse,
  PriceHistoryRecord
} from '../../services/variant-pricing.service';

@Component({
  selector: 'app-part-pricing-panel',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    ButtonModule, DialogModule, InputNumberModule, DatePickerModule,
    InputTextModule, TableModule, TagModule, TooltipModule, ToastModule
  ],
  providers: [MessageService],
  templateUrl: './part-pricing-panel.component.html',
  styleUrls: ['./part-pricing-panel.component.css']
})
export class PartPricingPanelComponent implements OnInit {
  @Input() partId!: string;
  @Input() variantId?: string;
  @Input() label = 'Selling Price';  // e.g. "Base Product Price" or "Variant Price"

  private readonly pricingService = inject(VariantPricingService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);

  activePrice = signal<ActivePriceResponse | null>(null);
  priceHistory = signal<PriceHistoryRecord[]>([]);
  loading = signal(false);
  loadingHistory = signal(false);
  showSetPriceDialog = signal(false);
  showHistory = signal(false);
  saving = signal(false);

  priceForm = this.fb.group({
    sellingPrice: [null as number | null, [Validators.required, Validators.min(0.01)]],
    startDate:    [new Date() as Date | null, [Validators.required]],
    currency:     ['BDT'],
    reason:       ['']
  });

  ngOnInit(): void {
    this.loadActivePrice();
  }

  loadActivePrice(): void {
    this.loading.set(true);
    this.pricingService.getActivePrice(this.partId, this.variantId).subscribe({
      next: (p) => { this.activePrice.set(p); this.loading.set(false); },
      error: () => { this.activePrice.set(null); this.loading.set(false); }
    });
  }

  loadHistory(): void {
    if (this.priceHistory().length > 0) { this.showHistory.set(!this.showHistory()); return; }
    this.loadingHistory.set(true);
    this.pricingService.getPriceHistory(this.partId).subscribe({
      next: (h) => {
        // Filter by scope — if variantId given, show variant records; else show base product records
        const filtered = this.variantId
          ? h.filter(r => r.productVariantId === this.variantId)
          : h.filter(r => !r.productVariantId);
        this.priceHistory.set(filtered);
        this.loadingHistory.set(false);
        this.showHistory.set(true);
      },
      error: () => this.loadingHistory.set(false)
    });
  }

  openSetPriceDialog(): void {
    this.priceForm.reset({
      sellingPrice: this.activePrice()?.sellingPrice ?? null,
      startDate: new Date(),
      currency: this.activePrice()?.currency ?? 'BDT',
      reason: ''
    });
    this.showSetPriceDialog.set(true);
  }

  onSavePrice(): void {
    if (!this.priceForm.valid) {
      this.priceForm.markAllAsTouched();
      return;
    }
    const v = this.priceForm.getRawValue();
    this.saving.set(true);
    this.pricingService.setPrice(this.partId, {
      sellingPrice: v.sellingPrice!,
      startDate: (v.startDate as Date).toISOString(),
      currency: v.currency || 'BDT',
      reason: v.reason || undefined
    }, this.variantId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Price Set', detail: 'New price saved successfully' });
        this.saving.set(false);
        this.showSetPriceDialog.set(false);
        this.priceHistory.set([]); // reset so history reloads fresh
        this.loadActivePrice();
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to save price' });
        this.saving.set(false);
      }
    });
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
