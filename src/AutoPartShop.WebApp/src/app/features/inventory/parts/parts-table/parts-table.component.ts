import { Component, EventEmitter, Input, Output, OnInit, ViewChild, inject, signal } from '@angular/core';
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

@Component({
  selector: 'app-parts-table',
  standalone: true,
  imports: [
    CommonModule,
    TableModule, ButtonModule, ConfirmDialogModule, TooltipModule,
    TagModule, ContextMenuModule, RippleModule, ToastModule
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

  ngOnInit(): void {
    this.initializeContextMenu();
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
