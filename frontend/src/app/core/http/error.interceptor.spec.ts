import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from '../auth/auth.service';

describe('errorInterceptor', () => {
  let client: HttpClient;
  let httpTesting: HttpTestingController;
  let mockAuthService: { logout: ReturnType<typeof vi.fn> };
  let mockRouter: { url: string; navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    mockAuthService = {
      logout: vi.fn()
    };

    mockRouter = {
      url: '/products',
      navigate: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter }
      ]
    });

    client = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should logout and redirect to /login with returnUrl on 401 from authenticated endpoint', () => {
    client.get('/api/products').subscribe({
      next: () => {
        throw new Error('Should have failed');
      },
      error: (err) => {
        expect(err.status).toBe(401);
      }
    });

    const req = httpTesting.expectOne('/api/products');
    req.flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(mockAuthService.logout).toHaveBeenCalledWith(false);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/products' }
    });
  });

  it('should NOT logout or redirect to /login when 401 is received from /auth/login (loop prevention)', () => {
    mockRouter.url = '/login';

    client.post('/auth/login', { email: 'wrong@sistema.com', password: 'bad' }).subscribe({
      next: () => {
        throw new Error('Should have failed');
      },
      error: (err) => {
        expect(err.status).toBe(401);
      }
    });

    const req = httpTesting.expectOne('/auth/login');
    req.flush({ detail: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

    expect(mockAuthService.logout).not.toHaveBeenCalled();
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('should NOT logout or redirect to /login when 403 Forbidden is received', () => {
    client.get('/api/audit').subscribe({
      next: () => {
        throw new Error('Should have failed');
      },
      error: (err) => {
        expect(err.status).toBe(403);
      }
    });

    const req = httpTesting.expectOne('/api/audit');
    req.flush(null, { status: 403, statusText: 'Forbidden' });

    expect(mockAuthService.logout).not.toHaveBeenCalled();
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });
});
