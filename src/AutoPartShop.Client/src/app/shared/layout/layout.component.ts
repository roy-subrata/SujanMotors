import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { TopbarComponent } from '../components/topbar/topbar.component';
import { DrawerComponent } from '../components/drawer/drawer.component';
import { DrawerService } from '../services/drawer.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, TopbarComponent, DrawerComponent],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.css'],
})
export class LayoutComponent {
  private drawerService = inject(DrawerService);
  isDrawerOpen = this.drawerService.isOpen;
}
