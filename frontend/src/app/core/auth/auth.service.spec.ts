import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { CurrentUserResponse, LoginRequest, LoginResponse } from './auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpTesting: HttpTestingController;
  let routerSpy: { navigate: ReturnType<typeof vi.fn> };

  const futureExpiresAt = new Date(Date.now() + 3600 * 1000).toISOString();
  const pastExpiresAt = new Date(Date.now() - 3600 * 1000).toISOString();

  beforeEach(() => {
    sessionStorage.clear();
    routerSpy = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    sessionStorage.clear();
  });

  it('should be created with null initial session', () => {
    expect(service).toBeTruthy();
    expect(service.session()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.isAdmin()).toBe(false);
  });

  describe('login', () => {
    it('should authenticate, retrieve profile via /auth/me, store session, and update signals', () => {
      const loginRequest: LoginRequest = { email: 'admin@sistema.com', password: 'Password123!' };
      const loginResponse: LoginResponse = {
        accessToken: 'mock-jwt-token',
        tokenType: 'Bearer',
        expiresAtUtc: futureExpiresAt
      };
      const meResponse: CurrentUserResponse = {
        userId: '11111111-1111-1111-1111-111111111111',
        roles: ['Admin', 'User']
      };

      let emittedSession: unknown = null;
      service.login(loginRequest).subscribe((session) => {
        emittedSession = session;
      });

      // 1. First expects POST /auth/login
      const loginReq = httpTesting.expectOne('/auth/login');
      expect(loginReq.request.method).toBe('POST');
      expect(loginReq.request.body).toEqual(loginRequest);
      loginReq.flush(loginResponse);

      // 2. Then expects GET /auth/me with Bearer token
      const meReq = httpTesting.expectOne('/auth/me');
      expect(meReq.request.method).toBe('GET');
      expect(meReq.request.headers.get('Authorization')).toBe('Bearer mock-jwt-token');
      meReq.flush(meResponse);

      expect(emittedSession).toEqual({
        userId: meResponse.userId,
        roles: meResponse.roles,
        accessToken: loginResponse.accessToken,
        expiresAtUtc: loginResponse.expiresAtUtc
      });

      expect(service.session()).toEqual(emittedSession);
      expect(service.isAuthenticated()).toBe(true);
      expect(service.isAdmin()).toBe(true);
      expect(sessionStorage.getItem('siv_access_token')).toBe('mock-jwt-token');
    });

    it('should not establish session when login fails', () => {
      const loginRequest: LoginRequest = { email: 'invalid@sistema.com', password: 'Wrong' };
      let errorOccurred = false;

      service.login(loginRequest).subscribe({
        next: () => {
          throw new Error('Should have failed');
        },
        error: () => {
          errorOccurred = true;
        }
      });

      const loginReq = httpTesting.expectOne('/auth/login');
      loginReq.flush({ detail: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

      expect(errorOccurred).toBe(true);
      expect(service.session()).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
      expect(sessionStorage.getItem('siv_access_token')).toBeNull();
    });
  });

  describe('logout', () => {
    it('should clear sessionStorage, reset signal, and navigate to /login', () => {
      sessionStorage.setItem('siv_access_token', 'token');
      sessionStorage.setItem('siv_token_expires_at', futureExpiresAt);

      service.logout();

      expect(service.session()).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
      expect(sessionStorage.getItem('siv_access_token')).toBeNull();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('initializeSession', () => {
    it('should return false without network call if token or expiry is missing from storage', () => {
      let result = true;
      service.initializeSession().subscribe((res) => {
        result = res;
      });

      expect(result).toBe(false);
      expect(service.session()).toBeNull();
      httpTesting.expectNone('/auth/me');
    });

    it('should return false and clear storage if stored token is expired', () => {
      sessionStorage.setItem('siv_access_token', 'expired-token');
      sessionStorage.setItem('siv_token_expires_at', pastExpiresAt);

      let result = true;
      service.initializeSession().subscribe((res) => {
        result = res;
      });

      expect(result).toBe(false);
      expect(sessionStorage.getItem('siv_access_token')).toBeNull();
      httpTesting.expectNone('/auth/me');
    });

    it('should validate active token through /auth/me and restore session signal', () => {
      sessionStorage.setItem('siv_access_token', 'valid-stored-token');
      sessionStorage.setItem('siv_token_expires_at', futureExpiresAt);

      let result = false;
      service.initializeSession().subscribe((res) => {
        result = res;
      });

      const meReq = httpTesting.expectOne('/auth/me');
      expect(meReq.request.headers.get('Authorization')).toBe('Bearer valid-stored-token');
      meReq.flush({
        userId: '22222222-2222-2222-2222-222222222222',
        roles: ['User']
      });

      expect(result).toBe(true);
      expect(service.session()?.userId).toBe('22222222-2222-2222-2222-222222222222');
      expect(service.isAuthenticated()).toBe(true);
      expect(service.isAdmin()).toBe(false);
    });

    it('should clear storage and return false if /auth/me rejects token with 401', () => {
      sessionStorage.setItem('siv_access_token', 'invalidated-token');
      sessionStorage.setItem('siv_token_expires_at', futureExpiresAt);

      let result = true;
      service.initializeSession().subscribe((res) => {
        result = res;
      });

      const meReq = httpTesting.expectOne('/auth/me');
      meReq.flush(null, { status: 401, statusText: 'Unauthorized' });

      expect(result).toBe(false);
      expect(service.session()).toBeNull();
      expect(sessionStorage.getItem('siv_access_token')).toBeNull();
    });
  });

  describe('isExpired', () => {
    it('should return true for past dates and false for future dates', () => {
      expect(service.isExpired(pastExpiresAt)).toBe(true);
      expect(service.isExpired(futureExpiresAt)).toBe(false);
      expect(service.isExpired('invalid-date-string')).toBe(true);
    });
  });

  describe('getStoredToken', () => {
    it('should return null when storage is empty or expired', () => {
      expect(service.getStoredToken()).toBeNull();

      sessionStorage.setItem('siv_access_token', 'token');
      sessionStorage.setItem('siv_token_expires_at', pastExpiresAt);
      expect(service.getStoredToken()).toBeNull();
    });

    it('should return token when non-expired', () => {
      sessionStorage.setItem('siv_access_token', 'valid-token');
      sessionStorage.setItem('siv_token_expires_at', futureExpiresAt);
      expect(service.getStoredToken()).toBe('valid-token');
    });
  });
});
