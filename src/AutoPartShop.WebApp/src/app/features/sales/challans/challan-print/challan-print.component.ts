import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ChallanService, ChallanResponse } from '../../services/challan.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { AppSettingsService, ShopProfile } from '@/shared/services/app-settings.service';

@Component({
  selector: 'app-challan-print',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './challan-print.component.html',
  styleUrls: ['./challan-print.component.css']
})
export class ChallanPrintComponent implements OnInit {
  private readonly route       = inject(ActivatedRoute);
  private readonly svc         = inject(ChallanService);
  private readonly fxSvc       = inject(CurrencyService);
  private readonly appSettings = inject(AppSettingsService);

  challan  = signal<ChallanResponse | null>(null);
  loading  = signal(true);
  error    = signal<string | null>(null);
  shop     = signal<ShopProfile | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;

    // Load shop profile (anonymous endpoint — same auth level as this print page)
    this.appSettings.getShopProfile().subscribe({ next: p => this.shop.set(p), error: () => {} });

    this.svc.getById(id).subscribe({
      next: c => { this.challan.set(c); this.loading.set(false); },
      error: () => { this.error.set('Challan not found'); this.loading.set(false); }
    });
  }

  print(): void { window.print(); }

  formatDate(iso: string | undefined | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatDateTime(iso: string | undefined | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  get statusLabel(): string {
    const m: Record<string, string> = { DRAFT: 'Draft', ISSUED: 'Issued', DELIVERED: 'Delivered' };
    return m[this.challan()?.status ?? ''] ?? (this.challan()?.status ?? '');
  }

  get isDelivered(): boolean { return this.challan()?.status === 'DELIVERED'; }
}
