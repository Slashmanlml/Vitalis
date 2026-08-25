import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReporteService, EstadisticasGenerales, ConteoPorCategoria } from '../services/reporte.service';
import { ProfesionalService } from '../services/profesional.service';
import { PacienteService } from '../services/paciente.service';
import { ObraSocialService } from '../services/obra-social.service';
import { CsvExportService } from '../services/csv-export.service';
import { Profesional } from '../models/profesional.model';
import { Paciente } from '../models/paciente.model';
import { ObraSocial } from '../models/obra-social.model';
import { Turno } from '../models/turno.model';

/** Una barra ya resuelta para pintar: ancho en % del máximo de su grupo. */
export interface Barra {
  etiqueta: string;
  cantidad: number;
  porcentaje: number;
}

/** Un punto de la serie mensual, con sus coordenadas ya en el sistema del SVG. */
export interface PuntoSerie {
  etiqueta: string;
  etiquetaCorta: string;
  cantidad: number;
  x: number;
  y: number;
}

type Dimension = 'profesional' | 'paciente' | 'obraSocial';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reportes.html',
  styleUrls: ['./reportes.css']
})
export class ReportesComponent implements OnInit {
  estadisticas: EstadisticasGenerales | null = null;
  cargando = true;

  profesionales: Profesional[] = [];
  pacientes: Paciente[] = [];
  obrasSociales: ObraSocial[] = [];

  // --- consulta de detalle ---
  dimension: Dimension = 'profesional';
  entidadId: number = 0;
  desde = '';
  hasta = '';
  detalle: Turno[] = [];
  consultaHecha = false;
  consultando = false;

  // --- geometría del gráfico de línea ---
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

  constructor(
    private reporteService: ReporteService,
    private profesionalService: ProfesionalService,
    private pacienteService: PacienteService,
    private obraSocialService: ObraSocialService,
    private csvExportService: CsvExportService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargarEstadisticas();
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

  cargarEstadisticas() {
    this.cargando = true;
    this.reporteService.estadisticas().subscribe({
      next: data => {
        this.estadisticas = data;
        this.construirSerieMensual(data.porMes);
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: err => {
        // El ErrorInterceptor global ya avisa al usuario.
        console.error('No se pudieron cargar las estadísticas', err);
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ------------------------------------------------------------------ barras

  /**
   * Las barras se pintan todas del mismo color: la longitud ya codifica la
   * magnitud, así que teñir cada barra de un color distinto gastaría el canal
   * de identidad en repetir información que la barra ya da.
   */
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

  /** Cuántas categorías quedaron fuera del tope, para declararlo en pantalla. */
  restantes(datos: ConteoPorCategoria[] | undefined, tope = 8): number {
    return !datos || datos.length <= tope ? 0 : datos.length - tope;
  }

  // ------------------------------------------------------------ serie mensual

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

  /**
   * Separación entre marcas del eje Y, tomada de la familia 1/2/5 x 10^n y
   * forzada a entero porque el eje cuenta turnos. Sin esto, un máximo de 68
   * producía marcas en 0-18-35-52-70: correcto pero ilegible.
   *
   * Se devuelve el paso (y no el techo) para que el techo se calcule como el
   * primer múltiplo por encima del máximo: así un máximo de 264 llega a 300 y
   * no a 400, sin desperdiciar un tercio de la altura del gráfico.
   */
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

  /** Resalta el punto más cercano al cursor, para el tooltip del gráfico. */
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

  // --------------------------------------------------------- consulta detalle

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
      // El "hasta" se lleva al final del día para que el rango sea inclusivo:
      // de lo contrario un turno de las 15:00 quedaría fuera de un filtro que
      // termina ese mismo día a las 00:00.
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

  // ------------------------------------------------------------- exportación

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

  private marcaDeTiempo(): string {
    return new Date().toISOString().slice(0, 10);
  }
}
