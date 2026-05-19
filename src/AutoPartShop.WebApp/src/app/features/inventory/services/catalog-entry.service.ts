import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface CatalogEntryResponse {
  partId: string;
  slug: string;
  shortDescription: string;
  isPublished: boolean;
  publishedAt?: string | null;
  isFeatured: boolean;
  featuredRank: number;
  metaTitle?: string | null;
  metaDescription?: string | null;
}

export interface UpsertCatalogEntryRequest {
  slug: string;
  shortDescription?: string;
  isPublished: boolean;
  isFeatured: boolean;
  featuredRank: number;
  metaTitle?: string | null;
  metaDescription?: string | null;
}

@Injectable({ providedIn: 'root' })
export class CatalogEntryService {
  private readonly http = inject(HttpClient);

  private url(partId: string): string {
    return `${environment.apiUrl}/parts/${partId}/catalog-entry`;
  }

  get(partId: string): Observable<CatalogEntryResponse | null> {
    return this.http.get<CatalogEntryResponse | null>(this.url(partId));
  }

  upsert(partId: string, req: UpsertCatalogEntryRequest): Observable<CatalogEntryResponse> {
    return this.http.put<CatalogEntryResponse>(this.url(partId), req);
  }
}
