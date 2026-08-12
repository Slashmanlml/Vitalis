import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Prescripcion, CrearPrescripcion } from '../models/prescripcion.model';

@Injectable({ providedIn: 'root' })
export class PrescripcionService {
  private apiUrl = `${environment.apiUrl}/prescripciones`;

  constructor(private http: HttpClient) {}

  obtenerPorPaciente(pacienteId: number): Observable<Prescripcion[]> {
    return this.http.get<Prescripcion[]>(`${this.apiUrl}/paciente/${pacienteId}`);
  }

  obtenerPorId(id: number): Observable<Prescripcion> {
    return this.http.get<Prescripcion>(`${this.apiUrl}/${id}`);
  }

  crear(dto: CrearPrescripcion): Observable<Prescripcion> {
    return this.http.post<Prescripcion>(this.apiUrl, dto);
  }
}
