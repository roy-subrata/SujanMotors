import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

/**
 * Full-page "access denied" screen shown when a guard (role/permission) or the
 * auth interceptor (403) bounces the user off a route they may not view.
 * Standalone like /login so it renders without the app shell.
 */
@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  template: `
    <div class="min-h-screen flex flex-col items-center justify-center p-6 text-center">
      <i class="pi pi-lock text-6xl text-primary mb-4" aria-hidden="true"></i>
      <h1 class="text-2xl font-semibold mb-2">{{ 'unauthorized.title' | translate }}</h1>
      <p class="text-muted-color max-w-md mb-6">{{ 'unauthorized.message' | translate }}</p>
      <a
        routerLink="/"
        class="px-4 py-2 rounded-md border border-surface-border hover:bg-surface-hover no-underline"
      >{{ 'unauthorized.goHome' | translate }}</a>
    </div>
  `,
})
export class UnauthorizedComponent {}
