import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-technicians',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './technicians.component.html',
})
export class TechniciansComponent {}
