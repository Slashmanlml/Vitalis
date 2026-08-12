import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConsultaMedica, CrearConsulta, EditarConsulta, Antecedente, CrearAntecedente, Alergia, CrearAlergia } from '../models/consulta.model';

@Injectable({ providedIn: 'root' })
export class ConsultaMedicaService {
  private apiUrl = `${environment.apiUrl}/consultasmedicas`;

  constructor(private http: HttpClient) {}

  obtenerPorPaciente(pacienteId: number): Observable<ConsultaMedica[]> {
    return this.http.get<ConsultaMedica[]>(`${this.apiUrl}/paciente/${pacienteId}`);
  }

  obtenerPorId(id: number): Observable<ConsultaMedica> {
    return this.http.get<ConsultaMedica>(`${this.apiUrl}/${id}`);
  }

  crear(dto: CrearConsulta): Observable<ConsultaMedica> {
    return this.http.post<ConsultaMedica>(this.apiUrl, dto);
  }

  editar(id: number, dto: EditarConsulta): Observable<ConsultaMedica> {
    return this.http.put<ConsultaMedica>(`${this.apiUrl}/${id}`, dto);
  }

  obtenerAntecedentes(pacienteId: number): Observable<Antecedente[]> {
    return this.http.get<Antecedente[]>(`${this.apiUrl}/antecedentes/${pacienteId}`);
  }

  crearAntecedente(dto: CrearAntecedente): Observable<Antecedente> {
    return this.http.post<Antecedente>(`${this.apiUrl}/antecedentes`, dto);
  }

  obtenerAlergias(pacienteId: number): Observable<Alergia[]> {
    return this.http.get<Alergia[]>(`${this.apiUrl}/alergias/${pacienteId}`);
  }

  crearAlergia(dto: CrearAlergia): Observable<Alergia> {
    return this.http.post<Alergia>(`${this.apiUrl}/alergias`, dto);
  }
}
