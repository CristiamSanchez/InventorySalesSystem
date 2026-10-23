import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { routes } from './app.routes';
import { AuthService } from '@core';

describe('AppRoutes', () => {
  let router: Router;
  let mockAuthService: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    isAdmin: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockAuthService = {
      isAuthenticated: vi.fn().mockReturnValue(true),
      isAdmin: vi.fn().mockReturnValue(true)
    };

    await TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
  });

  it('should have configured routes', () => {
    expect(routes.length).toBeGreaterThan(0);
    const loginRoute = routes.find((r) => r.path === 'login');
    expect(loginRoute).toBeDefined();

    const rootRoute = routes.find((r) => r.path === '');
    expect(rootRoute).toBeDefined();
    expect(rootRoute?.children?.length).toBe(10);
  });

  it('should navigate to dashboard by default when authenticated', async () => {
    await router.navigateByUrl('/');
    expect(router.url).toBe('/dashboard');
  });

  it('should redirect to login when unauthenticated', async () => {
    mockAuthService.isAuthenticated.mockReturnValue(false);
    await router.navigateByUrl('/products');
    expect(router.url).toContain('/login?returnUrl=%2Fproducts');
  });

  it('should redirect unknown routes to dashboard', async () => {
    await router.navigateByUrl('/unknown-route-test');
    expect(router.url).toBe('/dashboard');
  });
});
