import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Liquidacion, CrearLiquidacion } from '../models/liquidacion.model';

@Injectable({ providedIn: 'root' })
export class LiquidacionService {
  private apiUrl = `${environment.apiUrl}/liquidaciones`;

  constructor(private http: HttpClient) {}

  obtenerTodas(): Observable<Liquidacion[]> {
    return this.http.get<Liquidacion[]>(this.apiUrl);
  }

  obtenerPorId(id: number): Observable<Liquidacion> {
    return this.http.get<Liquidacion>(`${this.apiUrl}/${id}`);
  }

  crear(dto: CrearLiquidacion): Observable<Liquidacion> {
    return this.http.post<Liquidacion>(this.apiUrl, dto);
  }

  liquidar(id: number): Observable<Liquidacion> {
    return this.http.post<Liquidacion>(`${this.apiUrl}/${id}/liquidar`, {});
  }
}
