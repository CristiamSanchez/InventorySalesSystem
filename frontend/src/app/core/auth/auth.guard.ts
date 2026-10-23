import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Guard that ensures a user has an active authenticated session.
 * Redirects to `/login` preserving the attempted return URL.
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};

/**
 * Guard that ensures the authenticated user possesses the 'Admin' role.
 * Redirects non-admins to `/dashboard` without destroying their session.
 */
export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url }
    });
  }

  if (authService.isAdmin()) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};

/**
 * Guard for guest-only pages (e.g. `/login`).
 * Redirects already-authenticated users to `/dashboard`.
 */
export const unauthGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};
