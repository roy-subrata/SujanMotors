import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Role Guard - Checks if user has required role(s)
 * Usage in routes:
 * {
 *   path: 'admin',
 *   component: AdminComponent,
 *   canActivate: [roleGuard],
 *   data: { roles: ['Admin'] } // Single role
 * }
 * OR
 * {
 *   path: 'manager',
 *   component: ManagerComponent,
 *   canActivate: [roleGuard],
 *   data: {
 *     roles: ['Admin', 'Manager'], // Multiple roles (any)
 *     requireAll: false // default: false (any role), true (all roles)
 *   }
 * }
 */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // First check if user is logged in
  if (!authService.isLoggedIn()) {
    router.navigate(['/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }

  // Get required roles from route data
  const requiredRoles = route.data['roles'] as string[] | undefined;

  // If no roles specified, just check authentication
  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  // Check if user should have all roles or just any role
  const requireAll = route.data['requireAll'] as boolean ?? false;

  // Check user roles
  const hasAccess = requireAll
    ? authService.hasAllRoles(requiredRoles)
    : authService.hasAnyRole(requiredRoles);

  if (!hasAccess) {
    console.warn(`Access denied. Required roles: ${requiredRoles.join(', ')}`);
    router.navigate(['/unauthorized']);
    return false;
  }

  return true;
};
