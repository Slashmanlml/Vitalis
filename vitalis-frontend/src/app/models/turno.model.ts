export interface Turno {
  id: number;
  pacienteId: number;
  pacienteNombre: string;
  profesionalId: number;
  profesionalNombre: string;
  obraSocialId: number;
  obraSocialNombre: string;
  fechaHora: string;
  confirmado: boolean;
  estado: string;
}

export interface CrearTurno {
  pacienteId: number;
  profesionalId: number;
  obraSocialId: number;
  fechaHora: string;
}

export interface EditarTurno {
  pacienteId: number;
  profesionalId: number;
  obraSocialId: number;
  fechaHora: string;
  confirmado: boolean;
  estado?: string;
}
