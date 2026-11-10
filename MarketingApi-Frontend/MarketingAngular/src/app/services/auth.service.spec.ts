import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { APP_ROLES } from '../shared/constants';

function buildJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.signature`;
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('rejects login when server returns no token (C6)', () => {
    let thrownMessage = '';
    service.login({ username: 'sup', password: 'pw' }).subscribe({
      error: (err: Error) => {
        thrownMessage = err.message;
      },
    });

    const req = httpMock.expectOne('/api/Users/login');
    req.flush({ username: 'sup', name: 'Supervisor', token: '' });

    expect(thrownMessage).toContain('no token');
    expect(service.isAuthenticated()).toBe(false);
  });

  it('logs out when JWT is expired (C6)', () => {
    const expiredToken = buildJwt({
      unique_name: 'sup',
      role: APP_ROLES.SupervisorMaster,
      exp: Math.floor(Date.now() / 1000) - 60,
    });
    localStorage.setItem('marketing_api_token', expiredToken);

    expect(service.getToken()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('checks exact role names and returns false for empty role (H12)', () => {
    const validToken = buildJwt({
      unique_name: 'sup',
      role: APP_ROLES.SupervisorMaster,
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    service.saveSession({
      token: validToken,
      username: 'sup',
      name: 'Supervisor User',
      role: APP_ROLES.SupervisorMaster,
    });

    expect(service.hasRole('')).toBe(false);
    expect(service.hasRole('Supervisor')).toBe(false);
    expect(service.hasRole(APP_ROLES.SupervisorMaster)).toBe(true);
    expect(service.canAccessSalesman()).toBe(true);
    expect(service.canAccessMarketing()).toBe(false);
  });
});
