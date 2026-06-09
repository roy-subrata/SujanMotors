import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { SupplierService, SupplierPerformanceResponse } from '../../inventory/services/supplier.service';

@Component({
  selector: 'app-supplier-performance',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    TagModule,
    ToastModule,
    ButtonModule,
    PageContainerComponent,
    PageHeaderComponent,
    FilterBarComponent
  ],
  providers: [MessageService],
  templateUrl: './supplier-performance.component.html'
})
export class SupplierPerformanceComponent implements OnInit {
  private readonly supplierService = inject(SupplierService);
  private readonly messageService = inject(MessageService);

  rows: SupplierPerformanceResponse[] = [];
  loading = false;
  searchTerm = '';

  pageSize = 10;
  first = 0;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.supplierService.getPerformance(this.searchTerm).subscribe({
      next: (data) => {
        this.rows = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load supplier performance.'
        });
      }
    });
  }

  onSearch(): void {
    this.first = 0;
    this.loadData();
  }

  /** Higher damaged rate = worse quality → warn/danger tag. */
  rateSeverity(rate: number): 'success' | 'warn' | 'danger' {
    if (rate >= 10) return 'danger';
    if (rate >= 3) return 'warn';
    return 'success';
  }
}
