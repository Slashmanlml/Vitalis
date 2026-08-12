import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AuditoriaLog {
  id: number;
  usuarioEmail: string | null;
  accion: string;
  tabla: string;
  clavePrimaria: string;
  fecha: string;
  valoresAnteriores: string | null;
  valoresNuevos: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuditoriaService {
  private apiUrl = `${environment.apiUrl}/auditorias`;

  constructor(private http: HttpClient) {}

  obtenerTodas(): Observable<AuditoriaLog[]> {
    return this.http.get<AuditoriaLog[]>(this.apiUrl);
  }
}
