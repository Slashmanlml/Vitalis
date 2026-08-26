import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface FacturacionPorObraSocialItem {
  obraSocialId: number | null;
  obraSocialNombre: string;
  totalFacturado: number;
  cantidadFacturas: number;
  porcentajeDelTotal: number;
}

export interface ReporteFacturacionPorPeriodo {
  periodoDesde: string;
  periodoHasta: string;
  totalFacturado: number;
  cantidadFacturas: number;
  promedioPorFactura: number;
  porObraSocial: FacturacionPorObraSocialItem[];
}

export interface CobranzaPorMedioPagoItem {
  medioPago: string;
  totalCobrado: number;
  cantidadPagos: number;
  porcentajeDelTotal: number;
}

export interface ReporteCobranzas {
  periodoDesde: string;
  periodoHasta: string;
  totalFacturado: number;
  totalCobrado: number;
  saldoPendiente: number;
  tasaCobranzaPorcentaje: number;
  cantidadPagos: number;
  porMedioPago: CobranzaPorMedioPagoItem[];
}

export interface LiquidacionProfesionalItem {
  profesionalId: number;
  profesionalNombre: string;
  especialidad: string;
  totalLiquidado: number;
  cantidadLiquidaciones: number;
  estado: string;
  porcentajeDelTotal: number;
}

export interface ReporteLiquidacionesPorPeriodo {
  periodoDesde: string;
  periodoHasta: string;
  totalLiquidado: number;
  cantidadLiquidaciones: number;
  porProfesional: LiquidacionProfesionalItem[];
}

export interface ResumenFinanciero {
  periodoDesde: string;
  periodoHasta: string;
  totalFacturado: number;
  totalCobrado: number;
  saldoPendiente: number;
  totalLiquidado: number;
  margenBruto: number;
  tasaCobranzaPorcentaje: number;
  topObrasSociales: FacturacionPorObraSocialItem[];
  mediosPago: CobranzaPorMedioPagoItem[];
  topLiquidacionesProfesionales: LiquidacionProfesionalItem[];
}

@Injectable({ providedIn: 'root' })
export class ReporteFacturacionService {
  private apiUrl = `${environment.apiUrl}/ReportesFacturacion`;

  constructor(private http: HttpClient) {}

  obtenerResumenFinanciero(desde?: string, hasta?: string): Observable<ResumenFinanciero> {
    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);
    return this.http.get<ResumenFinanciero>(`${this.apiUrl}/resumen-financiero`, { params });
  }

  obtenerFacturacion(desde?: string, hasta?: string): Observable<ReporteFacturacionPorPeriodo> {
    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);
    return this.http.get<ReporteFacturacionPorPeriodo>(`${this.apiUrl}/facturacion`, { params });
  }

  obtenerCobranzas(desde?: string, hasta?: string): Observable<ReporteCobranzas> {
    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);
    return this.http.get<ReporteCobranzas>(`${this.apiUrl}/cobranzas`, { params });
  }

  obtenerLiquidaciones(desde?: string, hasta?: string): Observable<ReporteLiquidacionesPorPeriodo> {
    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);
    return this.http.get<ReporteLiquidacionesPorPeriodo>(`${this.apiUrl}/liquidaciones`, { params });
  }
}
