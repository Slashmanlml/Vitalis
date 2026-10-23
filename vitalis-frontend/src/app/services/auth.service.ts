import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SIN_TOAST_DE_ERROR } from '../interceptors/error.interceptor';

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
  expiresAt: string;
  usuarioId: number;
  email: string;
  nombreCompleto: string;
  rol: string;
}

export interface RegistroPaciente {
  nombre: string;
  apellido: string;
  dni: string;
  fechaNacimiento: string;
  email: string;
  telefono?: string;
  password: string;
  confirmarPassword: string;
  aceptaTerminos: boolean;
}

export interface Mensaje {
  mensaje: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;

  /** Pedidos de las pantallas públicas: el error se muestra en el formulario. */
  private publico = () => ({ context: new HttpContext().set(SIN_TOAST_DE_ERROR, true) });

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { email, password }, this.publico());
  }

  registrar(datos: RegistroPaciente): Observable<Mensaje> {
    return this.http.post<Mensaje>(`${this.apiUrl}/registro`, datos, this.publico());
  }

  confirmarEmail(token: string): Observable<Mensaje> {
    return this.http.post<Mensaje>(`${this.apiUrl}/confirmar-email`, { token }, this.publico());
  }

  reenviarConfirmacion(email: string): Observable<Mensaje> {
    return this.http.post<Mensaje>(`${this.apiUrl}/reenviar-confirmacion`, { email }, this.publico());
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
