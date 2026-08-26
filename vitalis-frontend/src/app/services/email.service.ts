import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface EmailLog {
  id: number;
  destinatario: string;
  asunto: string;
  cuerpo: string;
  fechaEnvio: string;
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

  obtenerTodos(): Observable<EmailLog[]> {
    return this.http.get<EmailLog[]>(this.apiUrl);
  }

  simularEnvio(dto: SimularEmailDto): Observable<EmailLog> {
    return this.http.post<EmailLog>(`${this.apiUrl}/simular`, dto);
  }

  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  limpiar(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/limpiar`);
  }
}
