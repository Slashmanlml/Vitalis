import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Medicamento, CrearMedicamento, EditarMedicamento } from '../models/medicamento.model';

@Injectable({ providedIn: 'root' })
export class MedicamentoService {
  private apiUrl = `${environment.apiUrl}/medicamentos`;

  constructor(private http: HttpClient) {}

  obtenerTodos(buscar?: string): Observable<Medicamento[]> {
    let params = new HttpParams();
    if (buscar) params = params.set('buscar', buscar);
    return this.http.get<Medicamento[]>(this.apiUrl, { params });
  }

  crear(dto: CrearMedicamento): Observable<Medicamento> {
    return this.http.post<Medicamento>(this.apiUrl, dto);
  }

  editar(id: number, dto: EditarMedicamento): Observable<Medicamento> {
    return this.http.put<Medicamento>(`${this.apiUrl}/${id}`, dto);
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
