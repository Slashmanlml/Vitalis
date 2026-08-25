import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Turno } from '../models/turno.model';

/** Par etiqueta/cantidad; espeja ConteoPorCategoriaDto del backend. */
export interface ConteoPorCategoria {
  etiqueta: string;
  cantidad: number;
}

/** Espeja EstadisticasGeneralesDto del backend. */
export interface EstadisticasGenerales {
  totalTurnos: number;
  confirmados: number;
  pendientes: number;
  atendidos: number;
  cancelados: number;
  porEspecialidad: ConteoPorCategoria[];
  porObraSocial: ConteoPorCategoria[];
  porProfesional: ConteoPorCategoria[];
  porMes: ConteoPorCategoria[];
}

/**
 * Este servicio no existía: la pantalla de reportes se descargaba TODOS los
 * turnos, pacientes y profesionales y los agregaba en el navegador, mientras
 * ReportesController ya exponía cuatro endpoints con la lógica hecha y probada.
 * Además de duplicar lógica, eso no escala: con miles de turnos el navegador
 * termina bajando toda la base para contar filas.
 */
@Injectable({ providedIn: 'root' })
export class ReporteService {
  private apiUrl = `${environment.apiUrl}/Reportes`;

  constructor(private http: HttpClient) {}

  estadisticas(): Observable<EstadisticasGenerales> {
    return this.http.get<EstadisticasGenerales>(`${this.apiUrl}/Estadisticas`);
  }

  turnosPorProfesional(profesionalId: number, desde?: string, hasta?: string): Observable<Turno[]> {
    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);
    return this.http.get<Turno[]>(`${this.apiUrl}/TurnosPorProfesional/${profesionalId}`, { params });
  }

  turnosPorPaciente(pacienteId: number): Observable<Turno[]> {
    return this.http.get<Turno[]>(`${this.apiUrl}/TurnosPorPaciente/${pacienteId}`);
  }

  turnosPorObraSocial(obraSocialId: number): Observable<Turno[]> {
    return this.http.get<Turno[]>(`${this.apiUrl}/TurnosPorObraSocial/${obraSocialId}`);
  }
}
