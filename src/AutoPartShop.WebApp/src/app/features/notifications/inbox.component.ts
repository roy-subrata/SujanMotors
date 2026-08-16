import { Component, OnInit, inject, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { DialogModule } from 'primeng/dialog';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { I18nService } from '@/shared/services/i18n.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import {
  InboxNotificationService,
  InboxNotification,
  InboxNotificationPayload
} from './inbox-notifications.service';

@Component({
  selector: 'app-inbox',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    BadgeModule,
    DialogModule,
    SelectButtonModule,
    TagModule,
    ToastModule,
    TooltipModule,
    PageContainerComponent,
    PageHeaderComponent,
    DataPaginationComponent
  ],
  providers: [MessageService],
  templateUrl: './inbox.component.html',
  styles: [`
    :host ::ng-deep .p-datatable .p-datatable-thead > tr > th {
      background-color: var(--surface-ground);
      font-weight: 600;
    }

    .inbox-row-unread {
      font-weight: 600;
      background-color: color-mix(in srgb, var(--primary-color) 4%, transparent);
    }

    .inbox-row-unread .inbox-title {
      color: var(--text-color);
    }

    .inbox-title {
      color: var(--text-color-secondary);
    }

    .unread-dot {
      display: inline-block;
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background-color: var(--primary-color);
    }

    .unread-indicator {
      display: inline-flex;
      align-items: center;
      gap: 6px;
    }
  `]
})
export class InboxComponent implements OnInit {
  private readonly service = inject(InboxNotificationService);
  private readonly messageService = inject(MessageService);
  readonly i18n = inject(I18nService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  notifications = signal<InboxNotification[]>([]);
  loading = signal(false);
  totalCount = 0;
  page = 1;
  pageSize = 20;
  first = 0;
  unreadOnly = signal(false);
  unreadFilter: 'ALL' | 'UNREAD' = 'ALL';
  filterOptions = computed(() => [
    { label: this.i18n.t('notifications.filterAll'), value: 'ALL' },
    { label: this.i18n.t('notifications.filterUnread'), value: 'UNREAD' }
  ]);

  unreadCount = signal(0);

  detailVisible = false;
  selected: InboxNotification | null = null;
  selectedPayload: InboxNotificationPayload | null = null;

  pageTitle = computed(() => this.i18n.t('notifications.title'));
  unreadCountLabel = computed(() => this.i18n.t('notifications.unreadCount', { count: String(this.unreadCount()) }));

  ngOnInit(): void {
    this.load();
    this.service.unreadCount$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(count => this.unreadCount.set(count));
  }

  load(): void {
    this.loading.set(true);
    this.service.getNotifications({
      unreadOnly: this.unreadOnly(),
      page: this.page,
      pageSize: this.pageSize
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.notifications.set(res.data);
        this.totalCount = res.pagination.totalCount;
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('notifications.messages.loadFailed')
        });
        this.loading.set(false);
      }
    });
  }

  onFilterChange(value: 'ALL' | 'UNREAD'): void {
    this.unreadOnly.set(value === 'UNREAD');
    this.page = 1;
    this.first = 0;
    this.load();
  }

  goToPage(page: number): void {
    this.page = page;
    this.first = (page - 1) * this.pageSize;
    this.load();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.first = 0;
    this.load();
  }

  openNotification(n: InboxNotification): void {
    if (!n.isRead) {
      this.markRead(n);
    }
    if (n.routerLink) {
      this.router.navigate([n.routerLink], n.queryParamsJson ? { queryParams: JSON.parse(n.queryParamsJson) } : {});
    }
  }

  showDetails(n: InboxNotification): void {
    this.selected = n;
    this.selectedPayload = null;
    if (n.payloadJson) {
      try {
        this.selectedPayload = JSON.parse(n.payloadJson);
      } catch {
        this.selectedPayload = null;
      }
    }
    this.detailVisible = true;
  }

  markRead(n: InboxNotification, isRead = true): void {
    if (n.isRead === isRead) return;
    this.service.markRead(n.id, isRead).subscribe({
      next: () => {
        this.notifications.update(ns => ns.map(x => x.id === n.id ? { ...x, isRead } : x));
        this.service.refreshUnreadCount();
      },
      error: () => { /* non-critical */ }
    });
  }

  markAllRead(): void {
    this.service.markAllRead().subscribe({
      next: () => {
        this.notifications.update(ns => ns.map(x => ({ ...x, isRead: true })));
        this.service.refreshUnreadCount();
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('notifications.messages.markAllReadSuccess')
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('notifications.messages.markAllReadFailed')
        });
      }
    });
  }

  typeLabel(type: string): string {
    switch (type) {
      case 'REORDER_ALERT':
        return this.i18n.t('notifications.types.reorderAlert');
      case 'SALE':
        return this.i18n.t('notifications.types.sale');
      default:
        return type;
    }
  }

  timeAgo(value: string | Date): string {
    const date = typeof value === 'string' ? new Date(value) : value;
    const seconds = Math.floor((Date.now() - date.getTime()) / 1000);
    if (seconds < 60) return this.i18n.t('common.time.justNow');
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return this.i18n.t('common.time.minutesAgo', { count: String(minutes) });
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return this.i18n.t('common.time.hoursAgo', { count: String(hours) });
    return this.i18n.t('common.time.daysAgo', { count: String(Math.floor(hours / 24)) });
  }
}
