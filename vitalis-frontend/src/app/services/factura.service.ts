import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Factura, CrearFactura, RegistrarPago } from '../models/factura.model';

@Injectable({ providedIn: 'root' })
export class FacturaService {
  private apiUrl = `${environment.apiUrl}/facturas`;

  constructor(private http: HttpClient) {}

  obtenerTodas(): Observable<Factura[]> {
    return this.http.get<Factura[]>(this.apiUrl);
  }

  obtenerPorPaciente(pacienteId: number): Observable<Factura[]> {
    return this.http.get<Factura[]>(`${this.apiUrl}/paciente/${pacienteId}`);
  }

  obtenerPorId(id: number): Observable<Factura> {
    return this.http.get<Factura>(`${this.apiUrl}/${id}`);
  }

  crear(dto: CrearFactura): Observable<Factura> {
    return this.http.post<Factura>(this.apiUrl, dto);
  }

  registrarPago(dto: RegistrarPago): Observable<Factura> {
    return this.http.post<Factura>(`${this.apiUrl}/pago`, dto);
  }
}
