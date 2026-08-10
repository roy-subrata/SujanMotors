import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';

// Auth endpoints that must never trigger a refresh-and-retry: a 401 from one of these IS
// the authentication failure, and retrying would recurse.
const AUTH_ENDPOINTS = ['/auth/login', '/auth/refresh-token', '/auth/logout'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const confirmationService = inject(ConfirmationService);

  const isAssetRequest = req.url.startsWith('/assets/') || req.url.includes('/assets/');

  const isAuthEndpoint = AUTH_ENDPOINTS.some(path => req.url.includes(path));

  const withToken = (request: HttpRequest<unknown>, token: string | null) =>
    token && !isAssetRequest
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  const authReq = withToken(req, authService.getToken());

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        if (isAuthEndpoint || !authService.getRefreshToken()) {
          authService.logout();
          router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => error);
        }

        // The access token has most likely just expired. Rotate it and replay the request
        // once; concurrent 401s share a single rotation inside AuthService.
        return authService.refreshToken().pipe(
          switchMap(result => {
            if (result.status === 'renewed') {
              return next(withToken(req, result.token));
            }

            // Throttled or unreachable — the session is probably fine, we just could not
            // renew right now. Surface the failure and keep the user signed in; the next
            // request will try again.
            if (result.status === 'throttled') {
              return throwError(() => error);
            }

            // Session is genuinely over — refreshToken() has already cleared it.
            router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
            return throwError(() => error);
          })
        );
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
