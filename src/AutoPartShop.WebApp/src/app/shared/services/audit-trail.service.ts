import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface AuditLogResponse {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  propertyName: string;
  oldValue: string | null;
  newValue: string | null;
  performedBy: string;
  performedAt: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface AuditLogSummary {
  entityName: string;
  entityId: string;
  action: string;
  performedBy: string;
  performedAt: string;
  changesCount: number;
  changes: PropertyChange[];
}

export interface PropertyChange {
  propertyName: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface AuditStatistics {
  totalChanges: number;
  insertCount: number;
  updateCount: number;
  deleteCount: number;
  firstChangeDate: string | null;
  lastChangeDate: string | null;
  entityChanges: EntityChangeCount[];
  userActivities: UserActivityCount[];
}

export interface EntityChangeCount {
  entityName: string;
  changeCount: number;
}

export interface UserActivityCount {
  userName: string;
  activityCount: number;
}

export interface PaginatedAuditLogResponse {
  data: AuditLogResponse[];
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

// New Dashboard DTOs
export interface AuditDashboardResponse {
  overview: AuditOverview;
  dailyTrends: ActivityTrend[];
  hourlyDistribution: HourlyActivity[];
  topEntities: EntityChangeCount[];
  topUsers: UserActivityCount[];
  actionBreakdown: ActionDistribution[];
  recentActivity: AuditLogResponse[];
}

export interface AuditOverview {
  totalChangesToday: number;
  totalChangesThisWeek: number;
  totalChangesThisMonth: number;
  totalChangesAllTime: number;
  uniqueUsersToday: number;
  uniqueEntitiesModified: number;
  averageChangesPerDay: number;
  lastActivityTime: string | null;
}

export interface ActivityTrend {
  date: string;
  insertCount: number;
  updateCount: number;
  deleteCount: number;
  totalCount: number;
}

export interface HourlyActivity {
  hour: number;
  activityCount: number;
}

export interface ActionDistribution {
  action: string;
  count: number;
  percentage: number;
}

export interface EntityTimeline {
  entityName: string;
  entityId: string;
  events: TimelineEvent[];
}

export interface TimelineEvent {
  timestamp: string;
  action: string;
  performedBy: string;
  changes: PropertyChange[];
  ipAddress?: string;
}

export interface UserActivitySummary {
  userName: string;
  totalActions: number;
  insertCount: number;
  updateCount: number;
  deleteCount: number;
  firstActivityDate: string | null;
  lastActivityDate: string | null;
  entityBreakdown: EntityChangeCount[];
  actionBreakdown: ActionDistribution[];
}

export interface EntityStateComparison {
  entityName: string;
  entityId: string;
  fromTimestamp: string | null;
  toTimestamp: string | null;
  propertyChanges: PropertyStateChange[];
  totalChanges: number;
}

export interface PropertyStateChange {
  propertyName: string;
  originalValue: string | null;
  currentValue: string | null;
  lastChangedAt: string | null;
  lastChangedBy: string | null;
}

export interface AuditLogFilterRequest {
  pageNumber?: number;
  pageSize?: number;
  entityName?: string;
  entityId?: string;
  action?: string;
  performedBy?: string;
  propertyName?: string;
  searchTerm?: string;
  fromDate?: string;
  toDate?: string;
  ipAddress?: string;
  sortBy?: string;
  sortDescending?: boolean;
  entityNames?: string[];
  actions?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuditTrailService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/auditlog`;

  /**
   * Get paginated audit logs with filters
   */
  getAuditLogs(filter: AuditLogFilterRequest = {}): Observable<PaginatedAuditLogResponse> {
    let params = new HttpParams();

    if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.entityName) params = params.set('entityName', filter.entityName);
    if (filter.entityId) params = params.set('entityId', filter.entityId);
    if (filter.action) params = params.set('action', filter.action);
    if (filter.performedBy) params = params.set('performedBy', filter.performedBy);
    if (filter.propertyName) params = params.set('propertyName', filter.propertyName);
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter.toDate) params = params.set('toDate', filter.toDate);
    if (filter.ipAddress) params = params.set('ipAddress', filter.ipAddress);
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params = params.set('sortDescending', filter.sortDescending.toString());

    return this.http.get<PaginatedAuditLogResponse>(`${this.apiUrl}/list`, { params });
  }

  /**
   * Get dashboard insights
   */
  getDashboard(fromDate?: Date, toDate?: Date, entityName?: string, topCount: number = 10): Observable<AuditDashboardResponse> {
    let params = new HttpParams().set('topCount', topCount.toString());

    if (fromDate) params = params.set('fromDate', fromDate.toISOString());
    if (toDate) params = params.set('toDate', toDate.toISOString());
    if (entityName) params = params.set('entityName', entityName);

    return this.http.get<AuditDashboardResponse>(`${this.apiUrl}/dashboard`, { params });
  }

  /**
   * Get entity timeline
   */
  getEntityTimeline(entityName: string, entityId: string): Observable<EntityTimeline> {
    return this.http.get<EntityTimeline>(`${this.apiUrl}/entity/${entityName}/${entityId}/timeline`);
  }

  /**
   * Compare entity states
   */
  compareEntityStates(entityName: string, entityId: string, fromTimestamp?: Date, toTimestamp?: Date): Observable<EntityStateComparison> {
    let params = new HttpParams();
    if (fromTimestamp) params = params.set('fromTimestamp', fromTimestamp.toISOString());
    if (toTimestamp) params = params.set('toTimestamp', toTimestamp.toISOString());

    return this.http.get<EntityStateComparison>(`${this.apiUrl}/entity/${entityName}/${entityId}/compare`, { params });
  }

  /**
   * Get user activity summary
   */
  getUserActivitySummary(userName: string, fromDate?: Date, toDate?: Date): Observable<UserActivitySummary> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate.toISOString());
    if (toDate) params = params.set('toDate', toDate.toISOString());

    return this.http.get<UserActivitySummary>(`${this.apiUrl}/user/${userName}/summary`, { params });
  }

  /**
   * Get activity trends
   */
  getActivityTrends(fromDate?: Date, toDate?: Date, entityName?: string): Observable<ActivityTrend[]> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate.toISOString());
    if (toDate) params = params.set('toDate', toDate.toISOString());
    if (entityName) params = params.set('entityName', entityName);

    return this.http.get<ActivityTrend[]>(`${this.apiUrl}/trends`, { params });
  }

  /**
   * Get list of audited entities
   */
  getAuditedEntities(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/entities`);
  }

  /**
   * Get list of audit users
   */
  getAuditUsers(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/users`);
  }

  /**
   * Export audit logs (returns blob)
   */
  exportAuditLogs(filter: AuditLogFilterRequest, format: 'csv' | 'json' = 'csv'): Observable<Blob> {
    let params = new HttpParams().set('format', format === 'json' ? '1' : '0');

    if (filter.entityName) params = params.set('entityName', filter.entityName);
    if (filter.action) params = params.set('action', filter.action);
    if (filter.performedBy) params = params.set('performedBy', filter.performedBy);
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter.toDate) params = params.set('toDate', filter.toDate);

    return this.http.get(`${this.apiUrl}/export`, { params, responseType: 'blob' });
  }
}
