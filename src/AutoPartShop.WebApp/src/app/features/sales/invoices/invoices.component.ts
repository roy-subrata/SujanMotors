import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-invoices',
  imports: [RouterOutlet],
  templateUrl: './invoices.component.html',
  standalone: true
})
export class InvoicesComponent {}
