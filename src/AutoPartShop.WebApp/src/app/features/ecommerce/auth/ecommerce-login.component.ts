import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { CustomerAuthService } from '../services/customer-auth.service';
import { extractApiError } from '../../../shared/utils/api-error.util';

@Component({
  selector: 'app-ecommerce-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="auth-logo">
          <span class="logo-mark">SM</span>
          <div class="logo-text">
            <span class="logo-title">Sujan Motors</span>
            <span class="logo-sub">Customer Login</span>
          </div>
        </div>

        <div class="error-banner" *ngIf="error()">
          <i class="pi pi-exclamation-triangle"></i> {{ error() }}
        </div>

        <div class="form-field">
          <label>Phone Number or Email</label>
          <input
            type="text"
            [(ngModel)]="identifier"
            placeholder="01712345678 or your@email.com"
            (keyup.enter)="login()"
            [class.error]="errors['identifier']"
          />
          <span class="field-error" *ngIf="errors['identifier']">{{ errors['identifier'] }}</span>
        </div>

        <div class="form-field">
          <label>Password</label>
          <input
            type="password"
            [(ngModel)]="password"
            placeholder="••••••••"
            (keyup.enter)="login()"
            [class.error]="errors['password']"
          />
          <span class="field-error" *ngIf="errors['password']">{{ errors['password'] }}</span>
        </div>

        <button class="btn-primary" (click)="login()" [disabled]="loading()">
          <i class="pi" [ngClass]="loading() ? 'pi-spin pi-spinner' : 'pi-sign-in'"></i>
          {{ loading() ? 'Signing in...' : 'Sign In' }}
        </button>

        <div class="auth-footer">
          Don't have an account?
          <a routerLink="/shop/register" [queryParams]="returnUrl ? { returnUrl } : {}">Create one</a>
        </div>

        <div class="auth-footer">
          <a routerLink="/shop"><i class="pi pi-arrow-left"></i> Back to Shop</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: #f5f5f5; padding: 16px; }
    .auth-card { background: #fff; border-radius: 12px; padding: 40px 36px; width: 100%; max-width: 420px; box-shadow: 0 4px 24px rgba(0,0,0,.08); }
    .auth-logo { display: flex; align-items: center; gap: 12px; margin-bottom: 32px; }
    .logo-mark { width: 44px; height: 44px; background: #e53e3e; color: #fff; font-weight: 800; font-size: 18px; border-radius: 10px; display: flex; align-items: center; justify-content: center; }
    .logo-title { font-weight: 700; font-size: 18px; display: block; }
    .logo-sub { font-size: 12px; color: #888; display: block; }
    .form-field { margin-bottom: 18px; }
    .form-field label { display: block; font-size: 13px; font-weight: 600; color: #444; margin-bottom: 6px; }
    .form-field input { width: 100%; padding: 10px 14px; border: 1.5px solid #ddd; border-radius: 8px; font-size: 14px; box-sizing: border-box; transition: border-color .2s; }
    .form-field input:focus { outline: none; border-color: #e53e3e; }
    .form-field input.error { border-color: #e53e3e; }
    .field-error { font-size: 12px; color: #e53e3e; margin-top: 4px; display: block; }
    .btn-primary { width: 100%; padding: 12px; background: #e53e3e; color: #fff; border: none; border-radius: 8px; font-size: 15px; font-weight: 600; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 8px; margin-top: 8px; transition: background .2s; }
    .btn-primary:hover:not(:disabled) { background: #c53030; }
    .btn-primary:disabled { opacity: .6; cursor: not-allowed; }
    .error-banner { background: #fff5f5; border: 1px solid #fc8181; color: #c53030; padding: 10px 14px; border-radius: 8px; font-size: 13px; margin-bottom: 18px; display: flex; align-items: center; gap: 8px; }
    .auth-footer { text-align: center; font-size: 13px; color: #666; margin-top: 18px; }
    .auth-footer a { color: #e53e3e; text-decoration: none; font-weight: 600; }
  `]
})
export class EcommerceLoginComponent implements OnInit, OnDestroy {
  private readonly authService = inject(CustomerAuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly doc = inject(DOCUMENT);

  ngOnInit(): void {
    this.doc.body.classList.add('storefront-scroll');
    this.doc.documentElement.classList.add('storefront-scroll');
  }

  ngOnDestroy(): void {
    this.doc.body.classList.remove('storefront-scroll');
    this.doc.documentElement.classList.remove('storefront-scroll');
  }

  identifier = '';
  password = '';
  loading = signal(false);
  error = signal<string | null>(null);
  errors: Record<string, string> = {};

  returnUrl: string = this.route.snapshot.queryParams['returnUrl'] ?? '/shop/checkout';

  login(): void {
    this.errors = {};
    if (!this.identifier.trim()) { this.errors['identifier'] = 'Email or phone is required'; return; }
    if (!this.password.trim()) { this.errors['password'] = 'Password is required'; return; }
    if (this.loading()) return;

    this.loading.set(true);
    this.error.set(null);

    this.authService.login(this.identifier.trim(), this.password).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl(this.returnUrl);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(extractApiError(err, 'Invalid credentials. Please try again.'));
      }
    });
  }
}
