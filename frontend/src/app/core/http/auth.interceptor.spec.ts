import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor, isBackendUrl } from './auth.interceptor';
import { AuthService } from '../auth/auth.service';

describe('authInterceptor', () => {
  let client: HttpClient;
  let httpTesting: HttpTestingController;
  let mockAuthService: { getStoredToken: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    mockAuthService = {
      getStoredToken: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService }
      ]
    });

    client = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('isBackendUrl', () => {
    it('should identify local /api and /auth relative endpoints as backend targets', () => {
      expect(isBackendUrl('/api/products')).toBe(true);
      expect(isBackendUrl('/api/sales/123')).toBe(true);
      expect(isBackendUrl('/auth/login')).toBe(true);
      expect(isBackendUrl('/auth/me')).toBe(true);
    });

    it('should reject non-backend relative endpoints', () => {
      expect(isBackendUrl('/assets/images/logo.png')).toBe(false);
      expect(isBackendUrl('/styles.css')).toBe(false);
      expect(isBackendUrl('/favicon.ico')).toBe(false);
    });

    it('should reject external and protocol-relative URLs', () => {
      expect(isBackendUrl('//evil.com/api')).toBe(false);
      expect(isBackendUrl('https://api.external-service.com/data')).toBe(false);
      expect(isBackendUrl('http://untrusted-host.org/auth/login')).toBe(false);
    });

    it('should accept same-origin absolute URLs targeting /api or /auth', () => {
      const origin = typeof window !== 'undefined' ? window.location.origin : 'http://localhost';
      expect(isBackendUrl(`${origin}/api/customers`)).toBe(true);
      expect(isBackendUrl(`${origin}/auth/me`)).toBe(true);
      expect(isBackendUrl(`${origin}/other`)).toBe(false);
    });
  });

  describe('interceptor behavior', () => {
    it('should attach Bearer token to backend requests when token is present', () => {
      mockAuthService.getStoredToken.mockReturnValue('jwt-token-123');

      client.get('/api/products').subscribe();

      const req = httpTesting.expectOne('/api/products');
      expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-token-123');
      req.flush([]);
    });

    it('should NOT attach token to external URLs even when token exists', () => {
      mockAuthService.getStoredToken.mockReturnValue('jwt-token-123');

      client.get('https://external-api.com/api/data').subscribe();

      const req = httpTesting.expectOne('https://external-api.com/api/data');
      expect(req.request.headers.has('Authorization')).toBe(false);
      req.flush({});
    });

    it('should NOT attach token when no token exists in storage', () => {
      mockAuthService.getStoredToken.mockReturnValue(null);

      client.get('/api/categories').subscribe();

      const req = httpTesting.expectOne('/api/categories');
      expect(req.request.headers.has('Authorization')).toBe(false);
      req.flush([]);
    });

    it('should preserve custom Authorization header if already set by caller', () => {
      mockAuthService.getStoredToken.mockReturnValue('default-token');

      client.get('/api/reports', {
        headers: { Authorization: 'CustomApiKey key-456' }
      }).subscribe();

      const req = httpTesting.expectOne('/api/reports');
      expect(req.request.headers.get('Authorization')).toBe('CustomApiKey key-456');
      req.flush({});
    });
  });
});
