import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface EmailLog {
  id: number;
  destinatario: string;
  asunto: string;
  cuerpo: string;
  fechaEnvio: string;
  origen: string;
  evento: string;
  turnoId?: number;
  estado: string;
  mensajeError?: string;
}

export interface SimularEmailDto {
  destinatario: string;
  tipoNotificacion: string;
  asunto?: string;
  cuerpo?: string;
}

@Injectable({ providedIn: 'root' })
export class EmailService {
  private apiUrl = `${environment.apiUrl}/EmailLogs`;

  constructor(private http: HttpClient) {}

  obtenerTodos(origen?: string, evento?: string, estado?: string): Observable<EmailLog[]> {
    let params = new HttpParams();
    if (origen) params = params.set('origen', origen);
    if (evento) params = params.set('evento', evento);
    if (estado) params = params.set('estado', estado);
    return this.http.get<EmailLog[]>(this.apiUrl, { params });
  }

  simularEnvio(dto: SimularEmailDto): Observable<EmailLog> {
    return this.http.post<EmailLog>(`${this.apiUrl}/simular`, dto);
  }

  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
