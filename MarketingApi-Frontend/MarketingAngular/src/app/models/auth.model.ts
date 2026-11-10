export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  id?: number;
  token?: string;
  username: string;
  name: string;
  role?: string;
  rolename?: string;
  roleid?: number;
}

export interface UserSession {
  id?: number;
  username: string;
  name: string;
  role: string;
  roleid?: number;
}
