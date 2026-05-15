import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { CustomerAuthService } from '../../features/ecommerce/services/customer-auth.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

// URLs that require the customer JWT (shop channel)
const CUSTOMER_AUTH_URLS = ['/ecommerce/checkout', '/customer-auth/'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const customerAuthService = inject(CustomerAuthService);
  const router = inject(Router);

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
      return throwError(() => error);
    })
  );
};
