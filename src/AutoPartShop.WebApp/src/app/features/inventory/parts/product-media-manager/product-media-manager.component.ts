import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ProductMedia, ProductMediaService } from '../../services/product-media.service';
import { FileUploadService, UPLOAD_LIMITS, resolveFileUrl } from '../../../../shared/services/file-upload.service';

@Component({
    selector: 'app-product-media-manager',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, DialogModule, InputTextModule, TextareaModule, TagModule, ToastModule, TooltipModule, SelectModule],
    providers: [MessageService],
    templateUrl: './product-media-manager.component.html',
    styleUrls: ['./product-media-manager.component.css']
})
export class ProductMediaManagerComponent implements OnInit {
    @Input() partId!: string;

    private readonly sanitizer = inject(DomSanitizer);
    private readonly messageService = inject(MessageService);
    private readonly mediaService = inject(ProductMediaService);
    private readonly fileUploadService = inject(FileUploadService);

    media: ProductMedia[] = [];
    loading = false;
    saving = false;

    showDialog = false;
    editingItem: ProductMedia | null = null; // null = add mode

    // Form state
    formSource: 'upload' | 'url' = 'upload';
    formUrl = '';
    formType: 'image' | 'video' = 'image';
    formAlt = '';
    formError = '';
    selectedFile: File | null = null;
    selectedFilePreview: string | null = null; // object URL for local image preview

    readonly typeOptions = [
        { label: 'Image', value: 'image' },
        { label: 'Video (URL or YouTube)', value: 'video' }
    ];

    ngOnInit(): void {
        this.load();
    }

    private load(): void {
        if (!this.partId) return;
        this.loading = true;
        this.mediaService.getByPart(this.partId).subscribe({
            next: (items) => {
                this.media = items;
                this.loading = false;
            },
            error: (err) => {
                this.loading = false;
                this.toastError(err, 'Failed to load media');
            }
        });
    }

    // ── Dialog ────────────────────────────────────────────────────────────────

    openAdd(): void {
        this.editingItem = null;
        this.formSource = 'upload';
        this.formUrl = '';
        this.formType = 'image';
        this.formAlt = '';
        this.formError = '';
        this.clearSelectedFile();
        this.showDialog = true;
    }

    openEdit(item: ProductMedia): void {
        this.editingItem = item;
        this.formSource = 'url'; // editing changes metadata/URL; re-uploading = delete + add
        this.formUrl = item.url;
        this.formType = item.mediaType;
        this.formAlt = item.altText ?? '';
        this.formError = '';
        this.clearSelectedFile();
        this.showDialog = true;
    }

    closeDialog(): void {
        this.showDialog = false;
        this.editingItem = null;
        this.clearSelectedFile();
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0] ?? null;
        input.value = ''; // allow re-selecting the same file
        if (!file) return;

        const limit = this.formType === 'video' ? UPLOAD_LIMITS.video : UPLOAD_LIMITS.image;
        if (file.size > limit.maxBytes) {
            this.formError = `File exceeds the ${limit.label} limit for ${this.formType}s.`;
            return;
        }

        this.formError = '';
        this.clearSelectedFile();
        this.selectedFile = file;
        if (this.formType === 'image') {
            this.selectedFilePreview = URL.createObjectURL(file);
        }
    }

    clearSelectedFile(): void {
        if (this.selectedFilePreview) {
            URL.revokeObjectURL(this.selectedFilePreview);
        }
        this.selectedFile = null;
        this.selectedFilePreview = null;
    }

    get uploadAccept(): string {
        return this.formType === 'video' ? UPLOAD_LIMITS.video.accept : UPLOAD_LIMITS.image.accept;
    }

    get uploadLimitLabel(): string {
        return this.formType === 'video' ? UPLOAD_LIMITS.video.label : UPLOAD_LIMITS.image.label;
    }

    save(): void {
        if (this.editingItem) {
            this.saveEdit();
            return;
        }

        if (this.formSource === 'upload') {
            if (!this.selectedFile) {
                this.formError = 'Choose a file to upload';
                return;
            }
            this.saving = true;
            // Two steps: upload the blob, then attach the returned URL as a media row.
            this.fileUploadService.upload(this.selectedFile, 'PRODUCT', this.partId).subscribe({
                next: (stored) => this.addMediaRow(stored.url, stored.fileName),
                error: (err) => {
                    this.saving = false;
                    this.toastError(err, 'Upload failed');
                }
            });
        } else {
            this.formUrl = this.formUrl.trim();
            if (!this.formUrl) {
                this.formError = 'URL is required';
                return;
            }
            this.saving = true;
            this.addMediaRow(this.formUrl, null);
        }
    }

    private addMediaRow(url: string, fileName: string | null): void {
        this.mediaService
            .add(this.partId, {
                url,
                mediaType: this.formType,
                altText: this.formAlt.trim() || null,
                fileName
            })
            .subscribe({
                next: () => {
                    this.saving = false;
                    this.load();
                    this.closeDialog();
                    this.messageService.add({ severity: 'success', summary: 'Added', detail: 'Media item added' });
                },
                error: (err) => {
                    this.saving = false;
                    this.toastError(err, 'Failed to add media');
                }
            });
    }

    private saveEdit(): void {
        this.formUrl = this.formUrl.trim();
        if (!this.formUrl) {
            this.formError = 'URL is required';
            return;
        }
        const item = this.editingItem!;
        this.saving = true;
        this.mediaService
            .update(this.partId, item.id, {
                url: this.formUrl,
                mediaType: this.formType,
                altText: this.formAlt.trim() || null,
                fileName: item.fileName,
                isPrimary: item.isPrimary,
                variantId: item.variantId
            })
            .subscribe({
                next: () => {
                    this.saving = false;
                    this.load();
                    this.closeDialog();
                    this.messageService.add({ severity: 'success', summary: 'Updated', detail: 'Media item updated' });
                },
                error: (err) => {
                    this.saving = false;
                    this.toastError(err, 'Failed to update media');
                }
            });
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    setPrimary(item: ProductMedia): void {
        this.mediaService.setPrimary(this.partId, item.id).subscribe({
            next: () => {
                this.media = this.media.map((m) => ({ ...m, isPrimary: m.id === item.id }));
                this.messageService.add({ severity: 'success', summary: 'Primary set', detail: `"${this.labelFor(item)}" is now the primary image` });
            },
            error: (err) => this.toastError(err, 'Failed to set primary')
        });
    }

    remove(item: ProductMedia): void {
        this.mediaService.delete(this.partId, item.id).subscribe({
            next: () => {
                this.load();
                this.messageService.add({ severity: 'info', summary: 'Removed', detail: 'Media item removed' });
            },
            error: (err) => this.toastError(err, 'Failed to remove media')
        });
    }

    moveUp(index: number): void {
        if (index === 0) return;
        this.swapAndPersist(index, index - 1);
    }

    moveDown(index: number): void {
        if (index === this.media.length - 1) return;
        this.swapAndPersist(index, index + 1);
    }

    private swapAndPersist(from: number, to: number): void {
        const previous = this.media;
        const arr = [...this.media];
        [arr[from], arr[to]] = [arr[to], arr[from]];
        this.media = arr.map((m, i) => ({ ...m, sortOrder: i })); // optimistic
        this.mediaService
            .reorder(
                this.partId,
                arr.map((m) => m.id)
            )
            .subscribe({
                next: (items) => (this.media = items),
                error: (err) => {
                    this.media = previous; // roll back on failure
                    this.toastError(err, 'Failed to reorder media');
                }
            });
    }

    // ── Display helpers ───────────────────────────────────────────────────────

    /** Uploaded URLs are stored relative — resolve against the API origin for <img>/<video>. */
    displayUrl(url: string): string {
        return resolveFileUrl(url);
    }

    isYouTube(url: string): boolean {
        return url.includes('youtube.com') || url.includes('youtu.be');
    }

    youTubeThumbnail(url: string): string {
        const match = url.match(/(?:v=|youtu\.be\/)([A-Za-z0-9_-]{11})/);
        const id = match?.[1] ?? '';
        return `https://img.youtube.com/vi/${id}/mqdefault.jpg`;
    }

    youTubeEmbedUrl(url: string): SafeResourceUrl {
        const match = url.match(/(?:v=|youtu\.be\/)([A-Za-z0-9_-]{11})/);
        const id = match?.[1] ?? '';
        return this.sanitizer.bypassSecurityTrustResourceUrl(`https://www.youtube.com/embed/${id}?rel=0`);
    }

    private labelFor(item: ProductMedia): string {
        return item.altText || item.fileName || item.url.split('/').pop() || 'item';
    }

    get dialogHeader(): string {
        return this.editingItem ? 'Edit Media Item' : 'Add Media';
    }

    private toastError(err: any, fallback: string): void {
        this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: err?.error?.message || fallback
        });
    }
}
