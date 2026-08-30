import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-sales-orders',
  imports: [RouterOutlet],
  templateUrl: './sales-orders.component.html',
  standalone: true
})
export class SalesOrdersComponent {}
