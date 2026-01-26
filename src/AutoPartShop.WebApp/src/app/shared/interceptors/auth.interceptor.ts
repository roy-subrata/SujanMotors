import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip auth header for static assets (translation files, etc.)
  const isAssetRequest = req.url.startsWith('/assets/') || req.url.includes('/assets/');

  // Get the auth token from the service
  const authToken = authService.getToken();

  // Clone the request and add the authorization header if token exists
  let authReq = req;
  if (authToken && !isAssetRequest) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${authToken}`
      }
    });
  }

  // Handle the request and catch errors
  return next(authReq).pipe(
    catchError((error) => {
      // Handle 401 Unauthorized errors
      if (error.status === 401) {
        // Token might be expired, try to refresh or logout
        authService.logout();
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url }
        });
      }

      // Handle 403 Forbidden errors
      if (error.status === 403) {
        console.error('Access forbidden - insufficient permissions');
        router.navigate(['/unauthorized']);
      }

      return throwError(() => error);
    })
  );
};
