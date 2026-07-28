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

  private readonly TOKEN_KEY = 'auth_token';
  private readonly REFRESH_TOKEN_KEY = 'auth_refresh_token';
  private readonly USER_KEY = 'current_user';

  /**
   * The in-flight refresh, shared so that several requests failing with 401 at once
   * trigger a single rotation. Rotation is single-use server-side: firing two in
   * parallel would spend the token twice and trip reuse detection, killing the session.
   */
  private refreshInFlight$: Observable<RefreshResult> | null = null;

  constructor() {
    // Check for existing session on service initialization
    this.loadStoredAuth();
  }

  /**
   * Login user with username and password
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
   * Revokes the session server-side so the refresh token cannot be replayed, but does not
   * wait for that call — the local session is cleared either way, so a network failure can
   * never trap the user in a signed-in state.
   */
  logout(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe({
        error: () => {
          /* best-effort: the token still expires on its own */
        }
      });
    }

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
   * Exchanges the stored refresh token for a fresh access token, rotating the refresh
   * token in the process. Emits the new access token, or null when the session is over.
   *
   * Concurrent callers share one request — see {@link refreshInFlight$}. On failure the
   * session is cleared but no redirect happens here; the caller decides where to send
   * the user, so a background refresh cannot yank someone off the page mid-edit.
   */
  refreshToken(): Observable<RefreshResult> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return of<RefreshResult>({ status: 'expired' });
    }

    this.refreshInFlight$ = this.http
      .post<RefreshTokenResponse>(`${this.apiUrl}/refresh-token`, { refreshToken })
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
   * Get current JWT token
   */
  getToken(): string | null {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Get the current refresh token, if the session is renewable.
   */
  getRefreshToken(): string | null {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * Check if user is authenticated.
   *
   * An expired access token no longer means the session is over: if a refresh token is
   * present the session is renewable, and the interceptor will rotate it on the next
   * 401. Reporting "logged out" here would bounce the user to /login on every reload
   * more than an hour after signing in.
   */
  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    if (this.isTokenExpired(token)) {
      return !!this.getRefreshToken();
    }

    return true;
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

  private setSession(authResult: LoginResponse): void {
    this.setToken(authResult.token);
    this.setRefreshToken(authResult.refreshToken);

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
   * Stores a rotation result. The refresh token MUST be replaced: the one just used is
   * spent, and presenting it again would trip server-side reuse detection and kill the
   * session. Roles and permissions are refreshed too, so a role change applied by an
   * admin takes effect on the next rotation instead of requiring a re-login.
   */
  private applyRefresh(response: RefreshTokenResponse): void {
    this.setToken(response.token);
    this.setRefreshToken(response.refreshToken);

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

  private setToken(token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem(this.TOKEN_KEY, token);
    }
  }

  private setRefreshToken(token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
    }
  }

  private setUser(user: User): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    }
  }

  private clearSession(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.REFRESH_TOKEN_KEY);
      localStorage.removeItem(this.USER_KEY);
    }
    this.updateAuthState(false, null);
  }

  /**
   * Restores the session on page load. An expired access token is fine as long as a
   * refresh token survives — the first API call will rotate it. Only a missing token or
   * an unrenewable expired one clears the session.
   */
  private loadStoredAuth(): void {
    if (typeof window === 'undefined') return;

    const token = this.getToken();
    const userStr = localStorage.getItem(this.USER_KEY);

    if (!token || !userStr) {
      this.clearSession();
      return;
    }

    if (this.isTokenExpired(token) && !this.getRefreshToken()) {
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

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp;
      const now = Math.floor(Date.now() / 1000);
      return now >= expiry;
    } catch (e) {
      console.error('Failed to decode token:', e);
      return true;
    }
  }

  /**
   * Decode JWT token to get user information
   */
  decodeToken(token: string): any {
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch (e) {
      console.error('Failed to decode token:', e);
      return null;
    }
  }
}
