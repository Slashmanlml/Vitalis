import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BloqueoService, BloqueoAgenda, ImpactoBloqueo } from '../services/bloqueo.service';
import { ProfesionalService } from '../services/profesional.service';
import { Profesional } from '../models/profesional.model';
import { ToastService } from '../services/toast.service';
import { decodeToken, obtenerRolUsuario, obtenerEmailUsuario } from '../utils/jwt.util';

@Component({
  selector: 'app-bloqueos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bloqueos.html',
  styleUrls: ['./bloqueos.css']
})
export class BloqueosComponent implements OnInit {
  bloqueos: BloqueoAgenda[] = [];
  profesionales: Profesional[] = [];
  userRol: string = '';
  userProfId: number | null = null;

  form = {
    profesionalId: 0,
    fechaHoraInicio: '',
    fechaHoraFin: '',
    motivo: ''
  };

  cargando = false;

  /** Turnos que se perderían con el bloqueo que se está por crear. */
  impacto: ImpactoBloqueo | null = null;
  consultandoImpacto = false;
  guardando = false;

  constructor(
    private bloqueoService: BloqueoService,
    private profesionalService: ProfesionalService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // Un único pedido de profesionales: antes se pedía dos veces, una acá y otra
    // dentro de la detección de rol.
    this.profesionalService.obtenerTodos().subscribe(docs => {
      this.profesionales = docs;
      this.aplicarRol(docs);
      this.cargarBloqueos();
      this.cdr.detectChanges();
    });
  }

  /**
   * Antes este componente decodificaba el JWT a mano con atob(), duplicando lo
   * que ya hace jwt.util.ts (que además es lo que usa el guard de rutas).
   */
  private aplicarRol(profesionales: Profesional[]) {
    const token = localStorage.getItem('token');
    if (!token) return;

    const claims = decodeToken(token);
    if (!claims) return;

    this.userRol = obtenerRolUsuario(claims);
    const email = obtenerEmailUsuario(claims);

    if (this.userRol === 'Medico' && email) {
      const doc = profesionales.find(d => (d.email || '').toLowerCase() === email.toLowerCase());
      if (doc) {
        this.userProfId = doc.id;
        this.form.profesionalId = doc.id;
      }
    }
  }

  cargarBloqueos() {
    this.cargando = true;
    const peticion = this.userProfId
      ? this.bloqueoService.obtenerPorProfesional(this.userProfId)
      : this.bloqueoService.obtenerTodos();

    peticion.subscribe({
      next: data => {
        this.bloqueos = data;
        this.cargando = false;
        this.cdr.detectChanges();
      },
      // El ErrorInterceptor global ya muestra el toast con el mensaje del backend.
      error: err => {
        console.error('Error al cargar bloqueos', err);
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ------------------------------------------------------- vigentes / pasados

  get bloqueosVigentes(): BloqueoAgenda[] {
    const ahora = new Date();
    return this.bloqueos.filter(b => new Date(b.fechaHoraFin) >= ahora);
  }

  get bloqueosPasados(): BloqueoAgenda[] {
    const ahora = new Date();
    return this.bloqueos.filter(b => new Date(b.fechaHoraFin) < ahora);
  }

  estaEnCurso(b: BloqueoAgenda): boolean {
    const ahora = new Date().getTime();
    return new Date(b.fechaHoraInicio).getTime() <= ahora
        && new Date(b.fechaHoraFin).getTime() >= ahora;
  }

  // ---------------------------------------------------------------- impacto

  get formCompleto(): boolean {
    return !!this.form.profesionalId && !!this.form.fechaHoraInicio
        && !!this.form.fechaHoraFin && !!this.form.motivo.trim();
  }

  /** Se dispara al cambiar profesional o fechas: invalida la previsualización. */
  invalidarImpacto() {
    this.impacto = null;
  }

  private rangoValido(): { inicio: Date; fin: Date } | null {
    const inicio = new Date(this.form.fechaHoraInicio);
    const fin = new Date(this.form.fechaHoraFin);

    if (inicio >= fin) {
      this.toastService.error('La fecha de inicio debe ser anterior a la de fin');
      return null;
    }
    if (inicio < new Date()) {
      this.toastService.error('No se pueden crear bloqueos en el pasado');
      return null;
    }
    return { inicio, fin };
  }

  verificarImpacto() {
    if (!this.formCompleto) {
      this.toastService.error('Complete profesional, fechas y motivo');
      return;
    }
    const rango = this.rangoValido();
    if (!rango) return;

    this.consultandoImpacto = true;
    this.bloqueoService
      .obtenerImpacto(Number(this.form.profesionalId), rango.inicio.toISOString(), rango.fin.toISOString())
      .subscribe({
        next: data => {
          this.impacto = data;
          this.consultandoImpacto = false;
          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error al consultar el impacto del bloqueo', err);
          this.consultandoImpacto = false;
          this.cdr.detectChanges();
        }
      });
  }

  // ---------------------------------------------------------------- guardar

  confirmar() {
    const rango = this.rangoValido();
    if (!rango) return;

    this.guardando = true;
    this.bloqueoService.crear({
      profesionalId: Number(this.form.profesionalId),
      fechaHoraInicio: rango.inicio.toISOString(),
      fechaHoraFin: rango.fin.toISOString(),
      motivo: this.form.motivo
    }).subscribe({
      next: () => {
        const n = this.impacto?.cantidadTurnos ?? 0;
        this.toastService.success(
          n > 0
            ? `Agenda bloqueada. Se cancelaron ${n} turno${n === 1 ? '' : 's'} y se notificó a los pacientes.`
            : 'Agenda bloqueada. No había turnos en ese rango.'
        );
        this.limpiarFormulario();
        this.cargarBloqueos();
        this.guardando = false;
      },
      error: err => {
        console.error('Error al guardar el bloqueo', err);
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  limpiarFormulario() {
    this.form.fechaHoraInicio = '';
    this.form.fechaHoraFin = '';
    this.form.motivo = '';
    this.impacto = null;
  }

  eliminar(id: number) {
    // Se avisa explícitamente que los turnos ya cancelados no vuelven: el
    // bloqueo libera el horario, pero las cancelaciones y los avisos enviados
    // no se deshacen.
    const mensaje = '¿Eliminar este bloqueo?\n\n' +
      'El horario vuelve a estar disponible, pero los turnos que se cancelaron ' +
      'al crearlo NO se restauran ni se avisa a los pacientes.';
    if (!confirm(mensaje)) return;

    this.bloqueoService.eliminar(id).subscribe({
      next: () => {
        this.toastService.success('Bloqueo eliminado');
        this.cargarBloqueos();
      },
      error: err => console.error('Error al eliminar el bloqueo', err)
    });
  }
}
