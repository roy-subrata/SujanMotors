import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { OrderService, EcommerceCheckoutResponse } from '../services/order.service';

@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './order-confirmation.component.html',
  styleUrls: ['./order-confirmation.component.css'],
})
export class OrderConfirmationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly orderService = inject(OrderService);

  order?: EcommerceCheckoutResponse;
  soNumber?: string;
  loading = false;
  notFound = false;

  ngOnInit(): void {
    // Primary path: router state from checkout (works on first navigation)
    const nav = this.router.getCurrentNavigation();
    const state = nav?.extras?.state as { order?: EcommerceCheckoutResponse } | undefined;

    if (state?.order) {
      this.order = state.order;
      this.soNumber = state.order.soNumber;
      return;
    }

    // Fallback: fetch order from API by SO number in URL (page refresh / direct link)
    const soFromUrl = this.route.snapshot.paramMap.get('orderId');
    if (soFromUrl) {
      this.soNumber = soFromUrl;
      this.loading = true;
      this.orderService.getOrderBySoNumber(soFromUrl).subscribe({
        next: data => {
          this.order = data ?? undefined;
          this.notFound = !data;
          this.loading = false;
        },
        error: () => {
          this.notFound = true;
          this.loading = false;
        },
      });
    } else {
      this.notFound = true;
    }
  }
}
