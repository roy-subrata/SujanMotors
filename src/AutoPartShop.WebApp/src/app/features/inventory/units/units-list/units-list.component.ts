import { Component, EventEmitter, inject, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { BadgeModule } from 'primeng/badge';
import { MessageService } from 'primeng/api';
import { UnitService, UnitResponse } from '../../services/unit.service';
import { Select } from 'primeng/select';

@Component({
  selector: 'app-units-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, InputTextModule, TooltipModule, BadgeModule, FormsModule, Select],
  templateUrl: './units-list.component.html',
  styleUrls: ['./units-list.component.css']
})
export class UnitsListComponent implements OnInit {
  @Output() editUnit = new EventEmitter<UnitResponse>();
  @Output() deleteUnit = new EventEmitter<UnitResponse>();
  @Output() toggleStatus = new EventEmitter<UnitResponse>();

  private readonly unitService = inject(UnitService);
  private readonly messageService = inject(MessageService);

  units: UnitResponse[] = [];
  selectedUnits: UnitResponse[] = [];
  loading = false;
  pageNumber = 1;
  pageSize = 10;
  totalRecords = 0;
  searchTerm = '';
  pageSizeOptions = [10, 20, 50];

  ngOnInit(): void {
    this.loadUnits();
  }

  /**
   * Load units with pagination and search
   */
  loadUnits(pageNum: number = 1): void {
    this.loading = true;
    this.pageNumber = pageNum;

    this.unitService.getListUnits(this.pageNumber, this.pageSize, this.searchTerm).subscribe({
      next: (response) => {
        this.units = response.data;
        this.totalRecords = response.pagination.totalCount;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to load units'
        });
        this.loading = false;
      }
    });
  }

  /**
   * Handle pagination change
   */
  onPageChange(event: any): void {
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
      return;
    }
    const pageNum = Math.floor(event.first / event.rows) + 1;
    this.pageSize = event.rows;
    this.loadUnits(pageNum);
  }

  /**
   * Search units
   */
  search(query: string): void {
    this.searchTerm = query;
    this.pageNumber = 1;
    this.loadUnits(1);
  }

  /**
   * Clear search
   */
  clearSearch(): void {
    this.searchTerm = '';
    this.pageNumber = 1;
    this.loadUnits(1);
  }

  /**
   * Handle edit action
   */
  onEdit(unit: UnitResponse): void {
    this.editUnit.emit(unit);
  }

  /**
   * Handle delete action
   */
  onDelete(unit: UnitResponse): void {
    this.deleteUnit.emit(unit);
  }

  /**
   * Handle toggle status action
   */
  onToggleStatus(unit: UnitResponse): void {
    this.toggleStatus.emit(unit);
  }

  /**
   * Reload units list
   */
  reload(): void {
    this.pageNumber = 1;
    this.searchTerm = '';
    this.loadUnits(1);
  }

  get first(): number {
    return Math.max(0, (this.pageNumber - 1) * this.pageSize);
  }

  get pageNumberDisplay(): number {
    if (!this.totalRecords) {
      return 0;
    }
    return Math.floor(this.first / this.pageSize) + 1;
  }

  get totalPages(): number {
    if (!this.totalRecords) {
      return 0;
    }
    return Math.ceil(this.totalRecords / this.pageSize);
  }
}
