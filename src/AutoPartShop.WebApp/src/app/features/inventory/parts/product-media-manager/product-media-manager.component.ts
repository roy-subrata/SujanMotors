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

export interface MediaItem {
  id: string;
  url: string;
  mediaType: 'image' | 'video';
  altText: string;
  sortOrder: number;
  isPrimary: boolean;
}

// TODO: replace with real service — load via this.mediaService.getByPart(partId) in ngOnInit
const MOCK_MEDIA: MediaItem[] = [
  {
    id: 'demo-1',
    url: 'https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?w=480&q=80',
    mediaType: 'image',
    altText: 'Product front view',
    sortOrder: 0,
    isPrimary: true,
  },
  {
    id: 'demo-2',
    url: 'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=480&q=80',
    mediaType: 'image',
    altText: 'Product detail close-up',
    sortOrder: 1,
    isPrimary: false,
  },
  {
    id: 'demo-3',
    url: 'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=480&q=80',
    mediaType: 'image',
    altText: 'Side angle view',
    sortOrder: 2,
    isPrimary: false,
  },
  {
    id: 'demo-4',
    url: 'https://www.youtube.com/watch?v=ysz5S6PUM-U',
    mediaType: 'video',
    altText: 'Product installation guide',
    sortOrder: 3,
    isPrimary: false,
  },
];

@Component({
  selector: 'app-product-media-manager',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    ButtonModule, DialogModule, InputTextModule, TextareaModule,
    TagModule, ToastModule, TooltipModule, SelectModule,
  ],
  providers: [MessageService],
  templateUrl: './product-media-manager.component.html',
  styleUrls: ['./product-media-manager.component.css'],
})
export class ProductMediaManagerComponent implements OnInit {
  @Input() partId!: string;

  private readonly sanitizer = inject(DomSanitizer);
  private readonly messageService = inject(MessageService);

  media: MediaItem[] = [];

  showDialog = false;
  editingItem: MediaItem | null = null;  // null = add mode

  // Form state
  formUrl = '';
  formType: 'image' | 'video' = 'image';
  formAlt = '';
  formUrlError = '';

  readonly typeOptions = [
    { label: 'Image', value: 'image' },
    { label: 'Video (URL or YouTube)', value: 'video' },
  ];

  ngOnInit(): void {
    // TODO: load from service — this.mediaService.getByPart(this.partId)
    this.media = [...MOCK_MEDIA];
  }

  // ── Dialog ────────────────────────────────────────────────────────────────

  openAdd(): void {
    this.editingItem = null;
    this.formUrl = '';
    this.formType = 'image';
    this.formAlt = '';
    this.formUrlError = '';
    this.showDialog = true;
  }

  openEdit(item: MediaItem): void {
    this.editingItem = item;
    this.formUrl = item.url;
    this.formType = item.mediaType;
    this.formAlt = item.altText;
    this.formUrlError = '';
    this.showDialog = true;
  }

  closeDialog(): void {
    this.showDialog = false;
    this.editingItem = null;
  }

  save(): void {
    this.formUrl = this.formUrl.trim();
    if (!this.formUrl) {
      this.formUrlError = 'URL is required';
      return;
    }
    this.formUrlError = '';

    if (this.editingItem) {
      // Update existing
      this.editingItem.url = this.formUrl;
      this.editingItem.mediaType = this.formType;
      this.editingItem.altText = this.formAlt;
      // TODO: call service to persist
      this.messageService.add({ severity: 'success', summary: 'Updated', detail: 'Media item updated' });
    } else {
      // Add new
      const newItem: MediaItem = {
        id: crypto.randomUUID(),
        url: this.formUrl,
        mediaType: this.formType,
        altText: this.formAlt,
        sortOrder: this.media.length,
        isPrimary: this.media.length === 0,  // first item auto-primary
      };
      this.media = [...this.media, newItem];
      // TODO: call service to persist
      this.messageService.add({ severity: 'success', summary: 'Added', detail: 'Media item added' });
    }

    this.closeDialog();
  }

  // ── Actions ───────────────────────────────────────────────────────────────

  setPrimary(item: MediaItem): void {
    this.media = this.media.map(m => ({ ...m, isPrimary: m.id === item.id }));
    // TODO: call service to persist
    this.messageService.add({ severity: 'success', summary: 'Primary set', detail: `"${this.labelFor(item)}" is now the primary image` });
  }

  remove(item: MediaItem): void {
    const wasPrimary = item.isPrimary;
    this.media = this.media.filter(m => m.id !== item.id).map((m, i) => ({ ...m, sortOrder: i }));
    // Auto-assign primary to first item if the primary was removed
    if (wasPrimary && this.media.length > 0) {
      this.media[0] = { ...this.media[0], isPrimary: true };
    }
    // TODO: call service to persist
    this.messageService.add({ severity: 'info', summary: 'Removed', detail: 'Media item removed' });
  }

  moveUp(index: number): void {
    if (index === 0) return;
    const arr = [...this.media];
    [arr[index - 1], arr[index]] = [arr[index], arr[index - 1]];
    this.media = arr.map((m, i) => ({ ...m, sortOrder: i }));
    // TODO: call service to persist order
  }

  moveDown(index: number): void {
    if (index === this.media.length - 1) return;
    const arr = [...this.media];
    [arr[index], arr[index + 1]] = [arr[index + 1], arr[index]];
    this.media = arr.map((m, i) => ({ ...m, sortOrder: i }));
    // TODO: call service to persist order
  }

  // ── Display helpers ───────────────────────────────────────────────────────

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
    return this.sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.youtube.com/embed/${id}?rel=0`
    );
  }

  private labelFor(item: MediaItem): string {
    return item.altText || item.url.split('/').pop() || 'item';
  }

  get dialogHeader(): string {
    return this.editingItem ? 'Edit Media Item' : 'Add Media';
  }
}
