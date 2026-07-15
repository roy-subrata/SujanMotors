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
  template: `
    <p-toast></p-toast>

    <app-page-container>
      <app-page-header
        title="Database Backups"
        subtitle="Scheduled backups, Google Drive upload and restore"
        [count]="totalRecords"
        countLabel="backups">
        <ng-container actions>
          <button class="btn-icon" (click)="loadHistory()" pTooltip="Refresh" tooltipPosition="bottom">
            <i class="pi pi-refresh"></i>
          </button>
          <button class="btn-primary" (click)="backupNow()" [disabled]="operationRunning()">
            <i class="pi" [class.pi-spin]="operationRunning()" [class.pi-spinner]="operationRunning()" [class.pi-play]="!operationRunning()"></i>
            <span>{{ operationRunning() ? 'Working…' : 'Backup Now' }}</span>
          </button>
        </ng-container>
      </app-page-header>

      <div class="w-full px-4 py-6">

        <!-- Schedule settings -->
        <p-card styleClass="mb-4">
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-calendar-clock text-blue-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">Backup Schedule</h2>
            </div>
          </ng-template>

          <div *ngIf="settingsLoading()" class="flex justify-center py-8">
            <i class="pi pi-spin pi-spinner text-2xl text-gray-400"></i>
          </div>

          <form *ngIf="!settingsLoading()" [formGroup]="form" (ngSubmit)="saveSettings()">
            <div class="flex items-center justify-between mb-5">
              <div>
                <p class="font-medium text-gray-700 m-0">Enable daily scheduled backups</p>
                <p class="text-sm text-gray-400 mt-1 m-0">Runs automatically at the time below (checked every 5 minutes)</p>
              </div>
              <p-toggleswitch formControlName="enabled"></p-toggleswitch>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-600 mb-2">Backup time (shop local)</label>
                <input type="time" pInputText formControlName="localTime" class="w-full" />
                <small class="text-gray-400">Daily backup time on the shop's clock</small>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-600 mb-2">Backups to keep</label>
                <p-inputNumber
                  formControlName="retentionCount"
                  [min]="1" [max]="365"
                  [showButtons]="true"
                  styleClass="w-full"
                  inputStyleClass="w-full">
                </p-inputNumber>
                <small class="text-gray-400">Older backups are deleted locally and on Google Drive</small>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-600 mb-2">Google Drive folder ID</label>
                <input pInputText formControlName="gdriveFolderId" placeholder="Leave empty to keep backups local only" class="w-full" />
                <small class="text-gray-400">The folder must be shared with the service account below</small>
              </div>
            </div>

            <!-- Drive status -->
            <div class="mt-4 p-3 rounded-lg border"
                 [class.bg-green-50]="driveStatus()?.ok"
                 [class.border-green-200]="driveStatus()?.ok"
                 [class.bg-amber-50]="driveStatus() && !driveStatus()?.ok"
                 [class.border-amber-200]="driveStatus() && !driveStatus()?.ok">
              <div class="flex items-center gap-2 text-sm">
                <i class="pi" [class.pi-cloud-upload]="driveStatus()?.ok" [class.text-green-600]="driveStatus()?.ok"
                   [class.pi-exclamation-triangle]="driveStatus() && !driveStatus()?.ok" [class.text-amber-600]="driveStatus() && !driveStatus()?.ok"></i>
                <span *ngIf="!driveStatus()" class="text-gray-500">
                  <i class="pi pi-spin pi-spinner mr-1"></i>Checking Google Drive connection…
                </span>
                <span *ngIf="driveStatus()?.ok" class="text-green-700">
                  Google Drive connected — backups will be uploaded.
                </span>
                <span *ngIf="driveStatus() && !driveStatus()?.ok" class="text-amber-700">
                  {{ driveStatus()?.error || 'Google Drive is not available; backups stay local only.' }}
                </span>
              </div>
              <div *ngIf="driveStatus()?.serviceAccountEmail" class="text-xs text-gray-500 mt-2">
                Share your Drive folder (as Editor) with:
                <code class="bg-gray-100 px-1 rounded">{{ driveStatus()?.serviceAccountEmail }}</code>
              </div>
            </div>

            <div class="flex justify-end mt-4">
              <button pButton type="submit" label="Save Settings" icon="pi pi-save"
                      [loading]="settingsSaving()" [disabled]="form.invalid || settingsSaving()"
                      class="p-button-success"></button>
            </div>
          </form>
        </p-card>

        <!-- History -->
        <p-card>
          <ng-template pTemplate="header">
            <div class="flex items-center gap-2 px-5 pt-4">
              <i class="pi pi-history text-gray-500 text-xl"></i>
              <h2 class="text-lg font-semibold text-gray-700 m-0">Backup History</h2>
            </div>
          </ng-template>

          <p-table [value]="records()" [loading]="historyLoading()" responsiveLayout="scroll" styleClass="p-datatable-sm">
            <ng-template pTemplate="header">
              <tr>
                <th>File</th>
                <th>Type</th>
                <th>Status</th>
                <th>Size</th>
                <th>Started</th>
                <th>By</th>
                <th style="width:110px">Actions</th>
              </tr>
            </ng-template>
            <ng-template pTemplate="body" let-record>
              <tr>
                <td>
                  <span class="font-mono text-sm">{{ record.fileName }}</span>
                  <i *ngIf="record.uploadedToDrive" class="pi pi-cloud-upload text-blue-500 ml-2"
                     pTooltip="Uploaded to Google Drive" tooltipPosition="top"></i>
                  <i *ngIf="!record.localFileExists && record.uploadedToDrive" class="pi pi-info-circle text-gray-400 ml-1"
                     pTooltip="Local file removed — will be re-downloaded from Drive when needed" tooltipPosition="top"></i>
                </td>
                <td><p-tag [value]="record.triggerType" [severity]="triggerSeverity(record.triggerType)"></p-tag></td>
                <td>
                  <p-tag [value]="record.status" [severity]="statusSeverity(record.status)"
                         [pTooltip]="record.errorMessage ?? ''" tooltipPosition="top"></p-tag>
                  <i *ngIf="record.status === 'Running'" class="pi pi-spin pi-spinner text-blue-500 ml-2"></i>
                </td>
                <td>{{ formatSize(record.sizeBytes) }}</td>
                <td>
                  <span [pTooltip]="((record.startedAt + 'Z') | date:'medium') ?? ''" tooltipPosition="top">
                    {{ (record.startedAt + 'Z') | date:'MMM d, HH:mm' }}
                  </span>
                </td>
                <td>{{ record.createdBy }}</td>
                <td>
                  <div class="flex gap-1">
                    <button pButton type="button" icon="pi pi-download" class="p-button-text p-button-sm"
                            pTooltip="Download .bak" tooltipPosition="top"
                            [disabled]="!isRestorable(record) || downloadingId() === record.id"
                            [loading]="downloadingId() === record.id"
                            (click)="download(record)"></button>
                    <button pButton type="button" icon="pi pi-replay" class="p-button-text p-button-sm p-button-danger"
                            pTooltip="Restore this backup" tooltipPosition="top"
                            [disabled]="!isRestorable(record) || operationRunning()"
                            (click)="openRestoreDialog(record)"></button>
                  </div>
                </td>
              </tr>
            </ng-template>
            <ng-template pTemplate="emptymessage">
              <tr>
                <td colspan="7" class="text-center text-gray-400 py-8">
                  No backups yet — click “Backup Now” or enable the schedule above.
                </td>
              </tr>
            </ng-template>
          </p-table>

          <app-data-pagination
            [first]="(page - 1) * pageSize"
            [pageSize]="pageSize"
            [totalRecords]="totalRecords"
            itemLabel="backups"
            (pageChange)="goToPage($event)"
            (pageSizeChange)="onPageSizeChange($event)">
          </app-data-pagination>
        </p-card>
      </div>
    </app-page-container>

    <!-- Restore confirmation -->
    <p-dialog header="Restore Database" [(visible)]="restoreDialogVisible" [modal]="true" [style]="{ width: '480px' }"
              [closable]="!restoring()">
      <div *ngIf="restoreTarget" class="flex flex-col gap-3">
        <div class="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700">
          <p class="font-semibold m-0 mb-2"><i class="pi pi-exclamation-triangle mr-1"></i>This will replace ALL current data</p>
          <ul class="m-0 pl-4 list-disc">
            <li>The database is rolled back to <strong>{{ (restoreTarget.startedAt + 'Z') | date:'medium' }}</strong> — everything entered after that is lost.</li>
            <li>A safety backup of the current state is taken automatically first.</li>
            <li>All users are briefly disconnected during the restore.</li>
            <li>Backup history itself also reverts to that point (files remain on disk/Drive).</li>
          </ul>
        </div>
        <p class="text-sm text-gray-600 m-0">
          Restoring <span class="font-mono">{{ restoreTarget.fileName }}</span>.
          Type <strong>RESTORE</strong> to confirm:
        </p>
        <input pInputText [(ngModel)]="restoreConfirmation" [disabled]="restoring()"
               placeholder="Type RESTORE" class="w-full" autocomplete="off" />
      </div>
      <ng-template pTemplate="footer">
        <button pButton type="button" label="Cancel" class="p-button-text" [disabled]="restoring()"
                (click)="restoreDialogVisible = false"></button>
        <button pButton type="button" label="Restore Database" icon="pi pi-replay" class="p-button-danger"
                [disabled]="restoreConfirmation !== 'RESTORE' || restoring()" [loading]="restoring()"
                (click)="confirmRestore()"></button>
      </ng-template>
    </p-dialog>
  `,
})
export class BackupsComponent implements OnInit, OnDestroy {
  private readonly settingsService = inject(AppSettingsService);
  private readonly backupService = inject(BackupService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);

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
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load backup settings' });
        this.settingsLoading.set(false);
      },
    });
  }

  saveSettings(): void {
    if (this.form.invalid) return;
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
            this.messageService.add({ severity: 'success', summary: 'Saved', detail: 'Backup settings updated — changes take effect within 5 minutes' });
            this.settingsSaving.set(false);
            this.loadDriveStatus();
          }
        },
        error: () => {
          if (!failed) {
            failed = true;
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to save one or more settings' });
            this.settingsSaving.set(false);
          }
        },
      });
    }
  }

  loadDriveStatus(): void {
    this.backupService.getDriveStatus().subscribe({
      next: status => this.driveStatus.set(status),
      error: () => this.driveStatus.set({ configured: false, ok: false, serviceAccountEmail: null, error: 'Could not check Google Drive status' }),
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
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load backup history' });
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
        this.messageService.add({ severity: 'info', summary: 'Backup started', detail: 'The backup is running in the background' });
        this.page = 1;
        this.loadHistory();
        this.startPolling();
      },
      error: err => {
        this.operationRunning.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message ?? 'Failed to start backup' });
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
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to download backup file' });
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
        this.messageService.add({ severity: 'success', summary: 'Restored', detail: result.message, life: 8000 });
        this.loadHistory();
      },
      error: err => {
        this.restoring.set(false);
        this.operationRunning.set(false);
        this.messageService.add({ severity: 'error', summary: 'Restore failed', detail: err?.error?.message ?? 'The restore failed — check server logs', life: 10000 });
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

  statusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 'Succeeded': return 'success';
      case 'Running': return 'info';
      case 'UploadFailed': return 'warn';
      case 'Failed': return 'danger';
      default: return 'secondary';
    }
  }

  triggerSeverity(trigger: string): 'info' | 'secondary' | 'warn' {
    switch (trigger) {
      case 'Scheduled': return 'info';
      case 'PreRestore': return 'warn';
      default: return 'secondary';
    }
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
