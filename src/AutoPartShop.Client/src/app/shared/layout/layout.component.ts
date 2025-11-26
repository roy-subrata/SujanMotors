import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { TopbarComponent } from '../components/topbar/topbar.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, TopbarComponent],
  template: `
    <div class="app-container">
      <!-- Top Navigation Bar -->
      <app-topbar></app-topbar>

      <!-- Main Content Area -->
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-container {
      display: flex;
      flex-direction: column;
      height: 100vh;
      background: #f9fafb;
    }

    :host ::ng-deep .dark .app-container {
      background: #111827;
    }

    .main-content {
      flex: 1;
      overflow-y: auto;
      overflow-x: hidden;
      padding: 2rem;
    }

    @media (max-width: 1024px) {
      .main-content {
        padding: 1.5rem;
      }
    }

    @media (max-width: 640px) {
      .main-content {
        padding: 1rem;
      }
    }
  `]
})
export class LayoutComponent {}
