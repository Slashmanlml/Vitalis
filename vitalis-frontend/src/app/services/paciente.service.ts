import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Paciente, CrearPaciente, EditarPaciente } from '../models/paciente.model';

@Injectable({ providedIn: 'root' })
export class PacienteService {
  private apiUrl = `${environment.apiUrl}/pacientes`;

  constructor(private http: HttpClient) {}

  obtenerTodos(buscar?: string): Observable<Paciente[]> {
    let params = new HttpParams();
    if (buscar) params = params.set('buscar', buscar);
    return this.http.get<Paciente[]>(this.apiUrl, { params });
  }

  obtenerPorId(id: number): Observable<Paciente> {
    return this.http.get<Paciente>(`${this.apiUrl}/${id}`);
  }

  crear(dto: CrearPaciente): Observable<Paciente> {
    return this.http.post<Paciente>(this.apiUrl, dto);
  }

  editar(id: number, dto: EditarPaciente): Observable<Paciente> {
    return this.http.put<Paciente>(`${this.apiUrl}/${id}`, dto);
  }

  desactivar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
