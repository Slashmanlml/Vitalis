import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Prestacion } from '../models/prestacion.model';

@Injectable({ providedIn: 'root' })
export class PrestacionService {
  private apiUrl = `${environment.apiUrl}/prestaciones`;

  constructor(private http: HttpClient) {}

  obtenerTodas(): Observable<Prestacion[]> {
    return this.http.get<Prestacion[]>(this.apiUrl);
  }

  crear(dto: any): Observable<Prestacion> {
    return this.http.post<Prestacion>(this.apiUrl, dto);
  }

  editar(id: number, dto: any): Observable<Prestacion> {
    return this.http.put<Prestacion>(`${this.apiUrl}/${id}`, dto);
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
