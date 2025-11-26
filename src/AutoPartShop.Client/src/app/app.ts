import { Component } from '@angular/core';
import { LayoutComponent } from './shared';


@Component({
  selector: 'app-root',
  standalone: true,
  imports: [LayoutComponent],
  template: '<app-layout></app-layout>',
  styleUrl: './app.css'
})
export class App {}
