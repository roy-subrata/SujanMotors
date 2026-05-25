import { Component, Output, EventEmitter, ViewChild, Input, OnInit, inject, DestroyRef } from '@angular/core';
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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';

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
export class DiscountsListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() discounts: DiscountResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;

  @Output() editDiscount = new EventEmitter<DiscountResponse>();
  @Output() deleteDiscount = new EventEmitter<DiscountResponse>();
  @Output() toggleActive = new EventEmitter<DiscountResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();

  contextMenuItems: MenuItem[] = [];
  selectedDiscount: DiscountResponse | null = null;

  Math = Math;

  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.selectedDiscount) this.buildContextMenu(this.selectedDiscount);
    });
  }

  private buildContextMenu(discount: DiscountResponse): void {
    this.selectedDiscount = discount;
    this.contextMenuItems = [
      {
        label: this.i18n.t('common.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => this.editDiscount.emit(discount)
      },
      { separator: true },
      {
        label: discount.isActive ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate'),
        icon: discount.isActive ? 'pi pi-ban' : 'pi pi-check-circle',
        command: () => this.toggleActive.emit(discount)
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.delete'),
        icon: 'pi pi-trash',
        command: () => this.deleteDiscount.emit(discount),
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  showContextMenu(event: MouseEvent, discount: DiscountResponse): void {
    event.preventDefault();
    event.stopPropagation();
    this.buildContextMenu(discount);
    this.contextMenu?.show(event);
  }

  getStatusLabel(isActive: boolean): string {
    return isActive ? this.i18n.t('common.status.active') : this.i18n.t('common.status.inactive');
  }

  formatDiscountValue(discount: DiscountResponse): string {
    if (discount.type === 'PERCENTAGE') {
      return `${discount.value}%`;
    }
    return `৳${discount.value.toLocaleString()}`;
  }

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

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.pageChange.emit({ page, rows: this.rows });
  }

  onPageSizeChange(newRows: number): void {
    this.pageChange.emit({ page: 1, rows: newRows });
  }

  get first(): number {
    return Math.max(0, (this.currentPage - 1) * this.rows);
  }

  get totalPages(): number {
    if (!this.totalRecords || !this.rows) return 0;
    return Math.ceil(this.totalRecords / this.rows);
  }
}
