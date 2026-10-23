import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, map, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '@env';
import { CurrentUserResponse, LoginRequest, LoginResponse, UserSession } from './auth.models';

const TOKEN_KEY = 'siv_access_token';
const EXPIRES_AT_KEY = 'siv_token_expires_at';
const USER_ID_KEY = 'siv_user_id';
const ROLES_KEY = 'siv_user_roles';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  /**
   * Reactive signal holding current authenticated session or null.
   */
  public readonly session = signal<UserSession | null>(null);

  /**
   * Computed signals for reactive consumer components and guards.
   */
  public readonly currentUser = computed(() => this.session());
  public readonly isAuthenticated = computed(() => {
    const s = this.session();
    return !!s && !this.isExpired(s.expiresAtUtc);
  });
  public readonly isAdmin = computed(() => {
    return this.session()?.roles.includes('Admin') ?? false;
  });

  /**
   * Authenticates user against backend `/auth/login`, retrieves profile via `/auth/me`,
   * stores credentials in sessionStorage, and updates reactive state.
   */
  public login(request: LoginRequest): Observable<UserSession> {
    const loginUrl = `${environment.authUrl}/login`;
    const meUrl = `${environment.authUrl}/me`;

    return this.http.post<LoginResponse>(loginUrl, request).pipe(
      switchMap((loginRes) => {
        const headers = new HttpHeaders({
          Authorization: `Bearer ${loginRes.accessToken}`
        });

        return this.http.get<CurrentUserResponse>(meUrl, { headers }).pipe(
          map((meRes) => {
            const userSession: UserSession = {
              userId: meRes.userId,
              roles: meRes.roles,
              accessToken: loginRes.accessToken,
              expiresAtUtc: loginRes.expiresAtUtc
            };

            this.persistSession(userSession);
            this.session.set(userSession);
            return userSession;
          })
        );
      })
    );
  }

  /**
   * Terminates active session, clears sessionStorage, and navigates to login.
   */
  public logout(redirect: boolean = true): void {
    this.clearStorage();
    this.session.set(null);

    if (redirect) {
      this.router.navigate(['/login']);
    }
  }

  /**
   * Restores and validates session on application startup through `/auth/me`.
   * Returns Observable<boolean> indicating whether session was restored.
   */
  public initializeSession(): Observable<boolean> {
    const token = this.getStoredToken();
    const expiresAt = this.getStoredItem(EXPIRES_AT_KEY);

    if (!token || !expiresAt || this.isExpired(expiresAt)) {
      this.clearStorage();
      this.session.set(null);
      return of(false);
    }

    const meUrl = `${environment.authUrl}/me`;
    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
    });

    return this.http.get<CurrentUserResponse>(meUrl, { headers }).pipe(
      map((meRes) => {
        const userSession: UserSession = {
          userId: meRes.userId,
          roles: meRes.roles,
          accessToken: token,
          expiresAtUtc: expiresAt
        };

        this.persistSession(userSession);
        this.session.set(userSession);
        return true;
      }),
      catchError(() => {
        this.clearStorage();
        this.session.set(null);
        return of(false);
      })
    );
  }

  /**
   * Retrieves active access token from sessionStorage if present and non-expired.
   */
  public getStoredToken(): string | null {
    const token = this.getStoredItem(TOKEN_KEY);
    const expiresAt = this.getStoredItem(EXPIRES_AT_KEY);

    if (!token || !expiresAt) {
      return null;
    }

    if (this.isExpired(expiresAt)) {
      this.clearStorage();
      this.session.set(null);
      return null;
    }

    return token;
  }

  /**
   * Validates whether a given ISO/UTC date string is in the past.
   */
  public isExpired(expiresAtUtc: string): boolean {
    const expiryTime = new Date(expiresAtUtc).getTime();
    if (isNaN(expiryTime)) {
      return true;
    }
    return expiryTime <= Date.now();
  }

  private persistSession(session: UserSession): void {
    if (typeof window !== 'undefined' && window.sessionStorage) {
      window.sessionStorage.setItem(TOKEN_KEY, session.accessToken);
      window.sessionStorage.setItem(EXPIRES_AT_KEY, session.expiresAtUtc);
      window.sessionStorage.setItem(USER_ID_KEY, session.userId);
      window.sessionStorage.setItem(ROLES_KEY, JSON.stringify(session.roles));
    }
  }

  private clearStorage(): void {
    if (typeof window !== 'undefined' && window.sessionStorage) {
      window.sessionStorage.removeItem(TOKEN_KEY);
      window.sessionStorage.removeItem(EXPIRES_AT_KEY);
      window.sessionStorage.removeItem(USER_ID_KEY);
      window.sessionStorage.removeItem(ROLES_KEY);
    }
  }

  private getStoredItem(key: string): string | null {
    if (typeof window !== 'undefined' && window.sessionStorage) {
      return window.sessionStorage.getItem(key);
    }
    return null;
  }
}
