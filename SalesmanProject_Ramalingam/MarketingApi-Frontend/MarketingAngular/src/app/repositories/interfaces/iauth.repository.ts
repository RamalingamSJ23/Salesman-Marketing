import { Observable } from 'rxjs';
import { LoginRequest, LoginResponse } from '../../models';

export interface IAuthRepository {
  login(request: LoginRequest): Observable<LoginResponse>;
}
