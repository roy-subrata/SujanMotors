import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
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

@Component({
  selector: 'app-pending-deliveries',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ToastModule, TooltipModule, ConfirmDialogModule, DialogModule, DividerModule, InputTextModule, TextareaModule, PageContainerComponent, PageHeaderComponent],
  providers: [MessageService, ConfirmationService],
  templateUrl: './pending-deliveries.component.html',
  styleUrls: ['./pending-deliveries.component.css']
})
export class PendingDeliveriesComponent implements OnInit {
  private readonly soSvc      = inject(SalesOrderService);
  private readonly challanSvc = inject(ChallanService);
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
      error: () => { this.toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load' }); this.loading.set(false); }
    });
  }

  view(id: string): void {
    this.router.navigate(['/sales/sales-orders/view'], { queryParams: { id, mode: 'view' } });
  }

  deliverDirect(order: SalesOrderResponse): void {
    this.confirm.confirm({
      message: `Mark ${order.soNumber} as Delivered now? Invoice will be issued automatically.`,
      header: 'Direct Delivery',
      icon: 'pi pi-check-circle',
      acceptButtonStyleClass: 'p-button-success',
      accept: () => {
        this.soSvc.deliverDirect(order.id).subscribe({
          next: () => { this.toast.add({ severity: 'success', summary: 'Delivered', detail: `${order.soNumber} marked as Delivered` }); this.load(); },
          error: err => this.toast.add({ severity: 'error', summary: 'Error', detail: err?.error?.detail || 'Failed' })
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
        this.toast.add({ severity: 'success', summary: 'Challan Created', detail: challan.challanNumber });
        window.open(`/sales/challans/${challan.id}/print`, '_blank');
        this.load();
      },
      error: err => this.toast.add({ severity: 'error', summary: 'Error', detail: err?.error?.detail || 'Failed' })
    });
  }

  statusLabel(s: string): string {
    return s === 'READY_FOR_DELIVERY' ? 'Ready for Delivery' : 'Confirmed';
  }

  formatCurrency(v: number): string {
    return this.fxSvc.formatCurrency(v, this.fxSvc.selectedCurrency());
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
