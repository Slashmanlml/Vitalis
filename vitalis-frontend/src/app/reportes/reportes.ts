import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReporteService, EstadisticasGenerales, ConteoPorCategoria } from '../services/reporte.service';
import { ReporteFacturacionService, ResumenFinanciero, FacturacionPorObraSocialItem, CobranzaPorMedioPagoItem, LiquidacionProfesionalItem } from '../services/reporte-facturacion.service';
import { ProfesionalService } from '../services/profesional.service';
import { PacienteService } from '../services/paciente.service';
import { ObraSocialService } from '../services/obra-social.service';
import { CsvExportService } from '../services/csv-export.service';
import { Profesional } from '../models/profesional.model';
import { Paciente } from '../models/paciente.model';
import { ObraSocial } from '../models/obra-social.model';
import { Turno } from '../models/turno.model';

export interface Barra {
  etiqueta: string;
  cantidad: number;
  porcentaje: number;
}

export interface BarraMonto {
  etiqueta: string;
  subetiqueta?: string;
  monto: number;
  porcentaje: number;
  cantidad?: number;
  badge?: string;
}

export interface PuntoSerie {
  etiqueta: string;
  etiquetaCorta: string;
  cantidad: number;
  x: number;
  y: number;
}

type Dimension = 'profesional' | 'paciente' | 'obraSocial';
type PestanaReporte = 'agenda' | 'financiero';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reportes.html',
  styleUrls: ['./reportes.css']
})
export class ReportesComponent implements OnInit {
  pestanaActiva: PestanaReporte = 'agenda';

  // --- Reporte Agenda (existente) ---
  estadisticas: EstadisticasGenerales | null = null;

  // Las barras se calculan UNA VEZ, al llegar los datos, y la plantilla itera
  // estas propiedades.
  //
  // Antes el HTML hacía *ngFor="let b of barras(...)". Angular ejecuta las
  // funciones de la plantilla en cada ciclo de detección de cambios, y esa
  // función devuelve un array NUEVO cada vez: Angular veía una lista distinta,
  // destruía el DOM de las barras y lo reconstruía; reconstruirlo disparaba otro
  // ciclo, que volvía a llamar la función. La pantalla de reportes se trababa.
  barrasEspecialidad: Barra[] = [];
  barrasObraSocial: Barra[] = [];
  barrasProfesional: Barra[] = [];
  cargando = true;

  profesionales: Profesional[] = [];
  pacientes: Paciente[] = [];
  obrasSociales: ObraSocial[] = [];

  dimension: Dimension = 'profesional';
  entidadId: number = 0;
  desde = '';
  hasta = '';
  detalle: Turno[] = [];
  consultaHecha = false;
  consultando = false;

  readonly ANCHO = 640;
  readonly ALTO = 200;
  readonly MARGEN = { arriba: 16, derecha: 16, abajo: 28, izquierda: 40 };
  serieMensual: PuntoSerie[] = [];
  pathLinea = '';
  pathArea = '';
  gridY: { y: number; valor: number }[] = [];
  puntoActivo: PuntoSerie | null = null;

  private readonly MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun',
                            'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

  // --- Reporte Financiero (nuevo) ---
  resumenFinanciero: ResumenFinanciero | null = null;
  barrasTopObrasSociales: BarraMonto[] = [];
  barrasTopMediosPago: BarraMonto[] = [];
  cargandoFinanciero = false;
  desdeFinanciero = '';
  hastaFinanciero = '';

  constructor(
    private reporteService: ReporteService,
    private reporteFacturacionService: ReporteFacturacionService,
    private profesionalService: ProfesionalService,
    private pacienteService: PacienteService,
    private obraSocialService: ObraSocialService,
    private csvExportService: CsvExportService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.inicializarFechasFinanciero();
    this.cargarEstadisticas();
    this.cargarReporteFinanciero();

    this.profesionalService.obtenerTodos().subscribe(d => {
      this.profesionales = d;
      this.cdr.detectChanges();
    });
    this.pacienteService.obtenerTodos().subscribe(d => {
      this.pacientes = d;
      this.cdr.detectChanges();
    });
    this.obraSocialService.obtenerTodas().subscribe(d => {
      this.obrasSociales = d;
      this.cdr.detectChanges();
    });
  }

  cambiarPestana(pestana: PestanaReporte) {
    this.pestanaActiva = pestana;
    if (pestana === 'financiero' && !this.resumenFinanciero && !this.cargandoFinanciero) {
      this.cargarReporteFinanciero();
    }
  }

  // ------------------------------------------------------------- Agenda / Turnos

  cargarEstadisticas() {
    this.cargando = true;
    this.reporteService.estadisticas().subscribe({
      next: data => {
        this.estadisticas = data;
        this.barrasEspecialidad = this.barras(data.porEspecialidad);
        this.barrasObraSocial = this.barras(data.porObraSocial);
        this.barrasProfesional = this.barras(data.porProfesional);
        this.construirSerieMensual(data.porMes);
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: err => {
        console.error('No se pudieron cargar las estadísticas', err);
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  barras(datos: ConteoPorCategoria[] | undefined, tope = 8): Barra[] {
    if (!datos || !datos.length) return [];
    const recortado = datos.slice(0, tope);
    const max = Math.max(...recortado.map(d => d.cantidad), 1);
    return recortado.map(d => ({
      etiqueta: d.etiqueta,
      cantidad: d.cantidad,
      porcentaje: Math.round((d.cantidad / max) * 100)
    }));
  }

  restantes(datos: ConteoPorCategoria[] | undefined, tope = 8): number {
    return !datos || datos.length <= tope ? 0 : datos.length - tope;
  }

  private construirSerieMensual(datos: ConteoPorCategoria[]) {
    this.serieMensual = [];
    this.pathLinea = '';
    this.pathArea = '';
    this.gridY = [];
    if (!datos || !datos.length) return;

    const max = Math.max(...datos.map(d => d.cantidad), 1);
    const pasoEje = this.pasoAgradable(max);
    const escalaMax = Math.ceil(max / pasoEje) * pasoEje;
    const x0 = this.MARGEN.izquierda;
    const x1 = this.ANCHO - this.MARGEN.derecha;
    const y0 = this.MARGEN.arriba;
    const y1 = this.ALTO - this.MARGEN.abajo;
    const paso = datos.length > 1 ? (x1 - x0) / (datos.length - 1) : 0;

    this.serieMensual = datos.map((d, i) => {
      const x = datos.length > 1 ? x0 + paso * i : (x0 + x1) / 2;
      const y = y1 - (d.cantidad / escalaMax) * (y1 - y0);
      return {
        etiqueta: d.etiqueta,
        etiquetaCorta: this.mesCorto(d.etiqueta),
        cantidad: d.cantidad,
        x: Math.round(x * 10) / 10,
        y: Math.round(y * 10) / 10
      };
    });

    this.pathLinea = this.serieMensual
      .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x},${p.y}`)
      .join(' ');
    const primero = this.serieMensual[0];
    const ultimo = this.serieMensual[this.serieMensual.length - 1];
    this.pathArea = `${this.pathLinea} L${ultimo.x},${y1} L${primero.x},${y1} Z`;

    for (let valor = 0; valor <= escalaMax; valor += pasoEje) {
      this.gridY.push({ y: y1 - (valor / escalaMax) * (y1 - y0), valor });
    }
  }

  private pasoAgradable(max: number, divisiones = 4): number {
    if (max <= 0) return 1;
    const bruto = max / divisiones;
    const magnitud = Math.pow(10, Math.floor(Math.log10(bruto)));
    const normalizado = bruto / magnitud;
    const paso = normalizado <= 1 ? 1 : normalizado <= 2 ? 2 : normalizado <= 5 ? 5 : 10;
    return Math.max(1, Math.round(paso * magnitud));
  }

  private mesCorto(etiqueta: string): string {
    const [anio, mes] = etiqueta.split('-');
    const i = Number(mes) - 1;
    return i >= 0 && i < 12 ? `${this.MESES[i]} ${anio.slice(2)}` : etiqueta;
  }

  moverSobreSerie(evento: MouseEvent, svg: Element) {
    if (!this.serieMensual.length) return;
    const caja = svg.getBoundingClientRect();
    const x = ((evento.clientX - caja.left) / caja.width) * this.ANCHO;
    let cercano = this.serieMensual[0];
    for (const p of this.serieMensual) {
      if (Math.abs(p.x - x) < Math.abs(cercano.x - x)) cercano = p;
    }
    this.puntoActivo = cercano;
  }

  salirDeSerie() {
    this.puntoActivo = null;
  }

  get entidades(): { id: number; nombre: string }[] {
    if (this.dimension === 'profesional') {
      return this.profesionales.map(p => ({ id: p.id, nombre: `${p.apellido}, ${p.nombre}` }));
    }
    if (this.dimension === 'paciente') {
      return this.pacientes.map(p => ({ id: p.id, nombre: `${p.apellido}, ${p.nombre}` }));
    }
    return this.obrasSociales.map(o => ({ id: o.id, nombre: o.nombre }));
  }

  cambiarDimension() {
    this.entidadId = 0;
    this.detalle = [];
    this.consultaHecha = false;
  }

  consultar() {
    if (!this.entidadId) return;
    this.consultando = true;
    this.consultaHecha = true;

    const listo = (data: Turno[]) => {
      this.detalle = data;
      this.consultando = false;
      this.cdr.detectChanges();
    };
    const falla = (err: unknown) => {
      console.error('No se pudo obtener el detalle del reporte', err);
      this.detalle = [];
      this.consultando = false;
      this.cdr.detectChanges();
    };

    if (this.dimension === 'profesional') {
      const desde = this.desde ? new Date(this.desde).toISOString() : undefined;
      const hasta = this.hasta
        ? new Date(new Date(this.hasta).setHours(23, 59, 59, 999)).toISOString()
        : undefined;
      this.reporteService.turnosPorProfesional(this.entidadId, desde, hasta)
        .subscribe({ next: listo, error: falla });
    } else if (this.dimension === 'paciente') {
      this.reporteService.turnosPorPaciente(this.entidadId)
        .subscribe({ next: listo, error: falla });
    } else {
      this.reporteService.turnosPorObraSocial(this.entidadId)
        .subscribe({ next: listo, error: falla });
    }
  }

  limpiar() {
    this.entidadId = 0;
    this.desde = '';
    this.hasta = '';
    this.detalle = [];
    this.consultaHecha = false;
  }

  get nombreEntidad(): string {
    return this.entidades.find(e => e.id === Number(this.entidadId))?.nombre ?? '';
  }

  claseEstado(t: Turno): string {
    if (t.estado === 'Cancelado') return 'estado-cancelado';
    if (t.estado === 'Atendido') return 'estado-atendido';
    return t.confirmado ? 'estado-confirmado' : 'estado-pendiente';
  }

  // -------------------------------------------------------- Reporte Financiero

  inicializarFechasFinanciero() {
    const ahora = new Date();
    const primerDiaMes = new Date(ahora.getFullYear(), ahora.getMonth(), 1);
    this.desdeFinanciero = primerDiaMes.toISOString().slice(0, 10);
    this.hastaFinanciero = ahora.toISOString().slice(0, 10);
  }

  cargarReporteFinanciero() {
    this.cargandoFinanciero = true;
    this.reporteFacturacionService.obtenerResumenFinanciero(
      this.desdeFinanciero || undefined,
      this.hastaFinanciero || undefined
    ).subscribe({
      next: (data) => {
        this.resumenFinanciero = data;
        this.barrasTopObrasSociales = this.barrasObrasSociales(data.topObrasSociales);
        this.barrasTopMediosPago = this.barrasMediosPago(data.mediosPago);
        this.cargandoFinanciero = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar reporte financiero:', err);
        this.cargandoFinanciero = false;
        this.cdr.detectChanges();
      }
    });
  }

  establecerMesActual() {
    this.inicializarFechasFinanciero();
    this.cargarReporteFinanciero();
  }

  establecerUltimos30Dias() {
    const ahora = new Date();
    const hace30 = new Date(ahora.getTime() - (30 * 24 * 60 * 60 * 1000));
    this.desdeFinanciero = hace30.toISOString().slice(0, 10);
    this.hastaFinanciero = ahora.toISOString().slice(0, 10);
    this.cargarReporteFinanciero();
  }

  barrasObrasSociales(items: FacturacionPorObraSocialItem[] | undefined): BarraMonto[] {
    if (!items || !items.length) return [];
    const max = Math.max(...items.map(i => i.totalFacturado), 1);
    return items.map(i => ({
      etiqueta: i.obraSocialNombre,
      monto: i.totalFacturado,
      cantidad: i.cantidadFacturas,
      porcentaje: Math.round((i.totalFacturado / max) * 100)
    }));
  }

  barrasMediosPago(items: CobranzaPorMedioPagoItem[] | undefined): BarraMonto[] {
    if (!items || !items.length) return [];
    const max = Math.max(...items.map(i => i.totalCobrado), 1);
    return items.map(i => ({
      etiqueta: i.medioPago,
      monto: i.totalCobrado,
      cantidad: i.cantidadPagos,
      porcentaje: Math.round((i.totalCobrado / max) * 100)
    }));
  }

  barrasLiquidaciones(items: LiquidacionProfesionalItem[] | undefined): BarraMonto[] {
    if (!items || !items.length) return [];
    const max = Math.max(...items.map(i => i.totalLiquidado), 1);
    return items.map(i => ({
      etiqueta: i.profesionalNombre,
      subetiqueta: i.especialidad,
      monto: i.totalLiquidado,
      cantidad: i.cantidadLiquidaciones,
      badge: i.estado,
      porcentaje: Math.round((i.totalLiquidado / max) * 100)
    }));
  }

  // ------------------------------------------------------------- Exportaciones

  exportarDetalle() {
    if (!this.detalle.length) return;
    const filas = this.detalle.map(t => ({
      'Fecha': new Date(t.fechaHora).toLocaleDateString('es-AR'),
      'Hora': new Date(t.fechaHora).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' }),
      'Paciente': t.pacienteNombre,
      'Profesional': t.profesionalNombre,
      'Obra Social': t.obraSocialNombre,
      'Estado': t.estado
    }));
    this.csvExportService.exportToCSV(filas, `reporte_${this.dimension}_${this.marcaDeTiempo()}`);
  }

  exportarResumen() {
    if (!this.estadisticas) return;
    const e = this.estadisticas;
    const filas = [
      { Indicador: 'Total de turnos', Valor: e.totalTurnos },
      { Indicador: 'Confirmados', Valor: e.confirmados },
      { Indicador: 'Pendientes', Valor: e.pendientes },
      { Indicador: 'Atendidos', Valor: e.atendidos },
      { Indicador: 'Cancelados', Valor: e.cancelados },
      ...e.porEspecialidad.map(x => ({ Indicador: `Especialidad · ${x.etiqueta}`, Valor: x.cantidad })),
      ...e.porObraSocial.map(x => ({ Indicador: `Obra social · ${x.etiqueta}`, Valor: x.cantidad })),
      ...e.porProfesional.map(x => ({ Indicador: `Profesional · ${x.etiqueta}`, Valor: x.cantidad })),
      ...e.porMes.map(x => ({ Indicador: `Mes · ${x.etiqueta}`, Valor: x.cantidad }))
    ];
    this.csvExportService.exportToCSV(filas, `resumen_general_${this.marcaDeTiempo()}`);
  }

  exportarResumenFinanciero() {
    if (!this.resumenFinanciero) return;
    const r = this.resumenFinanciero;
    const filas = [
      { Indicador: 'Total Facturado', Monto: r.totalFacturado },
      { Indicador: 'Total Cobrado', Monto: r.totalCobrado },
      { Indicador: 'Saldo Pendiente de Cobro', Monto: r.saldoPendiente },
      { Indicador: 'Total Liquidado a Profesionales', Monto: r.totalLiquidado },
      { Indicador: 'Margen Bruto Estimado', Monto: r.margenBruto },
      { Indicador: 'Tasa de Cobranza (%)', Monto: `${r.tasaCobranzaPorcentaje}%` },
      ...r.topObrasSociales.map(x => ({ Indicador: `Obra Social · ${x.obraSocialNombre}`, Monto: x.totalFacturado })),
      ...r.mediosPago.map(x => ({ Indicador: `Medio de Pago · ${x.medioPago}`, Monto: x.totalCobrado })),
      ...r.topLiquidacionesProfesionales.map(x => ({ Indicador: `Liquidación · ${x.profesionalNombre} (${x.especialidad})`, Monto: x.totalLiquidado }))
    ];
    this.csvExportService.exportToCSV(filas, `reporte_financiero_${this.marcaDeTiempo()}`);
  }

  private marcaDeTiempo(): string {
    return new Date().toISOString().slice(0, 10);
  }
}
