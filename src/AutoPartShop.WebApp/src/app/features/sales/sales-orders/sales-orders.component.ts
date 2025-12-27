import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-sales-orders',
  imports: [RouterOutlet],
  template: '<router-outlet />',
  standalone: true
})
export class SalesOrdersComponent {}
