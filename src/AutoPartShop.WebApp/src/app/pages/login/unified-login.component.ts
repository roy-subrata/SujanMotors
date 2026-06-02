import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../shared/services/auth.service';
import { CustomerAuthService } from '../../features/ecommerce/services/customer-auth.service';
import { extractApiError } from '../../shared/utils/api-error.util';

type LoginMode = 'customer' | 'staff';

@Component({
  selector: 'app-unified-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './unified-login.component.html',
  styleUrls: ['./unified-login.component.css'],
})
export class UnifiedLoginComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly customerAuthService = inject(CustomerAuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly doc = inject(DOCUMENT);

  // Determined by route data — never changed by the user
  mode: LoginMode = 'customer';

  identifier = '';
  password = '';
  rememberMe = false;
  showPassword = false;

  loading = signal(false);
  error = signal<string | null>(null);
  errors: Record<string, string> = {};

  returnUrl = '';

  ngOnInit(): void {
    this.doc.body.classList.add('storefront-scroll');
    this.doc.documentElement.classList.add('storefront-scroll');

    // Mode comes from route data; /login → staff, /shop/login → customer
    this.mode = (this.route.snapshot.data?.['mode'] as LoginMode) ?? 'customer';

    if (this.mode === 'staff' && this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    if (this.mode === 'customer' && this.customerAuthService.isCustomerLoggedIn()) {
      this.router.navigateByUrl('/shop');
      return;
    }

    this.returnUrl =
      this.route.snapshot.queryParams['returnUrl'] ??
      (this.mode === 'staff' ? '/' : '/shop');
  }

  ngOnDestroy(): void {
    this.doc.body.classList.remove('storefront-scroll');
    this.doc.documentElement.classList.remove('storefront-scroll');
  }

  get identifierLabel(): string {
    return this.mode === 'customer' ? 'Phone Number or Email' : 'Username or Email';
  }

  get identifierPlaceholder(): string {
    return this.mode === 'customer'
      ? '01712345678 or your@email.com'
      : 'Enter your username or email';
  }

  validate(): boolean {
    this.errors = {};
    if (!this.identifier.trim())
      this.errors['identifier'] =
        this.mode === 'customer' ? 'Phone or email is required' : 'Username is required';
    if (!this.password.trim()) this.errors['password'] = 'Password is required';
    return Object.keys(this.errors).length === 0;
  }

  submit(): void {
    if (!this.validate()) return;
    if (this.loading()) return;

    this.loading.set(true);
    this.error.set(null);

    if (this.mode === 'customer') {
      this.customerAuthService.login(this.identifier.trim(), this.password).subscribe({
        next: () => {
          this.loading.set(false);
          this.router.navigateByUrl(this.returnUrl || '/shop');
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(extractApiError(err, 'Invalid credentials. Please try again.'));
        },
      });
    } else {
      this.authService.login({ username: this.identifier.trim(), password: this.password }).subscribe({
        next: () => {
          this.loading.set(false);
          this.router.navigateByUrl(this.returnUrl || '/');
        },
        error: (err) => {
          this.loading.set(false);
          const msg = err?.status === 0
            ? 'Unable to connect to server. Please try again.'
            : extractApiError(err, 'Invalid username or password');
          this.error.set(msg);
        },
      });
    }
  }
}
