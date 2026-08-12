export interface Paciente {
  id: number;
  nombre: string;
  apellido: string;
  dni: string;
  fechaNacimiento: string;
  telefono?: string;
  email?: string;
  direccion?: string;
  obraSocialId?: number;
  obraSocialNombre?: string;
  numeroAfiliado?: string;
  fotoUrl?: string;
  activo: boolean;
}

export interface CrearPaciente {
  nombre: string;
  apellido: string;
  dni: string;
  fechaNacimiento: string;
  email?: string;
  telefono?: string;
  direccion?: string;
  obraSocialId?: number;
  numeroAfiliado?: string;
  fotoUrl?: string;
}

export interface EditarPaciente {
  nombre: string;
  apellido: string;
  telefono?: string;
  email?: string;
  direccion?: string;
  obraSocialId?: number;
  numeroAfiliado?: string;
  fotoUrl?: string;
}
