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

@Injectable({ providedIn: 'root' })
export class EmailService {
  private apiUrl = `${environment.apiUrl}/EmailLogs`;

  constructor(private http: HttpClient) {}

  obtenerTodos(): Observable<EmailLog[]> {
    return this.http.get<EmailLog[]>(this.apiUrl);
  }
}
