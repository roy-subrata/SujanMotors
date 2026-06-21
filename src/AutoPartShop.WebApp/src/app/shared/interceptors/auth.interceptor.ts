import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { CustomerAuthService } from '../../features/ecommerce/services/customer-auth.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';

// URLs that require the customer JWT (shop channel)
const CUSTOMER_AUTH_URLS = ['/ecommerce/checkout', '/customer-auth/'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const customerAuthService = inject(CustomerAuthService);
  const router = inject(Router);
  const confirmationService = inject(ConfirmationService);

  const isAssetRequest = req.url.startsWith('/assets/') || req.url.includes('/assets/');

  const needsCustomerToken = CUSTOMER_AUTH_URLS.some(path => req.url.includes(path));
  const token = needsCustomerToken
    ? customerAuthService.getToken()
    : authService.getToken();

  let authReq = req;
  if (token && !isAssetRequest) {
    authReq = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  return next(authReq).pipe(
    catchError((error) => {
      if (error.status === 401) {
        if (needsCustomerToken) {
          customerAuthService.logout();
        } else {
          authService.logout();
          router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
        }
      }
      if (error.status === 403) {
        router.navigate(['/unauthorized']);
      }
      // Optimistic-concurrency conflict: another user changed the record first. Offer a reload.
      // Targeted by the distinct CONCURRENCY_CONFLICT type so duplicate-key / business 409s
      // (handled per-page) are left untouched.
      if (error.status === 409 && error.error?.type === 'CONCURRENCY_CONFLICT') {
        confirmationService.confirm({
          key: 'global-concurrency',
          header: 'Record changed by another user',
          message: error.error?.message
            || error.error?.detail
            || 'This record was changed by another user. Reload to get the latest version?',
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: 'Reload',
          rejectLabel: 'Dismiss',
          accept: () => window.location.reload()
        });
      }
      return throwError(() => error);
    })
  );
};
