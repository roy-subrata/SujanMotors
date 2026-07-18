import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-quotations',
  imports: [RouterOutlet],
  template: '<router-outlet />',
  standalone: true
})
export class QuotationsComponent {}
