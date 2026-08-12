export interface Especialidad {
  id: number;
  nombre: string;
  descripcion?: string;
}

export interface CrearEspecialidad {
  nombre: string;
  descripcion?: string;
}

export interface EditarEspecialidad {
  nombre: string;
  descripcion?: string;
}
