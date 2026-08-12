import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Turno, CrearTurno, EditarTurno } from '../models/turno.model';

@Injectable({ providedIn: 'root' })
export class TurnoService {
  private apiUrl = `${environment.apiUrl}/turnos`;

  constructor(private http: HttpClient) {}

  obtenerTodos(): Observable<Turno[]> {
    return this.http.get<Turno[]>(this.apiUrl);
  }

  obtenerPorId(id: number): Observable<Turno> {
    return this.http.get<Turno>(`${this.apiUrl}/${id}`);
  }

  crear(dto: CrearTurno): Observable<Turno> {
    return this.http.post<Turno>(this.apiUrl, dto);
  }

  editar(id: number, dto: EditarTurno): Observable<Turno> {
    return this.http.put<Turno>(`${this.apiUrl}/${id}`, dto);
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
