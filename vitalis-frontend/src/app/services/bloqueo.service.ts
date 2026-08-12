import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface BloqueoAgenda {
  id: number;
  profesionalId: number;
  profesionalNombre: string;
  fechaHoraInicio: string;
  fechaHoraFin: string;
  motivo: string;
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

  crear(dto: CrearBloqueo): Observable<BloqueoAgenda> {
    return this.http.post<BloqueoAgenda>(this.apiUrl, dto);
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
