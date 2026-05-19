import { Component, Output, EventEmitter, ViewChild, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { Select } from 'primeng/select';
import { MenuItem } from 'primeng/api';
import { DiscountResponse } from '../../services/discount.service';

@Component({
  selector: 'app-discounts-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    TooltipModule,
    TagModule,
    ContextMenuModule,
    RippleModule,
    Select
  ],
  templateUrl: './discounts-list.component.html',
  styleUrls: ['./discounts-list.component.css']
})
export class DiscountsListComponent {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  // Input data from parent
  @Input() discounts: DiscountResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;

  // Output events
  @Output() editDiscount = new EventEmitter<DiscountResponse>();
  @Output() deleteDiscount = new EventEmitter<DiscountResponse>();
  @Output() toggleActive = new EventEmitter<DiscountResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();

  // Context menu
  contextMenuItems: MenuItem[] = [];
  selectedDiscount: DiscountResponse | null = null;

  // Expose Math for template
  Math = Math;

  /**
   * Build context menu for a discount
   */
  private buildContextMenu(discount: DiscountResponse): void {
    this.selectedDiscount = discount;
    this.contextMenuItems = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => this.editDiscount.emit(discount)
      },
      { separator: true },
      {
        label: discount.isActive ? 'Deactivate' : 'Activate',
        icon: discount.isActive ? 'pi pi-ban' : 'pi pi-check-circle',
        command: () => this.toggleActive.emit(discount)
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => this.deleteDiscount.emit(discount),
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  /**
   * Show context menu
   */
  showContextMenu(event: MouseEvent, discount: DiscountResponse): void {
    event.preventDefault();
    event.stopPropagation();
    this.buildContextMenu(discount);
    this.contextMenu?.show(event);
  }

  /**
   * Get status label
   */
  getStatusLabel(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }

  /**
   * Format discount value display
   */
  formatDiscountValue(discount: DiscountResponse): string {
    if (discount.type === 'PERCENTAGE') {
      return `${discount.value}%`;
    }
    return `৳${discount.value.toLocaleString()}`;
  }

  /**
   * Format valid period display
   */
  formatValidPeriod(discount: DiscountResponse): string {
    const start = new Date(discount.startDate).toLocaleDateString('en-GB', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
    if (discount.endDate) {
      const end = new Date(discount.endDate).toLocaleDateString('en-GB', {
        day: '2-digit', month: 'short', year: 'numeric'
      });
      return `${start} → ${end}`;
    }
    return `${start} → No expiry`;
  }

  /**
   * Navigate to a specific page
   */
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.pageChange.emit({ page, rows: this.rows });
  }

  /**
   * Handle page size change
   */
  onPageSizeChange(newRows: number): void {
    this.pageChange.emit({ page: 1, rows: newRows });
  }

  get first(): number {
    return Math.max(0, (this.currentPage - 1) * this.rows);
  }

  get totalPages(): number {
    if (!this.totalRecords || !this.rows) {
      return 0;
    }
    return Math.ceil(this.totalRecords / this.rows);
  }
}
