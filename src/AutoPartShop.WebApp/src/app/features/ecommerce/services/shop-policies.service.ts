import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, shareReplay } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ShopPolicies, DEFAULT_SHOP_POLICIES } from '../models/catalog.model';

@Injectable({ providedIn: 'root' })
export class ShopPoliciesService {
  private readonly http = inject(HttpClient);

  // Fetched once per app session, shared to all subscribers
  private readonly policies$: Observable<ShopPolicies> = this.http
    .get<ShopPolicies>(`${environment.apiUrl}/catalog/shop-policies`)
    .pipe(
      catchError(() => of(DEFAULT_SHOP_POLICIES)),
      shareReplay(1)
    );

  get(): Observable<ShopPolicies> {
    return this.policies$;
  }
}
