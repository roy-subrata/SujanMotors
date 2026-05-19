import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CustomerAuthService } from '../services/customer-auth.service';

export const customerAuthGuard: CanActivateFn = (_route, state) => {
  const authService = inject(CustomerAuthService);
  const router = inject(Router);

  if (authService.isCustomerLoggedIn()) return true;

  router.navigate(['/shop/login'], { queryParams: { returnUrl: state.url } });
  return false;
};
