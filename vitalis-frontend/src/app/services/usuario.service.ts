import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Usuario, CrearUsuario, EditarUsuario } from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private apiUrl = `${environment.apiUrl}/usuarios`;

  constructor(private http: HttpClient) {}

  obtenerTodos(buscar?: string): Observable<Usuario[]> {
    const params = buscar ? { buscar } : undefined;
    return this.http.get<Usuario[]>(this.apiUrl, { params });
  }

  obtenerPorId(id: number): Observable<Usuario> {
    return this.http.get<Usuario>(`${this.apiUrl}/${id}`);
  }

  crear(dto: CrearUsuario): Observable<Usuario> {
    return this.http.post<Usuario>(this.apiUrl, dto);
  }

  editar(id: number, dto: EditarUsuario): Observable<Usuario> {
    return this.http.put<Usuario>(`${this.apiUrl}/${id}`, dto);
  }

  desactivar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}