import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, LoginResponse, UserSession } from '../models';
import { APP_ROLES } from '../shared/constants';

const TOKEN_STORAGE_KEY = 'marketing_api_token';
const NAME_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const NAME_ID_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

interface JwtPayload {
  sub?: string;
  exp?: number;
  role?: string;
  unique_name?: string;
  [NAME_CLAIM]?: string;
  [ROLE_CLAIM]?: string;
  [NAME_ID_CLAIM]?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/Users`;

  readonly currentUser = signal<UserSession | null>(this.readSessionFromStoredToken());

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.url}/login`, request).pipe(
      tap((res) => {
        if (!res?.token) {
          this.logout();
          throw new Error('Login failed: no token returned by the server.');
        }

        this.saveSession(res);
      })
    );
  }

  saveSession(res: LoginResponse): void {
    if (typeof window === 'undefined' || !window.localStorage) {
      return;
    }

    if (!res.token) {
      throw new Error('No auth token received for session.');
    }

    localStorage.setItem(TOKEN_STORAGE_KEY, res.token);
    const parsed = this.parseToken(res.token);
    if (!parsed) {
      this.logout();
      throw new Error('Received invalid authentication token.');
    }

    this.currentUser.set({
      ...parsed.user,
      name: res.name || parsed.user.username,
    });
  }

  getToken(): string | null {
    if (typeof window === 'undefined' || !window.localStorage) {
      return null;
    }

    const token = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!token || token === 'undefined' || token === 'null') {
      return null;
    }

    const parsed = this.parseToken(token);
    if (!parsed || (parsed.expiresAtMs !== null && Date.now() >= parsed.expiresAtMs)) {
      this.logout();
      return null;
    }

    return token;
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    return !!token && !!this.currentUser();
  }

  logout(): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }
    this.currentUser.set(null);
  }

  hasRole(role: string): boolean {
    const target = role?.trim();
    const userRole = this.currentUser()?.role?.trim();
    if (!target || !userRole) {
      return false;
    }
    return userRole === target;
  }

  canAccessSalesman(): boolean {
    return this.hasRole(APP_ROLES.Admin) || this.hasRole(APP_ROLES.SupervisorMaster);
  }

  canAccessMarketing(): boolean {
    return this.hasRole(APP_ROLES.Admin) || this.hasRole(APP_ROLES.MarketingMaster);
  }

  getDefaultRoute(): string {
    if (!this.isAuthenticated()) {
      return '/login';
    }
    if (this.canAccessMarketing()) {
      return '/marketing-team';
    }
    if (this.canAccessSalesman()) {
      return '/salesman';
    }
    return '/login';
  }

  private readSessionFromStoredToken(): UserSession | null {
    if (typeof window === 'undefined' || !window.localStorage) {
      return null;
    }

    const token = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!token) {
      return null;
    }

    const parsed = this.parseToken(token);
    if (!parsed || (parsed.expiresAtMs !== null && Date.now() >= parsed.expiresAtMs)) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      return null;
    }

    return parsed.user;
  }

  private parseToken(token: string): { user: UserSession; expiresAtMs: number | null } | null {
    try {
      const payloadSegment = token.split('.')[1];
      if (!payloadSegment) {
        return null;
      }

      const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
      const payload = JSON.parse(atob(padded)) as JwtPayload;

      const username = payload[NAME_CLAIM] || payload.unique_name || '';
      const role = payload[ROLE_CLAIM] || payload.role || '';
      const idRaw = payload[NAME_ID_CLAIM] || payload.sub;
      const id = idRaw ? Number(idRaw) : undefined;
      const expiresAtMs = typeof payload.exp === 'number' ? payload.exp * 1000 : null;

      if (!username) {
        return null;
      }

      return {
        user: {
          id: Number.isFinite(id) ? id : undefined,
          username,
          name: username,
          role,
        },
        expiresAtMs,
      };
    } catch {
      return null;
    }
  }
}
