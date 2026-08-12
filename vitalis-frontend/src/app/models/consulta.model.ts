export interface ConsultaMedica {
  id: number;
  pacienteId: number;
  pacienteNombre: string;
  profesionalId: number;
  profesionalNombre: string;
  turnoId: number;
  fecha: string;
  motivoConsulta: string;
  diagnostico?: string;
  evolucion?: string;
  indicaciones?: string;
  observaciones?: string;
  estudioAdjuntoUrl?: string;
}

export interface CrearConsulta {
  turnoId: number;
  pacienteId: number;
  profesionalId: number;
  motivoConsulta: string;
  diagnostico?: string;
  evolucion?: string;
  indicaciones?: string;
  observaciones?: string;
  estudioAdjuntoUrl?: string;
}

export interface EditarConsulta {
  motivoConsulta: string;
  diagnostico?: string;
  evolucion?: string;
  indicaciones?: string;
  observaciones?: string;
  estudioAdjuntoUrl?: string;
}

export interface Antecedente {
  id: number;
  pacienteId: number;
  tipo: string;
  descripcion: string;
  fechaRegistro: string;
}

export interface CrearAntecedente {
  pacienteId: number;
  tipo: string;
  descripcion: string;
}

export interface Alergia {
  id: number;
  pacienteId: number;
  sustancia: string;
  reaccion?: string;
  severidad?: string;
  activa: boolean;
}

export interface CrearAlergia {
  pacienteId: number;
  sustancia: string;
  reaccion?: string;
  severidad?: string;
}
