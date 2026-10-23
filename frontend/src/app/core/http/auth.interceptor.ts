import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

/**
 * Validates whether the given URL targets this application's backend API or auth endpoints.
 * Explicitly rejects external URLs, protocol-relative URLs, and non-backend local assets.
 */
export function isBackendUrl(url: string): boolean {
  if (url.startsWith('//')) {
    return false;
  }

  if (url.startsWith('/api') || url.startsWith('/auth')) {
    return true;
  }

  if (url.startsWith('http://') || url.startsWith('https://')) {
    try {
      const parsed = new URL(url);
      const isSameOrigin = typeof window !== 'undefined' && parsed.origin === window.location.origin;
      const isBackendPath = parsed.pathname.startsWith('/api') || parsed.pathname.startsWith('/auth');
      return isSameOrigin && isBackendPath;
    } catch {
      return false;
    }
  }

  return false;
}

/**
 * Attaches the JWT Bearer token strictly to requests targeting the application backend.
 * Never exposes the token to external hosts or third-party APIs.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (!isBackendUrl(req.url)) {
    return next(req);
  }

  const token = authService.getStoredToken();
  if (!token) {
    return next(req);
  }

  // Preserve existing Authorization header if explicitly set by caller
  if (req.headers.has('Authorization')) {
    return next(req);
  }

  const clonedReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(clonedReq);
};
