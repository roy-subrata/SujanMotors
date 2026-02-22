import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CartService } from '../services/cart.service';
import { Order } from '../models/cart.model';

@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './order-confirmation.component.html',
  styleUrls: ['./order-confirmation.component.css'],
})
export class OrderConfirmationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly cartService = inject(CartService);

  order?: Order;

  ngOnInit(): void {
    const orderId = this.route.snapshot.paramMap.get('orderId');
    if (orderId) {
      this.order = this.cartService.getOrder(orderId);
    }
  }
}
