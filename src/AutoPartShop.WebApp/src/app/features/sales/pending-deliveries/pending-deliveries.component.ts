import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { DividerModule } from 'primeng/divider';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { MessageService, ConfirmationService } from 'primeng/api';
import { SalesOrderService, SalesOrderResponse } from '../services/sales-order.service';
import { ChallanService } from '../services/challan.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
  selector: 'app-pending-deliveries',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ButtonModule, ToastModule, TooltipModule, ConfirmDialogModule, DialogModule, DividerModule, InputTextModule, TextareaModule, PageContainerComponent, PageHeaderComponent,
        TranslatePipe],
  providers: [MessageService, ConfirmationService],
  templateUrl: './pending-deliveries.component.html',
  styleUrls: ['./pending-deliveries.component.css']
})
export class PendingDeliveriesComponent implements OnInit {
  private readonly soSvc      = inject(SalesOrderService);
  private readonly challanSvc = inject(ChallanService);
  private readonly i18n = inject(I18nService);
  private readonly toast      = inject(MessageService);
  private readonly confirm    = inject(ConfirmationService);
  private readonly router     = inject(Router);
  private readonly fxSvc      = inject(CurrencyService);

  orders  = signal<SalesOrderResponse[]>([]);
  loading = signal(false);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.soSvc.getPendingDeliveries().subscribe({
      next: r => { this.orders.set(r.data); this.loading.set(false); },
      error: () => { this.toast.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('pendingDeliveries.messages.loadFailed') }); this.loading.set(false); }
    });
  }

  view(id: string): void {
    this.router.navigate(['/sales/sales-orders/view'], { queryParams: { id, mode: 'view' } });
  }

  deliverDirect(order: SalesOrderResponse): void {
    this.confirm.confirm({
      message: this.i18n.t('pendingDeliveries.messages.deliverConfirm', { number: order.soNumber }),
      header: this.i18n.t('pendingDeliveries.messages.deliverHeader'),
      icon: 'pi pi-check-circle',
      acceptButtonStyleClass: 'p-button-success',
      accept: () => {
        this.soSvc.deliverDirect(order.id).subscribe({
          next: () => { this.toast.add({ severity: 'success', summary: this.i18n.t('pendingDeliveries.messages.deliveredTitle'), detail: this.i18n.t('pendingDeliveries.messages.deliveredDetail', { number: order.soNumber }) }); this.load(); },
          error: err => this.toast.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.detail || this.i18n.t('pendingDeliveries.messages.failed') })
        });
      }
    });
  }

  // ── Challan dialog ──────────────────────────────────────────────────
  showChallanDialog = false;
  selectedOrderForChallan: SalesOrderResponse | null = null;
  challanForm = {
    deliveryAddress: '', receiverName: '', receiverPhone: '',
    transportCompany: '', vehicleNumber: '', driverName: '', driverPhone: '', notes: ''
  };

  openChallanDialog(order: SalesOrderResponse): void {
    this.selectedOrderForChallan = order;
    this.challanForm = {
      deliveryAddress: order.customerCity    || '',
      receiverName:    order.customerName    || '',
      receiverPhone:   order.customerPhone   || '',
      transportCompany: '', vehicleNumber: '', driverName: '', driverPhone: '', notes: ''
    };
    this.showChallanDialog = true;
  }

  generateChallan(): void {
    if (!this.selectedOrderForChallan) return;
    this.showChallanDialog = false;
    this.challanSvc.generate(this.selectedOrderForChallan.id, { ...this.challanForm }).subscribe({
      next: challan => {
        this.toast.add({ severity: 'success', summary: this.i18n.t('pendingDeliveries.messages.challanCreatedTitle'), detail: challan.challanNumber });
        window.open(`/sales/challans/${challan.id}/print`, '_blank');
        this.load();
      },
      error: err => this.toast.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.detail || this.i18n.t('pendingDeliveries.messages.failed') })
    });
  }

  statusLabel(s: string): string {
    return this.i18n.t(s === 'READY_FOR_DELIVERY'
      ? 'pendingDeliveries.statusReadyForDelivery'
      : 'pendingDeliveries.statusConfirmed');
  }

  formatCurrency(v: number): string {
    return this.fxSvc.formatCurrency(v, this.fxSvc.selectedCurrency());
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
