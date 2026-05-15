import { Component, EventEmitter, Input, Output, OnInit, OnChanges, SimpleChanges, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { PartService, PartResponse } from '../../services/part.service';
import { PriceCodeService } from '@/shared/services/price-code.service';
import { VariantPricingService, ActivePriceResponse } from '../../services/variant-pricing.service';

@Component({
  selector: 'app-parts-table',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    TableModule, ButtonModule, ConfirmDialogModule, TooltipModule,
    TagModule, ContextMenuModule, RippleModule,
    DialogModule, InputNumberModule, DatePickerModule, InputTextModule, ToastModule
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './parts-table.component.html',
  styleUrls: ['./parts-table.component.css']
})
export class PartsTableComponent implements OnInit, OnChanges {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() parts: PartResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;

  @Output() editClick = new EventEmitter<PartResponse>();
  @Output() deleteClick = new EventEmitter<PartResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() partDeleted = new EventEmitter<void>();
  @Output() showBarcodeClick = new EventEmitter<PartResponse>();

  contextMenuItems: MenuItem[] = [];
  selectedPart: PartResponse | null = null;

  // Active price per partId (null = not set, undefined = loading)
  activePrices = new Map<string, ActivePriceResponse | null>();
  loadingPrices = signal(false);

  // Set Price dialog
  showSetPriceDialog = signal(false);
  savingPrice = signal(false);
  priceForm = this.fb.group({
    sellingPrice: [null as number | null, [Validators.required, Validators.min(0.01)]],
    startDate:    [new Date() as Date | null, [Validators.required]],
    currency:     ['BDT'],
    reason:       ['']
  });

  private readonly partService = inject(PartService);
  private readonly pricingService = inject(VariantPricingService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly priceCodeService = inject(PriceCodeService);

  ngOnInit(): void {
    this.initializeContextMenu();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['parts'] && this.parts.length > 0)
      this.loadActivePrices();
  }

  // ── Price loading ─────────────────────────────────────────────────────────

  private loadActivePrices(): void {
    this.parts.forEach(part => {
      if (!this.activePrices.has(part.id)) {
        this.pricingService.getActivePrice(part.id).subscribe({
          next: (p) => this.activePrices.set(part.id, p),
          error: () => this.activePrices.set(part.id, null)
        });
      }
    });
  }

  getActivePrice(partId: string): ActivePriceResponse | null | undefined {
    return this.activePrices.get(partId);
  }

  formatActivePrice(partId: string): string {
    const p = this.activePrices.get(partId);
    if (p === undefined) return '...';
    if (p === null) return '—';
    return `${p.sellingPrice.toLocaleString('en-BD', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${p.currency}`;
  }

  isPriceNotSet(partId: string): boolean {
    return this.activePrices.get(partId) === null;
  }

  isPriceLoading(partId: string): boolean {
    return !this.activePrices.has(partId);
  }

  // ── Set Price dialog ───────────────────────────────────────────────────────

  openSetPriceDialog(part: PartResponse): void {
    this.selectedPart = part;
    const current = this.activePrices.get(part.id);
    this.priceForm.reset({
      sellingPrice: current?.sellingPrice ?? null,
      startDate: new Date(),
      currency: current?.currency ?? 'BDT',
      reason: ''
    });
    this.showSetPriceDialog.set(true);
  }

  onSavePrice(): void {
    if (!this.priceForm.valid || !this.selectedPart) {
      this.priceForm.markAllAsTouched();
      return;
    }
    const v = this.priceForm.getRawValue();
    this.savingPrice.set(true);

    this.pricingService.setPrice(this.selectedPart.id, {
      sellingPrice: v.sellingPrice!,
      startDate: (v.startDate as Date).toISOString(),
      currency: v.currency || 'BDT',
      reason: v.reason || undefined
    }).subscribe({
      next: (saved) => {
        // Update cache
        this.activePrices.set(this.selectedPart!.id, {
          partId: this.selectedPart!.id,
          sellingPrice: saved.sellingPrice,
          currency: saved.currency,
          source: 'PRODUCT_HISTORY',
          validFrom: saved.startDate,
          validTo: saved.endDate ?? null
        });
        this.messageService.add({ severity: 'success', summary: 'Price Saved', detail: `Price set to ${saved.sellingPrice} ${saved.currency}` });
        this.savingPrice.set(false);
        this.showSetPriceDialog.set(false);
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to save price' });
        this.savingPrice.set(false);
      }
    });
  }

  // ── Context menu ───────────────────────────────────────────────────────────

  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => { if (this.selectedPart) this.onEditClick(this.selectedPart); }
      },
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => { if (this.selectedPart) this.onViewDetailsClick(this.selectedPart); }
      },
      {
        label: 'Set Price',
        icon: 'pi pi-tag',
        command: () => { if (this.selectedPart) this.openSetPriceDialog(this.selectedPart); }
      },
      {
        label: 'Show Barcode',
        icon: 'pi pi-qrcode',
        command: () => { if (this.selectedPart) this.onShowBarcodeClick(this.selectedPart); }
      },
      { separator: true },
      {
        label: 'Activate',
        icon: 'pi pi-check',
        command: () => { if (this.selectedPart && !this.selectedPart.isActive) this.activatePart(this.selectedPart.id); },
        visible: this.selectedPart ? !this.selectedPart.isActive : false
      },
      {
        label: 'Deactivate',
        icon: 'pi pi-times',
        command: () => { if (this.selectedPart && this.selectedPart.isActive) this.deactivatePart(this.selectedPart.id); },
        visible: this.selectedPart ? this.selectedPart.isActive : false
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => { if (this.selectedPart) this.onDeleteClick(this.selectedPart); },
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  showContextMenu(event: MouseEvent, part: PartResponse): void {
    this.selectedPart = part;
    this.initializeContextMenu();
    if (this.contextMenu) this.contextMenu.show(event);
  }

  // ── Other handlers ─────────────────────────────────────────────────────────

  private activatePart(partId: string): void {
    this.partService.activatePart(partId).subscribe({
      next: (updatedPart) => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Part activated successfully' });
        const idx = this.parts.findIndex(p => p.id === partId);
        if (idx !== -1) { this.parts[idx] = updatedPart; this.parts = [...this.parts]; }
      },
      error: (err) => this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to activate part' })
    });
  }

  private deactivatePart(partId: string): void {
    this.partService.deactivatePart(partId).subscribe({
      next: (updatedPart) => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Part deactivated successfully' });
        const idx = this.parts.findIndex(p => p.id === partId);
        if (idx !== -1) { this.parts[idx] = updatedPart; this.parts = [...this.parts]; }
      },
      error: (err) => this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to deactivate part' })
    });
  }

  onEditClick(part: PartResponse): void { this.editClick.emit(part); }
  onViewDetailsClick(part: PartResponse): void { this.router.navigate(['/inventory/parts', part.id]); }
  onShowBarcodeClick(part: PartResponse): void { this.showBarcodeClick.emit(part); }

  onDeleteClick(part: PartResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete part '${part.name}'? This action cannot be undone.`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.deletePart(part.id)
    });
  }

  private deletePart(partId: string): void {
    this.partService.deletePart(partId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Part deleted successfully' });
        this.partDeleted.emit();
      },
      error: (err) => this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to delete part' })
    });
  }

  onPageChange(event: any): void {
    if (!event || typeof event.page !== 'number' || typeof event.rows !== 'number') return;
    // Clear price cache on page change so new page loads fresh
    this.activePrices.clear();
    this.pageChange.emit({ page: event.page + 1, rows: event.rows });
  }

  getStatusSeverity(isActive: boolean): string { return isActive ? 'success' : 'danger'; }

  formatPrice(price: number): string { return `₹${price.toFixed(2)}`; }

  formatCostPrice(price: number): string {
    const coded = this.priceCodeService.getDisplayPrice(price);
    return coded !== null ? coded : this.formatPrice(price);
  }

  calculateMargin(costPrice: number, sellingPrice: number): string {
    if (costPrice === 0) return '0%';
    return `${(((sellingPrice - costPrice) / costPrice) * 100).toFixed(2)}%`;
  }
}
