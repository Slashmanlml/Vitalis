import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SearchItem {
  id: number;
  tipo: string;
  titulo: string;
  subtitulo: string;
  ruta: string;
}

export interface SearchResults {
  pacientes: SearchItem[];
  profesionales: SearchItem[];
  turnos: SearchItem[];
}

@Injectable({ providedIn: 'root' })
export class SearchService {
  private apiUrl = `${environment.apiUrl}/search`;

  constructor(private http: HttpClient) {}

  buscar(q: string): Observable<SearchResults> {
    return this.http.get<SearchResults>(`${this.apiUrl}?q=${encodeURIComponent(q)}`);
  }
}
