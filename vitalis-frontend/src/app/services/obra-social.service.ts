import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ObraSocial, CrearObraSocial, EditarObraSocial } from '../models/obra-social.model';

@Injectable({ providedIn: 'root' })
export class ObraSocialService {
  private apiUrl = `${environment.apiUrl}/obrassociales`;

  constructor(private http: HttpClient) {}

  obtenerTodas(): Observable<ObraSocial[]> {
    return this.http.get<ObraSocial[]>(this.apiUrl);
  }

  crear(dto: CrearObraSocial): Observable<ObraSocial> {
    return this.http.post<ObraSocial>(this.apiUrl, dto);
  }

  editar(id: number, dto: EditarObraSocial): Observable<ObraSocial> {
    return this.http.put<ObraSocial>(`${this.apiUrl}/${id}`, dto);
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
