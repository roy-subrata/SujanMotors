import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { DialogService, DynamicDialogModule } from 'primeng/dynamicdialog';
import { MessageService } from 'primeng/api';
import { MultiSelectModule } from 'primeng/multiselect';
import {
  AuditTrailService,
  AuditLogResponse,
  AuditLogFilterRequest
} from '../../../shared/services/audit-trail.service';
import { EntityTimelineDialogComponent } from '../entity-timeline-dialog/entity-timeline-dialog.component';
import { PaginatorState } from 'primeng/paginator';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    DatePickerModule,
    TagModule,
    TooltipModule,
    ToastModule,
    CardModule,
    DynamicDialogModule,
    MultiSelectModule,
    PageContainerComponent,
    PageHeaderComponent,
    FilterBarComponent,
    DataPaginationComponent,
    TranslatePipe
  ],
  providers: [MessageService, DialogService],
  templateUrl: './audit-logs.component.html',
  styleUrls: ['./audit-logs.component.css']
})
export class AuditLogsComponent implements OnInit {
  private readonly auditService = inject(AuditTrailService);
  private readonly messageService = inject(MessageService);
  private readonly dialogService = inject(DialogService);
  readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchSubject$ = new Subject<string>();

  logs: AuditLogResponse[] = [];
  loading = false;
  totalRecords = 0;

  // Pagination
  pageNumber = 1;
  pageSize = 25;
  pageSizeOptions = [10, 25, 50, 100];

  // Filters
  searchTerm = '';
  selectedEntity: string | null = null;
  selectedAction: string | null = null;
  selectedUser: string | null = null;
  dateRange: Date[] = [];

  // Filter options
  entities: { label: string; value: string }[] = [];
  users: { label: string; value: string }[] = [];
  actions: { label: string; value: string }[] = [];
  private rawEntities: string[] = [];
  private rawUsers: string[] = [];

  // Sort
  sortField = 'performedAt';
  sortOrder = -1;

  ngOnInit(): void {
    this.buildActionOptions();
    this.loadFilterOptions();
    this.setupSearchDebounce();
    this.loadLogs();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.buildActionOptions();
      this.buildEntityOptions();
      this.buildUserOptions();
    });
  }

  private setupSearchDebounce(): void {
    this.searchSubject$.pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.onSearch();
    });
  }

  onSearchInput(): void {
    this.searchSubject$.next(this.searchTerm);
  }

  private buildActionOptions(): void {
    this.actions = [
      { label: this.i18n.t('audit.logs.actions.all'), value: '' },
      { label: this.i18n.t('audit.logs.actions.insert'), value: 'INSERT' },
      { label: this.i18n.t('audit.logs.actions.update'), value: 'UPDATE' },
      { label: this.i18n.t('audit.logs.actions.delete'), value: 'DELETE' }
    ];
  }

  private buildEntityOptions(): void {
    this.entities = [
      { label: this.i18n.t('audit.logs.allEntities'), value: '' },
      ...this.rawEntities.map(e => ({ label: e, value: e }))
    ];
  }

  private buildUserOptions(): void {
    this.users = [
      { label: this.i18n.t('audit.logs.allUsers'), value: '' },
      ...this.rawUsers.map(u => ({ label: u, value: u }))
    ];
  }

  loadFilterOptions(): void {
    this.auditService.getAuditedEntities().subscribe({
      next: (entities) => {
        this.rawEntities = entities;
        this.buildEntityOptions();
      }
    });

    this.auditService.getAuditUsers().subscribe({
      next: (users) => {
        this.rawUsers = users;
        this.buildUserOptions();
      }
    });
  }

  loadLogs(): void {
    this.loading = true;

    const filter: AuditLogFilterRequest = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      sortBy: this.sortField,
      sortDescending: this.sortOrder === -1
    };

    if (this.searchTerm) filter.searchTerm = this.searchTerm;
    if (this.selectedEntity) filter.entityName = this.selectedEntity;
    if (this.selectedAction) filter.action = this.selectedAction;
    if (this.selectedUser) filter.performedBy = this.selectedUser;
    if (this.dateRange[0]) filter.fromDate = this.dateRange[0].toISOString();
    if (this.dateRange[1]) filter.toDate = this.dateRange[1].toISOString();

    this.auditService.getAuditLogs(filter).subscribe({
      next: (response) => {
        this.logs = response.data;
        this.totalRecords = response.pagination.totalCount;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading audit logs:', err);
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('audit.logs.messages.errorSummary'),
          detail: this.i18n.t('audit.logs.messages.loadFailed')
        });
        this.loading = false;
      }
    });
  }

  onPageChange(event: PaginatorState): void {
    this.pageNumber = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? this.pageSize;
    this.loadLogs();
  }

  goToPage(page: number): void {
    this.onPageChange({ page: page - 1, rows: this.pageSize, first: (page - 1) * this.pageSize } as PaginatorState);
  }

  onPageSizeChange(size: number): void {
    this.onPageChange({ page: 0, rows: size, first: 0 } as PaginatorState);
  }

  onSort(event: any): void {
    this.sortField = event.field;
    this.sortOrder = event.order;
    this.loadLogs();
  }

  onSearch(): void {
    this.pageNumber = 1;
    this.loadLogs();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.pageNumber = 1;
    this.loadLogs();
  }

  onFilterChange(): void {
    this.pageNumber = 1;
    this.loadLogs();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedEntity = null;
    this.selectedAction = null;
    this.selectedUser = null;
    this.dateRange = [];
    this.pageNumber = 1;
    this.loadLogs();
  }

  getActionSeverity(action: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (action?.toUpperCase()) {
      case 'INSERT': return 'success';
      case 'UPDATE': return 'info';
      case 'DELETE': return 'danger';
      default: return 'secondary';
    }
  }

  viewTimeline(log: AuditLogResponse): void {
    this.dialogService.open(EntityTimelineDialogComponent, {
      header: `${this.i18n.t('audit.dashboard.timelinePrefix')} ${log.entityName} #${log.entityId.substring(0, 8)}...`,
      width: '70vw',
      modal: true,
      data: {
        entityName: log.entityName,
        entityId: log.entityId
      }
    });
  }

  exportLogs(format: 'csv' | 'json'): void {
    const filter: AuditLogFilterRequest = {};
    if (this.selectedEntity) filter.entityName = this.selectedEntity;
    if (this.selectedAction) filter.action = this.selectedAction;
    if (this.selectedUser) filter.performedBy = this.selectedUser;
    if (this.dateRange[0]) filter.fromDate = this.dateRange[0].toISOString();
    if (this.dateRange[1]) filter.toDate = this.dateRange[1].toISOString();

    this.auditService.exportAuditLogs(filter, format).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `audit_logs_${new Date().toISOString().split('T')[0]}.${format}`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('audit.logs.messages.exportCompleteSummary'),
          detail: this.i18n.t('audit.logs.messages.exportSuccessFormat', { format: format.toUpperCase() })
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('audit.logs.messages.errorSummary'),
          detail: this.i18n.t('audit.logs.messages.exportFailed')
        });
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }

  truncateValue(value: string | null, maxLength: number = 50): string {
    if (!value) return '-';
    return value.length > maxLength ? value.substring(0, maxLength) + '...' : value;
  }

  get first(): number {
    return (this.pageNumber - 1) * this.pageSize;
  }
}
