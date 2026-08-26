import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';

// Auth endpoints that must never trigger a refresh-and-retry: a 401 from one of these IS
// the authentication failure, and retrying would recurse.
const AUTH_ENDPOINTS = ['/auth/login', '/auth/refresh-token', '/auth/logout'];

/**
 * Sends every HTTP request with `withCredentials: true` so the browser attaches the
 * httpOnly auth cookies (`ap_access`, `ap_refresh`) on cross-origin requests to the API.
 *
 * Tokens are NEVER read from JavaScript or attached as an Authorization header —
 * that flow is reserved for the Flutter mobile app. The browser manages the cookies
 * entirely; this interceptor only orchestrates the silent refresh-and-retry on 401.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const confirmationService = inject(ConfirmationService);

  const isAssetRequest = req.url.startsWith('/assets/') || req.url.includes('/assets/');

  const isAuthEndpoint = AUTH_ENDPOINTS.some(path => req.url.includes(path));

  // Attach withCredentials so the browser sends httpOnly cookies (ap_access, ap_refresh).
  // Assets do not need credentials and may be served from a different origin.
  const authReq = isAssetRequest
    ? req
    : req.clone({ withCredentials: true });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        if (isAuthEndpoint) {
          // A 401 from /login or /refresh-token is the terminal auth failure — retrying
          // would recurse. The refresh cookie is gone; clear the session and redirect.
          authService.logout();
          router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => error);
        }

        // The access cookie has most likely just expired. Rotate it via the httpOnly
        // refresh cookie and replay the request once. Concurrent 401s share a single
        // rotation inside AuthService.
        return authService.refreshToken().pipe(
          switchMap(result => {
            if (result.status === 'renewed') {
              // Cookies have been rotated by the server. Retry the original request —
              // the new access cookie will be attached automatically by the browser.
              return next(authReq);
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
