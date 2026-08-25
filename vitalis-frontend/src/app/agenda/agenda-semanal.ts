import {
  Component, EventEmitter, Input, OnChanges, OnInit, Output, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Turno } from '../models/turno.model';
import { Profesional } from '../models/profesional.model';
import { BloqueoAgenda, BloqueoService } from '../services/bloqueo.service';

/** Slot vacío sobre el que el usuario hizo click para dar un turno nuevo. */
export interface SlotLibre {
  fechaHora: Date;
  profesionalId: number | null;
}

interface Columna {
  /** En modo semana es el índice del día; en modo día, el id del profesional. */
  id: number;
  titulo: string;
  subtitulo: string;
  fecha: Date;
  profesionalId: number | null;
}

interface Franja {
  etiqueta: string;
  enPunto: boolean;
  hora: number;
  minuto: number;
}

interface Celda {
  columna: Columna;
  franja: Franja;
  inicio: Date;
  turnos: Turno[];
  bloqueo: BloqueoAgenda | null;
  pasado: boolean;
}

@Component({
  selector: 'app-agenda-semanal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agenda-semanal.html',
  styleUrls: ['./agenda-semanal.css']
})
export class AgendaSemanalComponent implements OnInit, OnChanges {
  /** Turnos ya cargados por el componente padre (evita pedirlos dos veces). */
  @Input() turnos: Turno[] = [];
  @Input() profesionales: Profesional[] = [];
  /** Si el usuario es Médico, la agenda se fija a su propia columna. */
  @Input() profesionalFijo: number | null = null;

  @Output() slotSeleccionado = new EventEmitter<SlotLibre>();
  @Output() turnoSeleccionado = new EventEmitter<Turno>();

  // La jornada replica las reglas de negocio ya validadas en el alta de turnos:
  // de lunes a viernes, de 08:00 a 20:00. Franjas de 30 minutos.
  readonly HORA_INICIO = 8;
  readonly HORA_FIN = 20;
  readonly MINUTOS_FRANJA = 30;

  private readonly MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun',
                            'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
  private readonly DIAS = ['Domingo', 'Lunes', 'Martes', 'Miércoles',
                           'Jueves', 'Viernes', 'Sábado'];

  modo: 'semana' | 'dia' = 'semana';
  ancla: Date = new Date();
  filtroProfesional: number | null = null;

  columnas: Columna[] = [];
  franjas: Franja[] = [];
  filas: Celda[][] = [];
  etiquetaRango = '';

  private bloqueos: BloqueoAgenda[] = [];

  constructor(private bloqueoService: BloqueoService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.construirFranjas();
    this.bloqueoService.obtenerTodos().subscribe({
      next: data => {
        this.bloqueos = data;
        this.construirGrilla();
        this.cdr.detectChanges();
      },
      // El ErrorInterceptor global ya notifica; la agenda sigue siendo utilizable
      // sin los bloqueos, solo que no los sombrea.
      error: err => console.error('No se pudieron cargar los bloqueos de agenda', err)
    });
  }

  ngOnChanges() {
    if (this.profesionalFijo) {
      this.filtroProfesional = this.profesionalFijo;
    }
    if (!this.franjas.length) {
      this.construirFranjas();
    }
    this.construirGrilla();
  }

  // ---------------------------------------------------------------- navegación

  cambiarModo(modo: 'semana' | 'dia') {
    this.modo = modo;
    this.construirGrilla();
  }

  mover(delta: number) {
    const d = new Date(this.ancla);
    d.setDate(d.getDate() + (this.modo === 'semana' ? 7 * delta : delta));
    // En modo día saltamos el fin de semana: la agenda no opera sábado ni domingo.
    if (this.modo === 'dia') {
      while (d.getDay() === 0 || d.getDay() === 6) {
        d.setDate(d.getDate() + (delta >= 0 ? 1 : -1));
      }
    }
    this.ancla = d;
    this.construirGrilla();
  }

  irHoy() {
    const hoy = new Date();
    if (this.modo === 'dia') {
      while (hoy.getDay() === 0 || hoy.getDay() === 6) {
        hoy.setDate(hoy.getDate() + 1);
      }
    }
    this.ancla = hoy;
    this.construirGrilla();
  }

  // ---------------------------------------------------------------- construcción

  private construirFranjas() {
    const franjas: Franja[] = [];
    for (let h = this.HORA_INICIO; h < this.HORA_FIN; h++) {
      for (let m = 0; m < 60; m += this.MINUTOS_FRANJA) {
        franjas.push({
          etiqueta: this.dosDigitos(h) + ':' + this.dosDigitos(m),
          enPunto: m === 0,
          hora: h,
          minuto: m
        });
      }
    }
    this.franjas = franjas;
  }

  private lunesDe(f: Date): Date {
    const d = new Date(f.getFullYear(), f.getMonth(), f.getDate());
    const dow = d.getDay();
    d.setDate(d.getDate() + (dow === 0 ? -6 : 1 - dow));
    return d;
  }

  construirGrilla() {
    this.columnas = this.modo === 'semana' ? this.columnasDeSemana() : this.columnasDeDia();
    this.etiquetaRango = this.calcularEtiqueta();

    const ahora = new Date();
    this.filas = this.franjas.map(franja =>
      this.columnas.map(columna => {
        const inicio = new Date(columna.fecha);
        inicio.setHours(franja.hora, franja.minuto, 0, 0);
        const fin = new Date(inicio.getTime() + this.MINUTOS_FRANJA * 60000);
        return {
          columna,
          franja,
          inicio,
          turnos: this.turnosEn(columna, inicio, fin),
          bloqueo: this.bloqueoEn(columna, inicio, fin),
          pasado: fin.getTime() < ahora.getTime()
        };
      })
    );
    this.cdr.detectChanges();
  }

  private columnasDeSemana(): Columna[] {
    const lunes = this.lunesDe(this.ancla);
    const cols: Columna[] = [];
    for (let i = 0; i < 5; i++) {
      const fecha = new Date(lunes);
      fecha.setDate(lunes.getDate() + i);
      cols.push({
        id: i,
        titulo: this.DIAS[fecha.getDay()],
        subtitulo: fecha.getDate() + ' ' + this.MESES[fecha.getMonth()],
        fecha,
        profesionalId: this.filtroProfesional
      });
    }
    return cols;
  }

  private columnasDeDia(): Columna[] {
    const fecha = new Date(this.ancla.getFullYear(), this.ancla.getMonth(), this.ancla.getDate());
    const visibles = this.profesionalFijo
      ? this.profesionales.filter(p => p.id === this.profesionalFijo)
      : this.profesionales.filter(p => p.activo !== false);
    return visibles.map(p => ({
      id: p.id,
      titulo: p.apellido + ', ' + p.nombre,
      subtitulo: p.especialidadNombre,
      fecha,
      profesionalId: p.id
    }));
  }

  private turnosEn(columna: Columna, inicio: Date, fin: Date): Turno[] {
    return this.turnos.filter(t => {
      if (columna.profesionalId != null && t.profesionalId !== columna.profesionalId) {
        return false;
      }
      if (this.profesionalFijo && t.profesionalId !== this.profesionalFijo) {
        return false;
      }
      const f = new Date(t.fechaHora).getTime();
      return f >= inicio.getTime() && f < fin.getTime();
    });
  }

  private bloqueoEn(columna: Columna, inicio: Date, fin: Date): BloqueoAgenda | null {
    // Sin profesional definido (vista semanal "todos") no tiene sentido sombrear:
    // un bloqueo es de un profesional puntual, no de la clínica entera.
    if (columna.profesionalId == null) return null;
    return this.bloqueos.find(b =>
      b.profesionalId === columna.profesionalId &&
      new Date(b.fechaHoraInicio).getTime() < fin.getTime() &&
      new Date(b.fechaHoraFin).getTime() > inicio.getTime()
    ) || null;
  }

  private calcularEtiqueta(): string {
    if (this.modo === 'dia') {
      const d = this.ancla;
      return this.DIAS[d.getDay()] + ' ' + d.getDate() + ' de ' +
             this.MESES[d.getMonth()] + '. ' + d.getFullYear();
    }
    const lunes = this.lunesDe(this.ancla);
    const viernes = new Date(lunes);
    viernes.setDate(lunes.getDate() + 4);
    const mismoMes = lunes.getMonth() === viernes.getMonth();
    return lunes.getDate() + (mismoMes ? '' : ' ' + this.MESES[lunes.getMonth()]) +
           ' – ' + viernes.getDate() + ' ' + this.MESES[viernes.getMonth()] +
           '. ' + viernes.getFullYear();
  }

  // ---------------------------------------------------------------- interacción

  clickCelda(celda: Celda) {
    if (celda.bloqueo || celda.pasado || celda.turnos.length) return;
    const prof = celda.columna.profesionalId ?? this.profesionalFijo ?? null;
    this.slotSeleccionado.emit({ fechaHora: celda.inicio, profesionalId: prof });
  }

  clickTurno(turno: Turno, evento: MouseEvent) {
    evento.stopPropagation();
    this.turnoSeleccionado.emit(turno);
  }

  // ---------------------------------------------------------------- presentación

  claseTurno(t: Turno): string {
    if (t.estado === 'Cancelado') return 'turno-cancelado';
    if (t.estado === 'Atendido') return 'turno-atendido';
    if (t.estado === 'En atención') return 'turno-en-atencion';
    return t.confirmado ? 'turno-confirmado' : 'turno-pendiente';
  }

  horaDe(t: Turno): string {
    const d = new Date(t.fechaHora);
    return this.dosDigitos(d.getHours()) + ':' + this.dosDigitos(d.getMinutes());
  }

  esHoy(columna: Columna): boolean {
    const hoy = new Date();
    return columna.fecha.getDate() === hoy.getDate() &&
           columna.fecha.getMonth() === hoy.getMonth() &&
           columna.fecha.getFullYear() === hoy.getFullYear();
  }

  /** Cuántos turnos activos tiene la columna: da lectura de carga de trabajo. */
  cargaDe(columna: Columna): number {
    return this.turnos.filter(t => {
      if (columna.profesionalId != null && t.profesionalId !== columna.profesionalId) return false;
      if (this.profesionalFijo && t.profesionalId !== this.profesionalFijo) return false;
      if (t.estado === 'Cancelado') return false;
      const f = new Date(t.fechaHora);
      return f.getDate() === columna.fecha.getDate() &&
             f.getMonth() === columna.fecha.getMonth() &&
             f.getFullYear() === columna.fecha.getFullYear();
    }).length;
  }

  private dosDigitos(n: number): string {
    return n < 10 ? '0' + n : '' + n;
  }

  trackFila = (i: number) => i;
  trackCelda = (i: number, c: Celda) => c.columna.id + '|' + c.franja.etiqueta;
}
