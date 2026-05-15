import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface AttributeOption {
  id: string;
  attributeId: string;
  value: string;
  sortOrder: number;
}

export interface ProductAttribute {
  id: string;
  attributeGroupId: string;
  name: string;
  code: string;
  dataType: 'text' | 'number' | 'boolean' | 'option';
  unit: string;
  isActive: boolean;
  options: AttributeOption[];
}

export interface ProductAttributeGroup {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
  attributes: ProductAttribute[];
}

export interface CreateAttributeGroupRequest {
  name: string;
  sortOrder?: number;
  isActive?: boolean;
}

export interface CreateAttributeRequest {
  name: string;
  code: string;
  dataType?: string;
  unit?: string;
  isActive?: boolean;
}

export interface CreateOptionRequest {
  value: string;
  sortOrder?: number;
}

export interface AttributeGroupQuery {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
}

export interface PaginationMeta {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PagedGroupResult {
  data: ProductAttributeGroup[];
  pagination: PaginationMeta;
}

@Injectable({ providedIn: 'root' })
export class ProductAttributeService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/attribute-groups`;

  getAllGroups(): Observable<ProductAttributeGroup[]> {
    return this.http.get<ProductAttributeGroup[]>(this.base);
  }

  getGroupsPaged(query: AttributeGroupQuery): Observable<PagedGroupResult> {
    return this.http.post<PagedGroupResult>(`${this.base}/list`, query);
  }

  getGroupById(id: string): Observable<ProductAttributeGroup> {
    return this.http.get<ProductAttributeGroup>(`${this.base}/${id}`);
  }

  createGroup(req: CreateAttributeGroupRequest): Observable<ProductAttributeGroup> {
    return this.http.post<ProductAttributeGroup>(this.base, req);
  }

  updateGroup(id: string, req: CreateAttributeGroupRequest): Observable<ProductAttributeGroup> {
    return this.http.put<ProductAttributeGroup>(`${this.base}/${id}`, req);
  }

  deleteGroup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  addAttribute(groupId: string, req: CreateAttributeRequest): Observable<ProductAttribute> {
    return this.http.post<ProductAttribute>(`${this.base}/${groupId}/attributes`, req);
  }

  updateAttribute(groupId: string, attrId: string, req: CreateAttributeRequest): Observable<ProductAttribute> {
    return this.http.put<ProductAttribute>(`${this.base}/${groupId}/attributes/${attrId}`, req);
  }

  deleteAttribute(groupId: string, attrId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${groupId}/attributes/${attrId}`);
  }

  addOption(groupId: string, attrId: string, req: CreateOptionRequest): Observable<AttributeOption> {
    return this.http.post<AttributeOption>(`${this.base}/${groupId}/attributes/${attrId}/options`, req);
  }

  updateOption(groupId: string, attrId: string, optId: string, req: CreateOptionRequest): Observable<AttributeOption> {
    return this.http.put<AttributeOption>(`${this.base}/${groupId}/attributes/${attrId}/options/${optId}`, req);
  }

  deleteOption(groupId: string, attrId: string, optId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${groupId}/attributes/${attrId}/options/${optId}`);
  }
}
