import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UsuarioPerfil {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  rol: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  usuario: UsuarioPerfil;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { email, password });
  }

  obtenerPerfil(): Observable<UsuarioPerfil> {
    return this.http.get<UsuarioPerfil>(`${this.apiUrl}/me`);
  }

  cambiarPassword(passwordActual: string, passwordNuevo: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/me/password`, { passwordActual, passwordNuevo });
  }

  logout(): void {
    localStorage.removeItem('token');
  }
}
