import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface CustomerUser {
  customerId: string;
  customerCode: string;
  fullName: string;
  email: string;
  phone: string;
}

export interface CustomerLoginResponse {
  token: string;
  customerId: string;
  customerCode: string;
  fullName: string;
  email: string;
  phone: string;
}

export interface CustomerRegisterRequest {
  firstName: string;
  lastName?: string;
  phone: string;
  email?: string;
  password: string;
  address?: string;
  city?: string;
}

@Injectable({ providedIn: 'root' })
export class CustomerAuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = `${environment.apiUrl}/v1/customer-auth`;

  private readonly TOKEN_KEY = 'customer_token';
  private readonly USER_KEY = 'customer_user';

  isLoggedIn = signal(false);
  currentCustomer = signal<CustomerUser | null>(null);

  constructor() {
    this.loadStoredAuth();
  }

  login(identifier: string, password: string): Observable<CustomerLoginResponse> {
    return this.http.post<CustomerLoginResponse>(`${this.apiUrl}/login`, { identifier, password }).pipe(
      tap(response => this.setSession(response)),
      catchError(error => throwError(() => error))
    );
  }

  register(request: CustomerRegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, request).pipe(
      catchError(error => throwError(() => error))
    );
  }

  logout(): void {
    this.clearSession();
    this.router.navigate(['/shop/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isCustomerLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;
    if (this.isTokenExpired(token)) {
      this.clearSession();
      return false;
    }
    return true;
  }

  private setSession(response: CustomerLoginResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    const user: CustomerUser = {
      customerId: response.customerId,
      customerCode: response.customerCode,
      fullName: response.fullName,
      email: response.email,
      phone: response.phone,
    };
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.isLoggedIn.set(true);
    this.currentCustomer.set(user);
  }

  private clearSession(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.isLoggedIn.set(false);
    this.currentCustomer.set(null);
  }

  private loadStoredAuth(): void {
    const token = this.getToken();
    const userStr = localStorage.getItem(this.USER_KEY);
    if (token && userStr && !this.isTokenExpired(token)) {
      try {
        const user: CustomerUser = JSON.parse(userStr);
        this.isLoggedIn.set(true);
        this.currentCustomer.set(user);
      } catch {
        this.clearSession();
      }
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return Math.floor(Date.now() / 1000) >= payload.exp;
    } catch {
      return true;
    }
  }
}
