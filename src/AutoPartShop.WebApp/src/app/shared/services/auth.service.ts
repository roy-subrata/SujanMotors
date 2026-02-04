import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject, tap, catchError, of } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions?: string[];
}

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
  private readonly apiUrl =`${environment.apiUrl}/auth`; 

  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  // Signals for reactive state management
  public isAuthenticated = signal(false);
  public currentUser = signal<User | null>(null);

  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'current_user';

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
   * Logout current user
   */
  logout(): void {
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
   * Refresh authentication token
   */
  refreshToken(): Observable<{ token: string }> {
    const currentToken = this.getToken();
    if (!currentToken) {
      return of({ token: '' });
    }

    return this.http.post<{ token: string }>(`${this.apiUrl}/refresh-token`, {
      token: currentToken
    }).pipe(
      tap(response => {
        if (response.token) {
          this.setToken(response.token);
        }
      }),
      catchError(error => {
        console.error('Token refresh failed:', error);
        this.logout();
        return of({ token: '' });
      })
    );
  }

  /**
   * Get current JWT token
   */
  getToken(): string | null {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Check if user is authenticated
   */
  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    // Check if token is expired
    if (this.isTokenExpired(token)) {
      this.clearSession();
      return false;
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
   * Check if user has a specific permission
   */
  hasPermission(permission: string): boolean {
    const user = this.currentUser();
    return user?.permissions?.includes(permission) ?? false;
  }

  /**
   * Check if user has any of the specified permissions
   */
  hasAnyPermission(permissions: string[]): boolean {
    const user = this.currentUser();
    if (!user || !user.permissions) return false;
    return permissions.some(permission => user.permissions.includes(permission));
  }

  /**
   * Check if user has all specified permissions
   */
  hasAllPermissions(permissions: string[]): boolean {
    const user = this.currentUser();
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

  private setToken(token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem(this.TOKEN_KEY, token);
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
      localStorage.removeItem(this.USER_KEY);
    }
    this.updateAuthState(false, null);
  }

  private loadStoredAuth(): void {
    if (typeof window === 'undefined') return;

    const token = this.getToken();
    const userStr = localStorage.getItem(this.USER_KEY);

    if (token && userStr && !this.isTokenExpired(token)) {
      try {
        const user: User = JSON.parse(userStr);
        this.updateAuthState(true, user);
      } catch (e) {
        console.error('Failed to parse stored user:', e);
        this.clearSession();
      }
    } else {
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
