import { Component, EventEmitter, Input, Output, OnInit, ViewChild, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { PartService, PartResponse } from '../../services/part.service';
import { PriceCodeService } from '@/shared/services/price-code.service';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { MoneyFormatPipe } from '@/shared/pipes/money-format.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-parts-table',
  standalone: true,
  imports: [
    CommonModule,
    TableModule, ButtonModule, ConfirmDialogModule, TooltipModule,
    TagModule, ContextMenuModule, RippleModule, ToastModule,
    DataPaginationComponent, TranslatePipe, MoneyFormatPipe
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './parts-table.component.html',
  styleUrls: ['./parts-table.component.css']
})
export class PartsTableComponent implements OnInit {
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

  private readonly partService = inject(PartService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  readonly priceCodeService = inject(PriceCodeService);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.initializeContextMenu();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.initializeContextMenu();
    });
  }

  // ── Context menu ───────────────────────────────────────────────────────────

  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: this.i18n.t('common.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => { if (this.selectedPart) this.onEditClick(this.selectedPart); }
      },
      {
        label: this.i18n.t('common.actions.viewDetails'),
        icon: 'pi pi-eye',
        command: () => { if (this.selectedPart) this.onViewDetailsClick(this.selectedPart); }
      },
      {
        label: this.i18n.t('parts.showBarcode'),
        icon: 'pi pi-qrcode',
        command: () => { if (this.selectedPart) this.onShowBarcodeClick(this.selectedPart); }
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.activate'),
        icon: 'pi pi-check',
        command: () => { if (this.selectedPart && !this.selectedPart.isActive) this.activatePart(this.selectedPart.id); },
        visible: this.selectedPart ? !this.selectedPart.isActive : false
      },
      {
        label: this.i18n.t('common.actions.deactivate'),
        icon: 'pi pi-times',
        command: () => { if (this.selectedPart && this.selectedPart.isActive) this.deactivatePart(this.selectedPart.id); },
        visible: this.selectedPart ? this.selectedPart.isActive : false
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.delete'),
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
        this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.messages.activateSuccess') });
        const idx = this.parts.findIndex(p => p.id === partId);
        if (idx !== -1) { this.parts[idx] = updatedPart; this.parts = [...this.parts]; }
      },
      error: (err) => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.message || this.i18n.t('parts.messages.activateFailed') })
    });
  }

  private deactivatePart(partId: string): void {
    this.partService.deactivatePart(partId).subscribe({
      next: (updatedPart) => {
        this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.messages.deactivateSuccess') });
        const idx = this.parts.findIndex(p => p.id === partId);
        if (idx !== -1) { this.parts[idx] = updatedPart; this.parts = [...this.parts]; }
      },
      error: (err) => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.message || this.i18n.t('parts.messages.deactivateFailed') })
    });
  }

  onEditClick(part: PartResponse): void { this.editClick.emit(part); }
  onViewDetailsClick(part: PartResponse): void { this.router.navigate(['/inventory/parts', part.id]); }
  onShowBarcodeClick(part: PartResponse): void { this.showBarcodeClick.emit(part); }

  onDeleteClick(part: PartResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('parts.messages.deleteConfirm', { name: part.name }),
      header: this.i18n.t('parts.messages.deleteConfirmHeader'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.deletePart(part.id)
    });
  }

  private deletePart(partId: string): void {
    this.partService.deletePart(partId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('parts.messages.deleteSuccess') });
        this.partDeleted.emit();
      },
      error: (err) => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.message || this.i18n.t('parts.messages.deleteFailed') })
    });
  }

  onPageChange(event: any): void {
    if (!event || typeof event.page !== 'number' || typeof event.rows !== 'number') return;
    this.pageChange.emit({ page: event.page + 1, rows: event.rows });
  }

  /** Wired to the shared <app-data-pagination> footer (1-based page). */
  goToPage(page: number): void {
    this.pageChange.emit({ page, rows: this.rows });
  }

  /** Wired to the shared <app-data-pagination> page-size selector. */
  onPageSizeChange(size: number): void {
    this.pageChange.emit({ page: 1, rows: size });
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
