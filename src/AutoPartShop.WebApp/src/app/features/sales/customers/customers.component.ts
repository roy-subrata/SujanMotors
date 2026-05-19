import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-customers',
  imports: [RouterOutlet],
  template: '<router-outlet />',
  standalone: true
})
export class CustomersComponent {}
