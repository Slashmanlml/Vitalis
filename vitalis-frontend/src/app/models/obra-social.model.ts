export interface ObraSocial {
  id: number;
  nombre: string;
  codigo: string;
  activa: boolean;
}

export interface CrearObraSocial {
  nombre: string;
  codigo: string;
}

export interface EditarObraSocial {
  nombre: string;
  codigo: string;
  activa: boolean;
}
