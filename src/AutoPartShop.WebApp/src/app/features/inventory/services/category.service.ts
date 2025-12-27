import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CategoryResponse {
  id: string;
  name: string;
  description: string;
  code: string;
  parentCategoryId: string | null;
  isActive: boolean;
  displayOrder: number;
  createdBy: string;
  modifiedBy: string;
  breadcrumbPath: string;
  depthLevel: number;
  childCount: number;
  subCategories: CategoryResponse[];
}

export interface CreateCategoryRequest {
  name: string;
  description: string;
  code: string;
  displayOrder: number;
  parentCategoryId: string | null;
}

export interface UpdateCategoryRequest {
  id: string;
  name: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private readonly apiUrl = 'http://localhost:5292/api/categories';

  constructor(private http: HttpClient) {}

  /**
   * Get all categories
   */
  getAllCategories(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(this.apiUrl);
  }

  /**
   * Get active categories only
   */
  getActiveCategories(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(`${this.apiUrl}/active`);
  }

  /**
   * Get top-level categories (without parents)
   */
  getTopLevelCategories(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(`${this.apiUrl}/top-level`);
  }

  /**
   * Get category by ID
   */
  getCategoryById(id: string): Observable<CategoryResponse> {
    return this.http.get<CategoryResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get subcategories of a parent category
   */
  getSubcategories(parentCategoryId: string): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(`${this.apiUrl}/${parentCategoryId}/subcategories`);
  }

  /**
   * Search categories by name or code
   */
  searchCategories(searchTerm: string): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(`${this.apiUrl}/search/${searchTerm}`);
  }

  /**
   * Get paginated categories with optional search filter
   */
  getPagedCategories(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): Observable<any> {
    const params: any = { pageNumber: pageNumber.toString(), pageSize: pageSize.toString() };
    if (searchTerm.trim()) {
      params.searchTerm = searchTerm;
    }
    return this.http.get<any>(`${this.apiUrl}/paged`, { params });
  }

  /**
   * Create a new category
   */
  createCategory(request: CreateCategoryRequest): Observable<CategoryResponse> {
    return this.http.post<CategoryResponse>(this.apiUrl, request);
  }

  /**
   * Update an existing category
   */
  updateCategory(id: string, request: UpdateCategoryRequest): Observable<CategoryResponse> {
    return this.http.put<CategoryResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete a category
   */
  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Activate a category
   */
  activateCategory(id: string): Observable<CategoryResponse> {
    return this.http.patch<CategoryResponse>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate a category
   */
  deactivateCategory(id: string): Observable<CategoryResponse> {
    return this.http.patch<CategoryResponse>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Get breadcrumb path for a category
   */
  getCategoryBreadcrumb(categoryId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${categoryId}/breadcrumb`);
  }

  /**
   * Get ancestors of a category (path to root)
   */
  getAncestors(categoryId: string): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(`${this.apiUrl}/${categoryId}/ancestors`);
  }

  /**
   * Get descendants of a category
   */
  getDescendants(categoryId: string): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(`${this.apiUrl}/${categoryId}/descendants`);
  }

  /**
   * Get category depth level
   */
  getCategoryDepth(categoryId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${categoryId}/depth`);
  }

  /**
   * Check if moving a category would create a circular reference
   */
  checkCircularReference(categoryId: string, newParentId: string | null): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${categoryId}/check-circular-reference`, {}, {
      params: { newParentId: newParentId || '' }
    });
  }
}
