import { Signal } from '@angular/core';
import { Observable } from 'rxjs';
import { LoginRequest, LoginResponse, UserSession } from '../../models';

export interface IAuthService {
  readonly currentUser: Signal<UserSession | null>;
  login(request: LoginRequest): Observable<LoginResponse>;
  saveSession(res: LoginResponse): void;
  getToken(): string | null;
  isAuthenticated(): boolean;
  logout(): void;
  hasRole(role: string): boolean;
  canAccessSalesman(): boolean;
  canAccessMarketing(): boolean;
  getDefaultRoute(): string;
}
