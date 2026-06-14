import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppBrandingService } from '../../../../shared/services/app-branding.service';

@Component({
  selector: 'app-store-footer',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './store-footer.component.html',
  styleUrls: ['./store-footer.component.css'],
})
export class StoreFooterComponent {
  readonly branding = inject(AppBrandingService);
  currentYear = new Date().getFullYear();

  /** Up-to-2-letter monogram derived from the configured application name. */
  get brandInitials(): string {
    const words = this.branding.appName().trim().split(/\s+/).filter(Boolean);
    if (words.length === 0) return '?';
    if (words.length === 1) return words[0].substring(0, 2).toUpperCase();
    return (words[0][0] + words[words.length - 1][0]).toUpperCase();
  }
}
