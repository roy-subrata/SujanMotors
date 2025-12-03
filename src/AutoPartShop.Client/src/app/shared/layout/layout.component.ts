import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { TopbarComponent } from '../components/topbar/topbar.component';
import { SidebarComponent } from '../components/sidebar/sidebar.component';
import { DrawerService } from '../services/drawer.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, TopbarComponent, SidebarComponent],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.css'],
})
export class LayoutComponent {
  private drawerService = inject(DrawerService);
  isCollapsed = this.drawerService.isCollapsed;

  getMainContentMargin() {
    return this.isCollapsed() ? '5rem' : '18rem'; // 80px when collapsed, 288px when expanded
  }
}
