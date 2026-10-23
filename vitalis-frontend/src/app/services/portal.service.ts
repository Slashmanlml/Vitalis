import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MiPerfilPaciente {
  nombre: string;
  apellido: string;
  dni: string;
  fechaNacimiento: string;
  email?: string;
  telefono?: string;
  obraSocialNombre?: string;
  numeroAfiliado?: string;
}

export interface MiTurno {
  id: number;
  fechaHora: string;
  profesionalNombre: string;
  especialidad: string;
  estado: string;
  confirmado: boolean;
}

/** Portal del paciente. Ninguna llamada manda un id: el backend usa el token. */
@Injectable({ providedIn: 'root' })
export class PortalService {
  private apiUrl = `${environment.apiUrl}/portal`;

  constructor(private http: HttpClient) {}

  miPerfil(): Observable<MiPerfilPaciente> {
    return this.http.get<MiPerfilPaciente>(`${this.apiUrl}/mi-perfil`);
  }

  misTurnos(): Observable<MiTurno[]> {
    return this.http.get<MiTurno[]>(`${this.apiUrl}/mis-turnos`);
  }
}
