import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { CustomerAuthService } from '../services/customer-auth.service';
import { AppBrandingService } from '../../../shared/services/app-branding.service';
import { extractApiError } from '../../../shared/utils/api-error.util';

@Component({
  selector: 'app-ecommerce-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="auth-logo">
          <span class="logo-mark">{{ brandInitials }}</span>
          <div class="logo-text">
            <span class="logo-title">{{ branding.appName() }}</span>
            <span class="logo-sub">Create Account</span>
          </div>
        </div>

        <div *ngIf="success()" class="success-banner">
          <i class="pi pi-check-circle"></i> Account created! You can now sign in.
          <br/><a routerLink="/shop/login">Go to Login</a>
        </div>

        <ng-container *ngIf="!success()">
          <div class="error-banner" *ngIf="error()">
            <i class="pi pi-exclamation-triangle"></i> {{ error() }}
          </div>

          <div class="form-row">
            <div class="form-field">
              <label>First Name *</label>
              <input type="text" [(ngModel)]="form.firstName" placeholder="John" [class.error]="errors['firstName']" />
              <span class="field-error" *ngIf="errors['firstName']">{{ errors['firstName'] }}</span>
            </div>
            <div class="form-field">
              <label>Last Name</label>
              <input type="text" [(ngModel)]="form.lastName" placeholder="Doe" />
            </div>
          </div>

          <div class="form-field">
            <label>Phone Number *</label>
            <input type="tel" [(ngModel)]="form.phone" placeholder="01712345678" [class.error]="errors['phone']" />
            <span class="field-error" *ngIf="errors['phone']">{{ errors['phone'] }}</span>
          </div>

          <div class="form-field">
            <label>Email <span class="optional-label">(optional)</span></label>
            <input type="email" [(ngModel)]="form.email" placeholder="john@example.com" [class.error]="errors['email']" />
            <span class="field-error" *ngIf="errors['email']">{{ errors['email'] }}</span>
          </div>

          <div class="form-field">
            <label>Password *</label>
            <input type="password" [(ngModel)]="form.password" placeholder="Min 8 chars, upper, lower, digit, symbol" [class.error]="errors['password']" />
            <span class="field-error" *ngIf="errors['password']">{{ errors['password'] }}</span>
          </div>

          <button class="btn-primary" (click)="register()" [disabled]="loading()">
            <i class="pi" [ngClass]="loading() ? 'pi-spin pi-spinner' : 'pi-user-plus'"></i>
            {{ loading() ? 'Creating Account...' : 'Create Account' }}
          </button>

          <div class="auth-footer">
            Already have an account?
            <a routerLink="/shop/login" [queryParams]="returnUrl ? { returnUrl } : {}">Sign in</a>
          </div>
        </ng-container>

        <div class="auth-footer">
          <a routerLink="/shop"><i class="pi pi-arrow-left"></i> Back to Shop</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: #f5f5f5; padding: 16px; }
    .auth-card { background: #fff; border-radius: 12px; padding: 40px 36px; width: 100%; max-width: 460px; box-shadow: 0 4px 24px rgba(0,0,0,.08); }
    .auth-logo { display: flex; align-items: center; gap: 12px; margin-bottom: 28px; }
    .logo-mark { width: 44px; height: 44px; background: #e53e3e; color: #fff; font-weight: 800; font-size: 18px; border-radius: 10px; display: flex; align-items: center; justify-content: center; }
    .logo-title { font-weight: 700; font-size: 18px; display: block; }
    .logo-sub { font-size: 12px; color: #888; display: block; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .form-field { margin-bottom: 16px; }
    .form-field label { display: block; font-size: 13px; font-weight: 600; color: #444; margin-bottom: 6px; }
    .form-field input { width: 100%; padding: 10px 14px; border: 1.5px solid #ddd; border-radius: 8px; font-size: 14px; box-sizing: border-box; transition: border-color .2s; }
    .form-field input:focus { outline: none; border-color: #e53e3e; }
    .form-field input.error { border-color: #e53e3e; }
    .field-error { font-size: 12px; color: #e53e3e; margin-top: 4px; display: block; }
    .btn-primary { width: 100%; padding: 12px; background: #e53e3e; color: #fff; border: none; border-radius: 8px; font-size: 15px; font-weight: 600; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 8px; margin-top: 4px; transition: background .2s; }
    .btn-primary:hover:not(:disabled) { background: #c53030; }
    .btn-primary:disabled { opacity: .6; cursor: not-allowed; }
    .error-banner { background: #fff5f5; border: 1px solid #fc8181; color: #c53030; padding: 10px 14px; border-radius: 8px; font-size: 13px; margin-bottom: 16px; display: flex; align-items: center; gap: 8px; }
    .success-banner { background: #f0fff4; border: 1px solid #68d391; color: #276749; padding: 16px; border-radius: 8px; font-size: 14px; margin-bottom: 16px; text-align: center; }
    .success-banner a { color: #276749; font-weight: 700; }
    .auth-footer { text-align: center; font-size: 13px; color: #666; margin-top: 16px; }
    .auth-footer a { color: #e53e3e; text-decoration: none; font-weight: 600; }
    .optional-label { font-weight: 400; color: #aaa; font-size: 11px; }
  `]
})
export class EcommerceRegisterComponent implements OnInit, OnDestroy {
  private readonly authService = inject(CustomerAuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly doc = inject(DOCUMENT);
  protected readonly branding = inject(AppBrandingService);

  /** Up-to-2-letter monogram derived from the configured application name. */
  get brandInitials(): string {
    const words = this.branding.appName().trim().split(/\s+/).filter(Boolean);
    if (words.length === 0) return '?';
    if (words.length === 1) return words[0].substring(0, 2).toUpperCase();
    return (words[0][0] + words[words.length - 1][0]).toUpperCase();
  }

  ngOnInit(): void {
    this.doc.body.classList.add('storefront-scroll');
    this.doc.documentElement.classList.add('storefront-scroll');
  }

  ngOnDestroy(): void {
    this.doc.body.classList.remove('storefront-scroll');
    this.doc.documentElement.classList.remove('storefront-scroll');
  }

  form = { firstName: '', lastName: '', phone: '', email: '', password: '' };
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal(false);
  errors: Record<string, string> = {};

  returnUrl: string = this.route.snapshot.queryParams['returnUrl'] ?? '/shop/checkout';

  private readonly EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  validate(): boolean {
    this.errors = {};
    if (!this.form.firstName.trim()) this.errors['firstName'] = 'First name is required';
    if (!this.form.phone.trim()) this.errors['phone'] = 'Phone number is required';
    if (this.form.email.trim() && !this.EMAIL_PATTERN.test(this.form.email.trim()))
      this.errors['email'] = 'Enter a valid email address';
    if (!this.form.password.trim()) this.errors['password'] = 'Password is required';
    return Object.keys(this.errors).length === 0;
  }

  register(): void {
    if (!this.validate() || this.loading()) return;

    this.loading.set(true);
    this.error.set(null);

    this.authService.register({
      firstName: this.form.firstName.trim(),
      lastName: this.form.lastName?.trim() || undefined,
      phone: this.form.phone.trim(),
      email: this.form.email.trim() || undefined,
      password: this.form.password,
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(extractApiError(err, 'Registration failed. Please try again.'));
      }
    });
  }
}
