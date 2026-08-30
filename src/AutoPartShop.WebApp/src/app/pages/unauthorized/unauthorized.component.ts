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
  templateUrl: './unauthorized.component.html',
})
export class UnauthorizedComponent {}
