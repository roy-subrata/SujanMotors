import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject, tap, catchError, of, map, finalize, shareReplay } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  username: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions?: string[];
}

export interface RefreshTokenResponse {
  token: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  roles: string[];
  permissions?: string[];
}

/**
 * Outcome of a token renewal.
 *
 * 'throttled' must stay distinct from 'expired': the rate limiter can reject a renewal that
 * would otherwise have succeeded — a whole shop shares one IP and sessions expire in step —
 * and treating that as a dead session would sign a cashier out mid-shift over a transient 429.
 */
export type RefreshResult =
  | { status: 'renewed'; token: string }
  | { status: 'throttled' }
  | { status: 'expired' };

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  defaultRole?: string;
}

export interface User {
  username: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl =`${environment.apiUrl}/v1/auth`;

  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  // Signals for reactive state management
  public isAuthenticated = signal(false);
  public currentUser = signal<User | null>(null);

  private readonly USER_KEY = 'current_user';

  /**
   * The in-flight refresh, shared so that several requests failing with 401 at once
   * trigger a single rotation. Rotation is single-use server-side: firing two in
   * parallel would spend the token twice and trip reuse detection, killing the session.
   */
  private refreshInFlight$: Observable<RefreshResult> | null = null;

  constructor() {
    // Restore user profile from previous session (UX data only — tokens live in
    // httpOnly cookies, invisible to JavaScript).
    this.loadStoredAuth();
  }

  /**
   * Login user with username and password.
   *
   * The server sets httpOnly cookies (ap_access + ap_refresh) on success. The response
   * body still carries token/refreshToken for mobile clients that use them directly;
   * the web SPA ignores those values entirely.
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => {
        this.setSession(response);
      }),
      catchError(error => {
        console.error('Login error:', error);
        throw error;
      })
    );
  }

  /**
   * Register new user
   */
  register(request: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, request);
  }

  /**
   * Logout current user.
   *
   * Sends an empty body — the httpOnly refresh cookie is the credential. The server
   * revokes the session and expires both cookies. Local state is cleared regardless
   * of network success so a failure can never trap the user in a signed-in state.
   */
  logout(): void {
    // Fire-and-forget: the server reads the refresh cookie, revokes it, and expires
    // both cookies. We do not wait for the response.
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe({
      error: () => { /* best-effort: cookies expire on their own */ }
    });

    this.clearSession();
    this.router.navigate(['/login']);
  }

  /**
   * Change user password
   */
  changePassword(username: string, currentPassword: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/change-password`, {
      username,
      currentPassword,
      newPassword
    });
  }

  /**
   * Exchanges the httpOnly refresh cookie for a fresh access token, rotating the refresh
   * token in the process. No body is sent — the cookie carries the credential.
   *
   * Concurrent callers share one request — see {@link refreshInFlight$}. On failure the
   * session is cleared but no redirect happens here; the caller decides where to send
   * the user, so a background refresh cannot yank someone off the page mid-edit.
   */
  refreshToken(): Observable<RefreshResult> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    this.refreshInFlight$ = this.http
      .post<RefreshTokenResponse>(`${this.apiUrl}/refresh-token`, {})
      .pipe(
        tap(response => this.applyRefresh(response)),
        map(response => ({ status: 'renewed', token: response.token }) as RefreshResult),
        catchError((error: HttpErrorResponse) => {
          // Retryable: the limiter rejected us (429), or the request never reached the
          // server (status 0 / 5xx). The session may well still be valid, so keep it and
          // let the next attempt renew.
          if (error.status === 429 || error.status === 0 || error.status >= 500) {
            return of<RefreshResult>({ status: 'throttled' });
          }

          // 401 — expired, revoked, or reuse-detected. All unrecoverable.
          this.clearSession();
          return of<RefreshResult>({ status: 'expired' });
        }),
        finalize(() => {
          this.refreshInFlight$ = null;
        }),
        shareReplay(1)
      );

    return this.refreshInFlight$;
  }

  /**
   * Get current JWT token.
   *
   * Tokens live in httpOnly cookies and are never accessible to JavaScript.
   * This always returns null. Interceptors use withCredentials instead.
   */
  getToken(): string | null {
    return null;
  }

  /**
   * Get the current refresh token.
   *
   * The refresh token lives in an httpOnly cookie and is never accessible to JavaScript.
   * This always returns null.
   */
  getRefreshToken(): string | null {
    return null;
  }

  /**
   * Check if user is authenticated.
   *
   * With cookie-based auth, we cannot read the access token to check expiry.
   * Instead we rely on the user profile stored in localStorage as a UX hint.
   * The real validation happens server-side: the first 401 triggers a silent
   * cookie refresh; only if that fails does the interceptor redirect to /login.
   */
  isLoggedIn(): boolean {
    return !!this.currentUser();
  }

  /**
   * Check if user has a specific role
   */
  hasRole(role: string): boolean {
    const user = this.currentUser();
    return user?.roles?.includes(role) ?? false;
  }

  /**
   * Check if user has any of the specified roles
   */
  hasAnyRole(roles: string[]): boolean {
    const user = this.currentUser();
    if (!user || !user.roles) return false;
    return roles.some(role => user.roles.includes(role));
  }

  /**
   * Check if user has all specified roles
   */
  hasAllRoles(roles: string[]): boolean {
    const user = this.currentUser();
    if (!user || !user.roles) return false;
    return roles.every(role => user.roles.includes(role));
  }

  /**
   * Get current user roles
   */
  getUserRoles(): string[] {
    const user = this.currentUser();
    return user?.roles ?? [];
  }

  /**
   * Check if user has a specific permission.
   * Mirrors the API: the Admin role bypasses permission checks entirely.
   */
  hasPermission(permission: string): boolean {
    const user = this.currentUser();
    if (user?.roles?.includes('Admin')) return true;
    return user?.permissions?.includes(permission) ?? false;
  }

  /**
   * Check if user has any of the specified permissions (Admin bypasses)
   */
  hasAnyPermission(permissions: string[]): boolean {
    const user = this.currentUser();
    if (user?.roles?.includes('Admin')) return true;
    if (!user || !user.permissions) return false;
    return permissions.some(permission => user.permissions.includes(permission));
  }

  /**
   * Check if user has all specified permissions (Admin bypasses)
   */
  hasAllPermissions(permissions: string[]): boolean {
    const user = this.currentUser();
    if (user?.roles?.includes('Admin')) return true;
    if (!user || !user.permissions) return false;
    return permissions.every(permission => user.permissions.includes(permission));
  }

  /**
   * Get current user permissions
   */
  getUserPermissions(): string[] {
    const user = this.currentUser();
    return user?.permissions ?? [];
  }

  // Private helper methods

  /**
   * Stores user profile data locally. Tokens are never stored in localStorage —
   * they live in httpOnly cookies set by the server on login/refresh.
   */
  private setSession(authResult: LoginResponse): void {
    const user: User = {
      username: authResult.username,
      email: authResult.email,
      fullName: authResult.fullName,
      roles: authResult.roles || [],
      permissions: authResult.permissions || []
    };

    this.setUser(user);
    this.updateAuthState(true, user);
  }

  /**
   * Updates the locally cached roles/permissions after a token rotation.
   * The server re-sends the current role/permission set on every refresh so
   * admin changes take effect without requiring a re-login.
   *
   * Tokens are NOT stored locally — they are set as httpOnly cookies by the server.
   */
  private applyRefresh(response: RefreshTokenResponse): void {
    const current = this.currentUser();
    if (current) {
      const updated: User = {
        ...current,
        roles: response.roles || [],
        permissions: response.permissions || []
      };
      this.setUser(updated);
      this.updateAuthState(true, updated);
    }
  }

  private setUser(user: User): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    }
  }

  private clearSession(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem(this.USER_KEY);
    }
    this.updateAuthState(false, null);
  }

  /**
   * Restores the session on page load from the user profile stored in localStorage.
   * Tokens live in httpOnly cookies — they cannot be read by JavaScript.
   *
   * The real validation is lazy: the first 401 from any API call triggers a silent
   * cookie-based refresh in the interceptor. If that fails, the session is cleared
   * and the user is redirected to /login.
   */
  private loadStoredAuth(): void {
    if (typeof window === 'undefined') return;

    const userStr = localStorage.getItem(this.USER_KEY);

    if (!userStr) {
      this.clearSession();
      return;
    }

    try {
      const user: User = JSON.parse(userStr);
      this.updateAuthState(true, user);
    } catch (e) {
      console.error('Failed to parse stored user:', e);
      this.clearSession();
    }
  }

  private updateAuthState(isAuthenticated: boolean, user: User | null): void {
    this.isAuthenticated.set(isAuthenticated);
    this.currentUser.set(user);
    this.isAuthenticatedSubject.next(isAuthenticated);
    this.currentUserSubject.next(user);
  }
}
