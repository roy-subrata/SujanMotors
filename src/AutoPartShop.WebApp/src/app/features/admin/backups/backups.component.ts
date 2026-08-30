import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { AppSettingsService } from '../../../shared/services/app-settings.service';
import { BackupService, BackupRecord, DriveStatus } from '../../../shared/services/backup.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { BackupRecordStatus } from '@/shared/models/status.types';
import { StatusDisplayService, StatusSeverity } from '@/shared/services/status-display.service';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
  selector: 'app-backups',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    ToggleSwitchModule,
    ToastModule,
    CardModule,
    TableModule,
    TagModule,
    DialogModule,
    TooltipModule,
    PageContainerComponent,
    PageHeaderComponent,
    DataPaginationComponent,
  ],
  providers: [MessageService],
  templateUrl: './backups.component.html',
  styleUrl: './backups.component.scss',
})
export class BackupsComponent implements OnInit, OnDestroy {
  private readonly settingsService = inject(AppSettingsService);
  private readonly backupService = inject(BackupService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  private readonly statusDisplayService = inject(StatusDisplayService);
  readonly i18n = inject(I18nService);

  settingsLoading = signal(true);
  settingsSaving = signal(false);
  historyLoading = signal(false);
  restoring = signal(false);
  downloadingId = signal<string | null>(null);
  operationRunning = signal(false);
  driveStatus = signal<DriveStatus | null>(null);
  records = signal<BackupRecord[]>([]);

  totalRecords = 0;
  page = 1;
  pageSize = 10;

  restoreDialogVisible = false;
  restoreTarget: BackupRecord | null = null;
  restoreConfirmation = '';

  private pollTimer: ReturnType<typeof setInterval> | null = null;

  form: FormGroup = this.fb.group({
    enabled: [false],
    localTime: ['02:00', Validators.required],
    retentionCount: [14, [Validators.required, Validators.min(1), Validators.max(365)]],
    gdriveFolderId: [''],
  });

  ngOnInit(): void {
    this.loadSettings();
    this.loadHistory();
    this.loadDriveStatus();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  // ── Settings ────────────────────────────────────────────────────────────

  loadSettings(): void {
    this.settingsService.getByCategory('BACKUP').subscribe({
      next: settings => {
        const get = (key: string) => settings.find(s => s.key === key)?.value;
        this.form.patchValue({
          enabled: (get('BACKUP:ENABLED') ?? 'false').toLowerCase() === 'true',
          localTime: get('BACKUP:LOCAL_TIME') ?? '02:00',
          retentionCount: parseInt(get('BACKUP:RETENTION_COUNT') ?? '14', 10),
          gdriveFolderId: get('BACKUP:GDRIVE_FOLDER_ID') ?? '',
        });
        this.settingsLoading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('backups.messages.loadSettingsFailed') });
        this.settingsLoading.set(false);
      },
    });
  }

  saveSettings(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.settingsSaving.set(true);
    const v = this.form.value;

    const updates = [
      this.settingsService.update('BACKUP:ENABLED', { value: String(v.enabled), dataType: 'BOOL', category: 'BACKUP', isSystemSetting: true }),
      this.settingsService.update('BACKUP:LOCAL_TIME', { value: v.localTime, dataType: 'STRING', category: 'BACKUP', isSystemSetting: true }),
      this.settingsService.update('BACKUP:RETENTION_COUNT', { value: String(v.retentionCount), dataType: 'INT', category: 'BACKUP', isSystemSetting: true }),
      this.settingsService.update('BACKUP:GDRIVE_FOLDER_ID', { value: v.gdriveFolderId ?? '', dataType: 'STRING', category: 'BACKUP', isSystemSetting: true }),
    ];

    let completed = 0;
    let failed = false;
    for (const update$ of updates) {
      update$.subscribe({
        next: () => {
          completed++;
          if (completed === updates.length && !failed) {
            this.messageService.add({ severity: 'success', summary: this.i18n.t('backups.messages.settingsSavedSummary'), detail: this.i18n.t('backups.messages.settingsSavedDetail') });
            this.settingsSaving.set(false);
            this.loadDriveStatus();
          }
        },
        error: () => {
          if (!failed) {
            failed = true;
            this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('backups.messages.saveSettingsFailed') });
            this.settingsSaving.set(false);
          }
        },
      });
    }
  }

  loadDriveStatus(): void {
    this.backupService.getDriveStatus().subscribe({
      next: status => this.driveStatus.set(status),
      error: () => this.driveStatus.set({ configured: false, ok: false, serviceAccountEmail: null, error: this.i18n.t('backups.messages.driveStatusCheckFailed') }),
    });
  }

  // ── History ─────────────────────────────────────────────────────────────

  loadHistory(): void {
    this.historyLoading.set(true);
    this.backupService.getHistory(this.page, this.pageSize).subscribe({
      next: response => {
        this.records.set(response.data);
        this.totalRecords = response.pagination.totalCount;
        this.historyLoading.set(false);

        const running = response.data.some(r => r.status === 'Running' || r.status === 'Pending');
        this.operationRunning.set(running || this.restoring());
        if (running) this.startPolling();
        else this.stopPolling();
      },
      error: () => {
        this.historyLoading.set(false);
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('backups.messages.loadHistoryFailed') });
      },
    });
  }

  goToPage(page: number): void {
    this.page = page;
    this.loadHistory();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.loadHistory();
  }

  // ── Actions ─────────────────────────────────────────────────────────────

  backupNow(): void {
    this.operationRunning.set(true);
    this.backupService.runBackup().subscribe({
      next: () => {
        this.messageService.add({ severity: 'info', summary: this.i18n.t('backups.messages.backupStartedSummary'), detail: this.i18n.t('backups.messages.backupStartedDetail') });
        this.page = 1;
        this.loadHistory();
        this.startPolling();
      },
      error: err => {
        this.operationRunning.set(false);
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.message ?? this.i18n.t('backups.messages.startBackupFailed') });
      },
    });
  }

  download(record: BackupRecord): void {
    this.downloadingId.set(record.id);
    this.backupService.download(record.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = record.fileName;
        anchor.click();
        URL.revokeObjectURL(url);
        this.downloadingId.set(null);
      },
      error: () => {
        this.downloadingId.set(null);
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('backups.messages.downloadFailed') });
      },
    });
  }

  openRestoreDialog(record: BackupRecord): void {
    this.restoreTarget = record;
    this.restoreConfirmation = '';
    this.restoreDialogVisible = true;
  }

  confirmRestore(): void {
    if (!this.restoreTarget || this.restoreConfirmation !== 'RESTORE') return;
    this.restoring.set(true);
    this.operationRunning.set(true);

    this.backupService.restore(this.restoreTarget.id, this.restoreConfirmation).subscribe({
      next: result => {
        this.restoring.set(false);
        this.operationRunning.set(false);
        this.restoreDialogVisible = false;
        this.messageService.add({ severity: 'success', summary: this.i18n.t('backups.messages.restoredSummary'), detail: result.message, life: 8000 });
        this.loadHistory();
      },
      error: err => {
        this.restoring.set(false);
        this.operationRunning.set(false);
        this.messageService.add({ severity: 'error', summary: this.i18n.t('backups.messages.restoreFailedSummary'), detail: err?.error?.message ?? this.i18n.t('backups.messages.restoreFailedDetail'), life: 10000 });
      },
    });
  }

  // ── Polling while a backup runs ─────────────────────────────────────────

  private startPolling(): void {
    if (this.pollTimer) return;
    this.pollTimer = setInterval(() => this.loadHistory(), 3000);
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  // ── Display helpers ─────────────────────────────────────────────────────

  isRestorable(record: BackupRecord): boolean {
    return record.status === 'Succeeded' || record.status === 'UploadFailed';
  }

  statusSeverity(status: BackupRecordStatus): StatusSeverity {
    return this.statusDisplayService.getSeverity(status, 'backup');
  }

  triggerSeverity(trigger: string): 'info' | 'secondary' | 'warn' {
    switch (trigger) {
      case 'Scheduled': return 'info';
      case 'PreRestore': return 'warn';
      default: return 'secondary';
    }
  }

  statusLabel(status: BackupRecordStatus): string {
    const map: Record<BackupRecordStatus, string> = {
      Pending: 'backups.statuses.pending',
      Running: 'backups.statuses.running',
      Succeeded: 'backups.statuses.succeeded',
      UploadFailed: 'backups.statuses.uploadFailed',
      Failed: 'backups.statuses.failed',
    };
    return this.i18n.t(map[status] ?? status);
  }

  triggerLabel(trigger: 'Manual' | 'Scheduled' | 'PreRestore'): string {
    const map: Record<string, string> = {
      Manual: 'backups.triggers.manual',
      Scheduled: 'backups.triggers.scheduled',
      PreRestore: 'backups.triggers.preRestore',
    };
    return this.i18n.t(map[trigger] ?? trigger);
  }

  formatSize(bytes: number): string {
    if (!bytes) return '—';
    const units = ['B', 'KB', 'MB', 'GB'];
    let value = bytes;
    let unit = 0;
    while (value >= 1024 && unit < units.length - 1) {
      value /= 1024;
      unit++;
    }
    return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
  }
}
