import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse } from '../../models';
import { IAuthRepository } from '../interfaces/iauth.repository';

@Injectable({ providedIn: 'root' })
export class AuthRepository implements IAuthRepository {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/users`;

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.url}/login`, request);
  }
}
