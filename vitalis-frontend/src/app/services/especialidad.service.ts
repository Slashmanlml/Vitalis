import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Especialidad, CrearEspecialidad, EditarEspecialidad } from '../models/especialidad.model';

@Injectable({ providedIn: 'root' })
export class EspecialidadService {
  private apiUrl = `${environment.apiUrl}/especialidades`;

  constructor(private http: HttpClient) {}

  obtenerTodas(): Observable<Especialidad[]> {
    return this.http.get<Especialidad[]>(this.apiUrl);
  }

  crear(dto: CrearEspecialidad): Observable<Especialidad> {
    return this.http.post<Especialidad>(this.apiUrl, dto);
  }

  editar(id: number, dto: EditarEspecialidad): Observable<Especialidad> {
    return this.http.put<Especialidad>(`${this.apiUrl}/${id}`, dto);
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
