import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ChartModule } from 'primeng/chart';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { DialogService, DynamicDialogModule } from 'primeng/dynamicdialog';
import {
  AuditTrailService,
  AuditDashboardResponse,
  AuditLogResponse,
  EntityChangeCount,
  UserActivityCount
} from '../../../shared/services/audit-trail.service';
import { EntityTimelineDialogComponent } from '../entity-timeline-dialog/entity-timeline-dialog.component';

@Component({
  selector: 'app-audit-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    CardModule,
    ChartModule,
    TableModule,
    TagModule,
    ButtonModule,
    SkeletonModule,
    TooltipModule,
    DatePickerModule,
    SelectModule,
    ToastModule,
    DynamicDialogModule
  ],
  providers: [MessageService, DialogService],
  templateUrl: './audit-dashboard.component.html',
  styleUrls: ['./audit-dashboard.component.css']
})
export class AuditDashboardComponent implements OnInit {
  private readonly auditService = inject(AuditTrailService);
  private readonly messageService = inject(MessageService);
  private readonly dialogService = inject(DialogService);

  dashboard: AuditDashboardResponse | null = null;
  loading = true;

  // Filter options
  dateRange: Date[] = [];
  selectedEntity: string | null = null;
  entities: { label: string; value: string }[] = [];

  // Chart data
  trendChartData: any;
  trendChartOptions: any;
  actionPieData: any;
  actionPieOptions: any;
  hourlyChartData: any;
  hourlyChartOptions: any;

  ngOnInit(): void {
    this.initChartOptions();
    this.loadEntities();
    this.loadDashboard();
  }

  loadEntities(): void {
    this.auditService.getAuditedEntities().subscribe({
      next: (entities) => {
        this.entities = [
          { label: 'All Entities', value: '' },
          ...entities.map(e => ({ label: e, value: e }))
        ];
      }
    });
  }

  loadDashboard(): void {
    this.loading = true;
    const fromDate = this.dateRange[0] || undefined;
    const toDate = this.dateRange[1] || undefined;

    this.auditService.getDashboard(fromDate, toDate, this.selectedEntity || undefined).subscribe({
      next: (data) => {
        this.dashboard = data;
        this.updateCharts();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading dashboard:', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load audit dashboard'
        });
        this.loading = false;
      }
    });
  }

  onFilterChange(): void {
    this.loadDashboard();
  }

  initChartOptions(): void {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColor = documentStyle.getPropertyValue('--text-color') || '#495057';
    const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary') || '#6c757d';
    const surfaceBorder = documentStyle.getPropertyValue('--surface-border') || '#dee2e6';

    this.trendChartOptions = {
      maintainAspectRatio: false,
      aspectRatio: 0.6,
      plugins: {
        legend: {
          labels: { color: textColor }
        }
      },
      scales: {
        x: {
          ticks: { color: textColorSecondary },
          grid: { color: surfaceBorder }
        },
        y: {
          ticks: { color: textColorSecondary },
          grid: { color: surfaceBorder }
        }
      }
    };

    this.actionPieOptions = {
      plugins: {
        legend: {
          labels: { color: textColor },
          position: 'bottom'
        }
      }
    };

    this.hourlyChartOptions = {
      maintainAspectRatio: false,
      aspectRatio: 0.8,
      plugins: {
        legend: { display: false }
      },
      scales: {
        x: {
          ticks: { color: textColorSecondary },
          grid: { display: false }
        },
        y: {
          ticks: { color: textColorSecondary },
          grid: { color: surfaceBorder }
        }
      }
    };
  }

  updateCharts(): void {
    if (!this.dashboard) return;

    // Trend chart
    const labels = this.dashboard.dailyTrends.map(t => new Date(t.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }));
    this.trendChartData = {
      labels,
      datasets: [
        {
          label: 'Inserts',
          data: this.dashboard.dailyTrends.map(t => t.insertCount),
          fill: false,
          borderColor: '#22c55e',
          backgroundColor: '#22c55e',
          tension: 0.4
        },
        {
          label: 'Updates',
          data: this.dashboard.dailyTrends.map(t => t.updateCount),
          fill: false,
          borderColor: '#3b82f6',
          backgroundColor: '#3b82f6',
          tension: 0.4
        },
        {
          label: 'Deletes',
          data: this.dashboard.dailyTrends.map(t => t.deleteCount),
          fill: false,
          borderColor: '#ef4444',
          backgroundColor: '#ef4444',
          tension: 0.4
        }
      ]
    };

    // Action pie chart
    this.actionPieData = {
      labels: this.dashboard.actionBreakdown.map(a => a.action),
      datasets: [
        {
          data: this.dashboard.actionBreakdown.map(a => a.count),
          backgroundColor: ['#22c55e', '#3b82f6', '#ef4444', '#f59e0b'],
          hoverBackgroundColor: ['#16a34a', '#2563eb', '#dc2626', '#d97706']
        }
      ]
    };

    // Hourly chart
    this.hourlyChartData = {
      labels: this.dashboard.hourlyDistribution.map(h => `${h.hour}:00`),
      datasets: [
        {
          label: 'Activity',
          data: this.dashboard.hourlyDistribution.map(h => h.activityCount),
          backgroundColor: '#6366f1',
          borderRadius: 4
        }
      ]
    };
  }

  getActionSeverity(action: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (action?.toUpperCase()) {
      case 'INSERT': return 'success';
      case 'UPDATE': return 'info';
      case 'DELETE': return 'danger';
      default: return 'secondary';
    }
  }

  formatTimeAgo(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  }

  viewEntityTimeline(log: AuditLogResponse): void {
    this.dialogService.open(EntityTimelineDialogComponent, {
      header: `Timeline: ${log.entityName} #${log.entityId}`,
      width: '70vw',
      modal: true,
      data: {
        entityName: log.entityName,
        entityId: log.entityId
      }
    });
  }

  exportDashboard(): void {
    this.auditService.exportAuditLogs({}, 'csv').subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `audit_export_${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.messageService.add({
          severity: 'success',
          summary: 'Export Complete',
          detail: 'Audit logs exported successfully'
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to export audit logs'
        });
      }
    });
  }
}
