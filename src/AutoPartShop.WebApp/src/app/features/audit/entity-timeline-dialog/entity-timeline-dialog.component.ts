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
  template: `
    <div class="timeline-dialog">
      <!-- Loading State -->
      <div *ngIf="loading" class="loading-state">
        <div *ngFor="let i of [1,2,3]" class="mb-4">
          <p-skeleton height="100px" styleClass="mb-2"></p-skeleton>
        </div>
      </div>

      <!-- Timeline Content -->
      <div *ngIf="!loading && timeline" class="timeline-content">
        <!-- Summary -->
        <div class="timeline-summary mb-4">
          <div class="summary-item">
            <span class="summary-label">Entity:</span>
            <span class="summary-value">{{ timeline.entityName }}</span>
          </div>
          <div class="summary-item">
            <span class="summary-label">ID:</span>
            <span class="summary-value id-value">{{ timeline.entityId }}</span>
          </div>
          <div class="summary-item">
            <span class="summary-label">Total Events:</span>
            <span class="summary-value">{{ timeline.events.length }}</span>
          </div>
        </div>

        <!-- Timeline -->
        <p-timeline [value]="timeline.events" align="left" styleClass="custom-timeline">
          <ng-template pTemplate="marker" let-event>
            <span class="timeline-marker" [ngClass]="getMarkerClass(event.action)">
              <i [class]="getActionIcon(event.action)"></i>
            </span>
          </ng-template>
          
          <ng-template pTemplate="content" let-event>
            <div class="timeline-event-card">
              <div class="event-header">
                <p-tag [value]="event.action" [severity]="getActionSeverity(event.action)"></p-tag>
                <span class="event-time">{{ event.timestamp | date:'medium' }}</span>
              </div>
              
              <div class="event-meta">
                <span class="event-user">
                  <i class="pi pi-user"></i>
                  {{ event.performedBy }}
                </span>
                <span class="event-ip" *ngIf="event.ipAddress">
                  <i class="pi pi-globe"></i>
                  {{ event.ipAddress }}
                </span>
              </div>

              <!-- Changes Table -->
              <div class="changes-section" *ngIf="event.changes && event.changes.length > 0">
                <p-table [value]="event.changes" styleClass="p-datatable-sm changes-table">
                  <ng-template pTemplate="header">
                    <tr>
                      <th style="width: 30%">Property</th>
                      <th style="width: 35%">Old Value</th>
                      <th style="width: 35%">New Value</th>
                    </tr>
                  </ng-template>
                  <ng-template pTemplate="body" let-change>
                    <tr>
                      <td class="property-cell">{{ change.propertyName }}</td>
                      <td>
                        <span class="old-value" *ngIf="change.oldValue" [pTooltip]="change.oldValue">
                          {{ truncate(change.oldValue) }}
                        </span>
                        <span class="no-value" *ngIf="!change.oldValue">-</span>
                      </td>
                      <td>
                        <span class="new-value" *ngIf="change.newValue" [pTooltip]="change.newValue">
                          {{ truncate(change.newValue) }}
                        </span>
                        <span class="no-value" *ngIf="!change.newValue">-</span>
                      </td>
                    </tr>
                  </ng-template>
                </p-table>
              </div>

              <div class="no-changes" *ngIf="!event.changes || event.changes.length === 0">
                <i class="pi pi-info-circle"></i>
                No property changes recorded
              </div>
            </div>
          </ng-template>
        </p-timeline>

        <!-- Empty State -->
        <div *ngIf="timeline.events.length === 0" class="empty-state">
          <i class="pi pi-inbox"></i>
          <h3>No history found</h3>
          <p>No audit events recorded for this entity</p>
        </div>
      </div>

      <!-- Error State -->
      <div *ngIf="!loading && error" class="error-state">
        <i class="pi pi-exclamation-triangle"></i>
        <h3>Failed to load timeline</h3>
        <p>{{ error }}</p>
        <button pButton type="button" label="Retry" icon="pi pi-refresh" (click)="loadTimeline()"></button>
      </div>

      <!-- Footer -->
      <div class="dialog-footer">
        <button pButton type="button" label="Close" class="p-button-text" (click)="close()"></button>
      </div>
    </div>
  `,
  styles: [`
    .timeline-dialog {
      padding: 0.5rem;
    }

    .loading-state {
      padding: 1rem;
    }

    .timeline-summary {
      display: flex;
      flex-wrap: wrap;
      gap: 1.5rem;
      padding: 1rem;
      background: var(--surface-ground);
      border-radius: 8px;
    }

    .summary-item {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .summary-label {
      font-size: 0.8rem;
      color: var(--text-color-secondary);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .summary-value {
      font-weight: 600;
      color: var(--text-color);
    }

    .id-value {
      font-family: monospace;
      font-size: 0.9rem;
    }

    /* Timeline */
    :host ::ng-deep .custom-timeline .p-timeline-event-opposite {
      display: none;
    }

    .timeline-marker {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-size: 0.9rem;
    }

    .timeline-marker.insert {
      background: #22c55e;
    }

    .timeline-marker.update {
      background: #3b82f6;
    }

    .timeline-marker.delete {
      background: #ef4444;
    }

    .timeline-marker.default {
      background: #6b7280;
    }

    .timeline-event-card {
      background: var(--surface-card);
      border: 1px solid var(--surface-border);
      border-radius: 8px;
      padding: 1rem;
      margin-bottom: 0.5rem;
    }

    .event-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 0.75rem;
    }

    .event-time {
      font-size: 0.85rem;
      color: var(--text-color-secondary);
    }

    .event-meta {
      display: flex;
      gap: 1rem;
      margin-bottom: 0.75rem;
      font-size: 0.85rem;
      color: var(--text-color-secondary);
    }

    .event-meta i {
      margin-right: 0.25rem;
    }

    .changes-section {
      margin-top: 0.75rem;
    }

    :host ::ng-deep .changes-table {
      font-size: 0.85rem;
    }

    :host ::ng-deep .changes-table th {
      background: var(--surface-ground);
      padding: 0.5rem;
    }

    :host ::ng-deep .changes-table td {
      padding: 0.5rem;
    }

    .property-cell {
      font-family: monospace;
      font-weight: 500;
      color: var(--primary-color);
    }

    .old-value {
      background: rgba(239, 68, 68, 0.1);
      color: #dc2626;
      padding: 0.2rem 0.4rem;
      border-radius: 4px;
      font-family: monospace;
      font-size: 0.8rem;
      cursor: help;
    }

    .new-value {
      background: rgba(34, 197, 94, 0.1);
      color: #16a34a;
      padding: 0.2rem 0.4rem;
      border-radius: 4px;
      font-family: monospace;
      font-size: 0.8rem;
      cursor: help;
    }

    .no-value {
      color: var(--text-color-secondary);
      font-style: italic;
    }

    .no-changes {
      padding: 0.75rem;
      background: var(--surface-ground);
      border-radius: 4px;
      color: var(--text-color-secondary);
      font-size: 0.85rem;
      text-align: center;
    }

    .no-changes i {
      margin-right: 0.5rem;
    }

    .empty-state,
    .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 3rem;
      text-align: center;
    }

    .empty-state i,
    .error-state i {
      font-size: 3rem;
      color: var(--text-color-secondary);
      margin-bottom: 1rem;
    }

    .empty-state h3,
    .error-state h3 {
      margin: 0 0 0.5rem 0;
      color: var(--text-color);
    }

    .empty-state p,
    .error-state p {
      margin: 0 0 1rem 0;
      color: var(--text-color-secondary);
    }

    .dialog-footer {
      display: flex;
      justify-content: flex-end;
      padding-top: 1rem;
      border-top: 1px solid var(--surface-border);
      margin-top: 1rem;
    }
  `]
})
export class EntityTimelineDialogComponent implements OnInit {
  private readonly config = inject(DynamicDialogConfig);
  private readonly ref = inject(DynamicDialogRef);
  private readonly auditService = inject(AuditTrailService);

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
        this.error = 'Failed to load entity timeline. Please try again.';
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
