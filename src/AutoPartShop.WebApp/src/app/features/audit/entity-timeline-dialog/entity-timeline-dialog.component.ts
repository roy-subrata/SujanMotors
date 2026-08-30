import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { TimelineModule } from 'primeng/timeline';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { TableModule } from 'primeng/table';
import {
  AuditTrailService,
  EntityTimeline,
  TimelineEvent,
  PropertyChange
} from '../../../shared/services/audit-trail.service';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
  selector: 'app-entity-timeline-dialog',
  standalone: true,
  imports: [
    CommonModule,
    TimelineModule,
    CardModule,
    TagModule,
    ButtonModule,
    SkeletonModule,
    TooltipModule,
    TableModule
  ],
  templateUrl: './entity-timeline-dialog.component.html',
  styleUrl: './entity-timeline-dialog.component.scss',
})
export class EntityTimelineDialogComponent implements OnInit {
  private readonly config = inject(DynamicDialogConfig);
  private readonly ref = inject(DynamicDialogRef);
  private readonly auditService = inject(AuditTrailService);
  readonly i18n = inject(I18nService);

  timeline: EntityTimeline | null = null;
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.loadTimeline();
  }

  loadTimeline(): void {
    const { entityName, entityId } = this.config.data;
    this.loading = true;
    this.error = null;

    this.auditService.getEntityTimeline(entityName, entityId).subscribe({
      next: (data) => {
        this.timeline = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading timeline:', err);
        this.error = this.i18n.t('audit.timelineDialog.errorMessage');
        this.loading = false;
      }
    });
  }

  getActionSeverity(action: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (action?.toUpperCase()) {
      case 'INSERT': return 'success';
      case 'UPDATE': return 'info';
      case 'DELETE': return 'danger';
      default: return 'secondary';
    }
  }

  getMarkerClass(action: string): string {
    switch (action?.toUpperCase()) {
      case 'INSERT': return 'insert';
      case 'UPDATE': return 'update';
      case 'DELETE': return 'delete';
      default: return 'default';
    }
  }

  getActionIcon(action: string): string {
    switch (action?.toUpperCase()) {
      case 'INSERT': return 'pi pi-plus';
      case 'UPDATE': return 'pi pi-pencil';
      case 'DELETE': return 'pi pi-trash';
      default: return 'pi pi-circle';
    }
  }

  truncate(value: string, maxLength: number = 40): string {
    if (!value) return '';
    return value.length > maxLength ? value.substring(0, maxLength) + '...' : value;
  }

  close(): void {
    this.ref.close();
  }
}
