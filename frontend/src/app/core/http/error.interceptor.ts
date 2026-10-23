import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

/**
 * Handles HTTP error responses globally across application requests.
 * - 401: Clears session and redirects to `/login` (except for login attempts and unauthenticated routes).
 * - 403: Preserves the active session and surfaces permission errors to callers without logout.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        // Prevent redirect loops on login failure
        const isLoginRequest = req.url.includes('/auth/login');

        if (error.status === 401 && !isLoginRequest) {
          authService.logout(false);

          const currentUrl = router.url;
          if (!currentUrl.startsWith('/login')) {
            router.navigate(['/login'], {
              queryParams: { returnUrl: currentUrl }
            });
          }
        }

        // 403 Forbidden: do NOT logout user, let caller or page handle permission denial
        if (error.status === 403) {
          // User remains authenticated; permission denial is passed to caller
        }
      }

      return throwError(() => error);
    })
  );
};
