export const ROLES_USUARIO = ['Administrador', 'Medico', 'Recepcionista', 'Facturacion'] as const;

export interface Usuario {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  rol: string;
  activo: boolean;
}

export interface CrearUsuario {
  nombre: string;
  apellido: string;
  email: string;
  password: string;
  rol: string;
}

export interface EditarUsuario {
  nombre?: string;
  apellido?: string;
  email?: string;
  rol?: string;
}