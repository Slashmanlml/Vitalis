import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Profesional, CrearProfesional, EditarProfesional } from '../models/profesional.model';

@Injectable({ providedIn: 'root' })
export class ProfesionalService {
  private apiUrl = `${environment.apiUrl}/profesionales`;

  constructor(private http: HttpClient) {}

  obtenerTodos(): Observable<Profesional[]> {
    return this.http.get<Profesional[]>(this.apiUrl);
  }

  obtenerPorId(id: number): Observable<Profesional> {
    return this.http.get<Profesional>(`${this.apiUrl}/${id}`);
  }

  crear(dto: CrearProfesional): Observable<Profesional> {
    return this.http.post<Profesional>(this.apiUrl, dto);
  }

  editar(id: number, dto: EditarProfesional): Observable<Profesional> {
    return this.http.put<Profesional>(`${this.apiUrl}/${id}`, dto);
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
