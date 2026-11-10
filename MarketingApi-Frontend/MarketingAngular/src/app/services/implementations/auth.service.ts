import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { LoginRequest, LoginResponse, UserSession } from '../../models';
import { IAuthService } from '../interfaces/iauth.service';
import { AuthRepository } from '../../repositories/implementations/auth.repository';

@Injectable({ providedIn: 'root' })
export class AuthService implements IAuthService {
  private readonly repository = inject(AuthRepository);

  private readonly tokenKey = 'marketing_api_token';
  private readonly userKey = 'marketing_api_user';

  readonly currentUser = signal<UserSession | null>(this.getStoredUser());

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.repository.login(request).pipe(
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
    if (typeof window !== 'undefined' && window.localStorage) {
      const token = res.token;
      if (!token) {
        throw new Error('No auth token received for session.');
      }

      localStorage.setItem(this.tokenKey, token);
      const role = res.role || res.rolename || '';
      const user: UserSession = {
        id: res.id,
        username: res.username,
        name: res.name || res.username,
        role: role,
        roleid: res.roleid,
      };
      localStorage.setItem(this.userKey, JSON.stringify(user));
      this.currentUser.set(user);
    }
  }

  getToken(): string | null {
    if (typeof window === 'undefined' || !window.localStorage) {
      return null;
    }

    const token = localStorage.getItem(this.tokenKey);
    if (!token || token === 'undefined' || token === 'null') {
      return null;
    }

    const expiration = this.getTokenExpiration(token);
    if (expiration && Date.now() >= expiration) {
      this.logout();
      return null;
    }

    return token;
  }

  private getTokenExpiration(token: string): number | null {
    try {
      const payload = token.split('.')[1];
      if (!payload) {
        return null;
      }

      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
      const decoded = JSON.parse(atob(padded));
      const exp = decoded?.exp;
      return typeof exp === 'number' ? exp * 1000 : null;
    } catch {
      return null;
    }
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    const user = this.currentUser();
    return !!token && !!user;
  }

  logout(): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.removeItem(this.tokenKey);
      localStorage.removeItem(this.userKey);
      this.currentUser.set(null);
    }
  }

  hasRole(role: string): boolean {
    const user = this.currentUser();
    if (!user) return false;
    const r = (user.role || '').toLowerCase();
    const target = (role || '').toLowerCase();
    return r.includes(target) || target.includes(r);
  }

  canAccessSalesman(): boolean {
    const user = this.currentUser();
    if (!user) return false;
    const r = (user.role || '').toLowerCase();
    return r.includes('admin') || r.includes('supervis');
  }

  canAccessMarketing(): boolean {
    const user = this.currentUser();
    if (!user) return false;
    const r = (user.role || '').toLowerCase();
    return r.includes('admin') || r.includes('market') || r.includes('manag');
  }

  getDefaultRoute(): string {
    const user = this.currentUser();
    if (!user) return '/login';
    const role = (user.role || '').toLowerCase();
    if (role.includes('market') || role.includes('manag')) {
      return '/marketing-team';
    }
    return '/salesman';
  }

  private getStoredUser(): UserSession | null {
    if (typeof window === 'undefined' || !window.localStorage) {
      return null;
    }
    const raw = localStorage.getItem(this.userKey);
    if (!raw || raw === 'undefined' || raw === 'null') return null;
    try {
      const parsed = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object') return null;
      return parsed as UserSession;
    } catch {
      return null;
    }
  }
}
