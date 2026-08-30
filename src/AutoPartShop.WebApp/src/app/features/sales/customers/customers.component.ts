import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-customers',
  imports: [RouterOutlet],
  templateUrl: './customers.component.html',
  standalone: true
})
export class CustomersComponent {}
