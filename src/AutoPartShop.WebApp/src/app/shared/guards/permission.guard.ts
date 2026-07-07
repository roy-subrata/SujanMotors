import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Permission Guard - mirrors the API's HasPermission policies.
 * The permission list comes from the login response; the Admin role bypasses
 * checks (handled inside AuthService, same as the server-side handler).
 *
 * Usage:
 * {
 *   path: 'inventory',
 *   canActivate: [permissionGuard],
 *   data: { permissions: ['inventory.view'] } // any-of
 * }
 */
export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    router.navigate(['/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }

  const requiredPermissions = route.data['permissions'] as string[] | undefined;

  if (!requiredPermissions || requiredPermissions.length === 0) {
    return true;
  }

  if (!authService.hasAnyPermission(requiredPermissions)) {
    console.warn(`Access denied. Required permission(s): ${requiredPermissions.join(', ')}`);
    router.navigate(['/unauthorized']);
    return false;
  }

  return true;
};
