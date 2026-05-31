import { Component, Output, EventEmitter, ViewChild, Input, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { MenuItem } from 'primeng/api';
import { BrandResponse } from '../../services/brand.service';
import { I18nService } from '@/shared/services/i18n.service';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-brands-list',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, TooltipModule, TagModule, ContextMenuModule, RippleModule, DataPaginationComponent],
    templateUrl: './brands-list.component.html',
    styleUrls: ['./brands-list.component.css']
})
export class BrandsListComponent implements OnInit {
    @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

    @Input() brands: BrandResponse[] = [];
    @Input() loading = false;
    @Input() totalRecords = 0;
    @Input() rows = 10;
    @Input() currentPage = 1;

    @Output() editBrand       = new EventEmitter<BrandResponse>();
    @Output() deleteBrand     = new EventEmitter<BrandResponse>();
    @Output() toggleStatus    = new EventEmitter<BrandResponse>();
    @Output() pageChange      = new EventEmitter<{ page: number; rows: number }>();

    contextMenuItems: MenuItem[] = [];
    selectedBrand: BrandResponse | null = null;
    Math = Math;

    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    ngOnInit(): void {
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (this.selectedBrand) this.rebuildContextMenu(this.selectedBrand);
        });
    }

    showContextMenu(event: MouseEvent, brand: BrandResponse): void {
        event.preventDefault();
        event.stopPropagation();
        this.selectedBrand = brand;
        this.rebuildContextMenu(brand);
        this.contextMenu?.show(event);
    }

    private rebuildContextMenu(brand: BrandResponse): void {
        this.contextMenuItems = [
            { label: this.i18n.t('common.actions.edit'), icon: 'pi pi-pencil', command: () => this.editBrand.emit(brand) },
            { separator: true },
            {
                label: brand.isActive ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate'),
                icon: brand.isActive ? 'pi pi-ban' : 'pi pi-check-circle',
                command: () => this.toggleStatus.emit(brand)
            },
            { separator: true },
            { label: this.i18n.t('common.actions.delete'), icon: 'pi pi-trash', command: () => this.deleteBrand.emit(brand), styleClass: 'p-menuitem-danger' }
        ];
    }

    getStatusLabel(isActive: boolean): string {
        return isActive ? this.i18n.t('common.status.active') : this.i18n.t('common.status.inactive');
    }

    goToPage(page: number): void {
        if (page < 1 || page > this.totalPages) return;
        this.pageChange.emit({ page, rows: this.rows });
    }

    onPageSizeChange(newRows: number): void {
        this.pageChange.emit({ page: 1, rows: newRows });
    }

    get first(): number   { return Math.max(0, (this.currentPage - 1) * this.rows); }
    get totalPages(): number {
        if (!this.totalRecords || !this.rows) return 0;
        return Math.ceil(this.totalRecords / this.rows);
    }
}
