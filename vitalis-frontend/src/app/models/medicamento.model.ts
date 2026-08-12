export interface Medicamento {
  id: number;
  nombre: string;
  presentacion?: string;
  activo: boolean;
}

export interface CrearMedicamento {
  nombre: string;
  presentacion?: string;
}

export interface EditarMedicamento {
  nombre: string;
  presentacion?: string;
  activo: boolean;
}
