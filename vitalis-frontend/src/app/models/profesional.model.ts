export interface Profesional {
  id: number;
  nombre: string;
  apellido: string;
  matricula: string;
  email: string;
  especialidadId: number;
  especialidadNombre: string;
  fotoUrl?: string;
  activo: boolean;
}

export interface CrearProfesional {
  nombre: string;
  apellido: string;
  matricula: string;
  email: string;
  especialidadId: number;
  fotoUrl?: string;
}

export interface EditarProfesional {
  nombre: string;
  apellido: string;
  matricula: string;
  email: string;
  especialidadId: number;
  fotoUrl?: string;
}

