import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.parseUrl('/login');
};

export const salesmanGuard: CanActivateFn = (): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.parseUrl('/login');
  }

  if (!auth.canAccessSalesman()) {
    if (auth.canAccessMarketing()) {
      return router.parseUrl('/marketing-team');
    }
    return router.parseUrl('/login');
  }

  return true;
};

export const marketingGuard: CanActivateFn = (): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.parseUrl('/login');
  }

  if (!auth.canAccessMarketing()) {
    if (auth.canAccessSalesman()) {
      return router.parseUrl('/salesman');
    }
    return router.parseUrl('/login');
  }

  return true;
};
