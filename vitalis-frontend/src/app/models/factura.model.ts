export interface Factura {
  id: number;
  pacienteId: number;
  pacienteNombre: string;
  fecha: string;
  total: number;
  estado: string;
  observaciones?: string;
  detalles: FacturaDetalle[];
  pagos: Pago[];
}

export interface FacturaDetalle {
  id: number;
  prestacionId: number;
  prestacionNombre: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
}

export interface Pago {
  id: number;
  fecha: string;
  medioPago: string;
  importe: number;
  observaciones?: string;
}

export interface CrearFactura {
  pacienteId: number;
  observaciones?: string;
  detalles: CrearFacturaDetalle[];
}

export interface CrearFacturaDetalle {
  prestacionId: number;
  cantidad: number;
  precioUnitario: number;
}

export interface RegistrarPago {
  facturaId: number;
  medioPago: string;
  importe: number;
  observaciones?: string;
}
