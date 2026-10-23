import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { signal } from '@angular/core';
import { adminGuard, authGuard, unauthGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('Route Guards', () => {
  let router: Router;
  let mockAuthService: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    isAdmin: ReturnType<typeof vi.fn>;
  };

  const dummyRoute = {} as ActivatedRouteSnapshot;
  const dummyState = { url: '/products' } as RouterStateSnapshot;

  beforeEach(() => {
    mockAuthService = {
      isAuthenticated: vi.fn(),
      isAdmin: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: mockAuthService }
      ]
    });

    router = TestBed.inject(Router);
  });

  describe('authGuard', () => {
    it('should allow access when user is authenticated', () => {
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() => authGuard(dummyRoute, dummyState));
      expect(result).toBe(true);
    });

    it('should redirect to /login with returnUrl when user is not authenticated', () => {
      mockAuthService.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => authGuard(dummyRoute, dummyState)) as UrlTree;
      expect(result instanceof UrlTree).toBe(true);
      expect(router.serializeUrl(result)).toBe('/login?returnUrl=%2Fproducts');
    });
  });

  describe('adminGuard', () => {
    it('should redirect unauthenticated user to /login with returnUrl', () => {
      mockAuthService.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => adminGuard(dummyRoute, dummyState)) as UrlTree;
      expect(result instanceof UrlTree).toBe(true);
      expect(router.serializeUrl(result)).toBe('/login?returnUrl=%2Fproducts');
    });

    it('should allow access when authenticated user is an Admin', () => {
      mockAuthService.isAuthenticated.mockReturnValue(true);
      mockAuthService.isAdmin.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() => adminGuard(dummyRoute, dummyState));
      expect(result).toBe(true);
    });

    it('should redirect non-admin authenticated user to /dashboard without logging out', () => {
      mockAuthService.isAuthenticated.mockReturnValue(true);
      mockAuthService.isAdmin.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => adminGuard(dummyRoute, dummyState)) as UrlTree;
      expect(result instanceof UrlTree).toBe(true);
      expect(router.serializeUrl(result)).toBe('/dashboard');
    });
  });

  describe('unauthGuard', () => {
    it('should allow guest access when user is not authenticated', () => {
      mockAuthService.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => unauthGuard(dummyRoute, dummyState));
      expect(result).toBe(true);
    });

    it('should redirect to /dashboard when user is already authenticated', () => {
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() => unauthGuard(dummyRoute, dummyState)) as UrlTree;
      expect(result instanceof UrlTree).toBe(true);
      expect(router.serializeUrl(result)).toBe('/dashboard');
    });
  });
});
