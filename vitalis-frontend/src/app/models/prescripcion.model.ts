export interface Prescripcion {
  id: number;
  consultaMedicaId: number;
  pacienteId: number;
  pacienteNombre: string;
  profesionalId: number;
  profesionalNombre: string;
  fecha: string;
  observaciones?: string;
  detalles: PrescripcionDetalle[];
}

export interface PrescripcionDetalle {
  id: number;
  medicamentoId: number;
  medicamentoNombre: string;
  dosis: string;
  frecuencia: string;
  duracion: string;
  indicaciones?: string;
}

export interface CrearPrescripcion {
  consultaMedicaId: number;
  pacienteId: number;
  profesionalId: number;
  observaciones?: string;
  detalles: CrearPrescripcionDetalle[];
}

export interface CrearPrescripcionDetalle {
  medicamentoId: number;
  dosis: string;
  frecuencia: string;
  duracion: string;
  indicaciones?: string;
}
