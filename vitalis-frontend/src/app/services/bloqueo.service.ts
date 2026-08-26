import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface BloqueoAgenda {
  id: number;
  profesionalId: number;
  profesionalNombre: string;
  fechaHoraInicio: string;
  fechaHoraFin: string;
  motivo: string;
}

/** Turnos que un bloqueo dejaría cancelados. Espeja ImpactoBloqueoDto del backend. */
export interface ImpactoBloqueo {
  cantidadTurnos: number;
  pacientesAfectados: number;
  pacientesConEmail: number;
  turnos: TurnoAfectado[];
}

export interface TurnoAfectado {
  turnoId: number;
  fechaHora: string;
  pacienteNombre: string;
  estado: string;
  tieneEmail: boolean;
}

export interface CrearBloqueo {
  profesionalId: number;
  fechaHoraInicio: string;
  fechaHoraFin: string;
  motivo: string;
}

@Injectable({ providedIn: 'root' })
export class BloqueoService {
  private apiUrl = `${environment.apiUrl}/BloqueosAgenda`;

  constructor(private http: HttpClient) {}

  obtenerTodos(): Observable<BloqueoAgenda[]> {
    return this.http.get<BloqueoAgenda[]>(this.apiUrl);
  }

  obtenerPorProfesional(profesionalId: number): Observable<BloqueoAgenda[]> {
    return this.http.get<BloqueoAgenda[]>(`${this.apiUrl}/profesional/${profesionalId}`);
  }

  /**
   * Consulta qué turnos se cancelarían, sin aplicar nada. Se llama antes de
   * confirmar: crear el bloqueo cancela turnos y notifica pacientes, y eso no
   * se puede deshacer.
   */
  obtenerImpacto(profesionalId: number, desde: string, hasta: string): Observable<ImpactoBloqueo> {
    const params = new HttpParams()
      .set('profesionalId', profesionalId)
      .set('desde', desde)
      .set('hasta', hasta);
    return this.http.get<ImpactoBloqueo>(`${this.apiUrl}/impacto`, { params });
  }

  crear(dto: CrearBloqueo): Observable<BloqueoAgenda> {
    return this.http.post<BloqueoAgenda>(this.apiUrl, dto);
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
