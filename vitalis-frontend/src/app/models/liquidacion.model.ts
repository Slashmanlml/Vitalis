export interface Liquidacion {
  id: number;
  profesionalId: number;
  profesionalNombre: string;
  periodoDesde: string;
  periodoHasta: string;
  total: number;
  estado: string;
  fechaCreacion: string;
}

export interface CrearLiquidacion {
  profesionalId: number;
  periodoDesde: string;
  periodoHasta: string;
}
