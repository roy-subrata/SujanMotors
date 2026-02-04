import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface UserResponse {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  roles: string[];
}

export interface RoleResponse {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface PermissionResponse {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  category: string;
  isActive: boolean;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles?: string[];
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface AssignRolesRequest {
  roles: string[];
}

export interface CreatePermissionRequest {
  name: string;
  displayName: string;
  category: string;
  description?: string;
}

export interface AssignPermissionsRequest {
  permissionIds: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl =`${environment.apiUrl}/admin`; 

  // User Management
  getAllUsers(): Observable<UserResponse[]> {
    return this.http.get<UserResponse[]>(`${this.apiUrl}/users`);
  }

  getUserById(id: string): Observable<UserResponse> {
    return this.http.get<UserResponse>(`${this.apiUrl}/users/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/users`, request);
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<any> {
    return this.http.put(`${this.apiUrl}/users/${id}`, request);
  }

  toggleUserStatus(id: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/users/${id}/toggle-status`, {});
  }

  resetUserPassword(id: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/users/${id}/reset-password`, { newPassword });
  }

  // Role Management
  getAllRoles(): Observable<RoleResponse[]> {
    return this.http.get<RoleResponse[]>(`${this.apiUrl}/roles`);
  }

  createRole(request: CreateRoleRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/roles`, request);
  }

  updateRole(id: string, request: UpdateRoleRequest): Observable<any> {
    return this.http.put(`${this.apiUrl}/roles/${id}`, request);
  }

  deleteRole(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/roles/${id}`);
  }

  // User-Role Assignment
  getUserRoles(userId: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/users/${userId}/roles`);
  }

  assignRolesToUser(userId: string, request: AssignRolesRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/users/${userId}/roles`, request);
  }

  // Permission Management
  getAllPermissions(): Observable<PermissionResponse[]> {
    return this.http.get<PermissionResponse[]>(`${this.apiUrl}/permissions`);
  }

  createPermission(request: CreatePermissionRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/permissions`, request);
  }

  getRolePermissions(roleId: string): Observable<PermissionResponse[]> {
    return this.http.get<PermissionResponse[]>(`${this.apiUrl}/roles/${roleId}/permissions`);
  }

  assignPermissionsToRole(roleId: string, request: AssignPermissionsRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/roles/${roleId}/permissions`, request);
  }
}
