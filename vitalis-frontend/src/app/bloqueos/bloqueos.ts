import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BloqueoService, BloqueoAgenda } from '../services/bloqueo.service';
import { ProfesionalService } from '../services/profesional.service';
import { Profesional } from '../models/profesional.model';
import { ToastService } from '../services/toast.service';

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

  constructor(
    private bloqueoService: BloqueoService,
    private profesionalService: ProfesionalService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.detectUserRole();
    this.cargarDatos();
  }

  detectUserRole() {
    const token = localStorage.getItem('token');
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        this.userRol = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.Rol || 'Administrador';
        const userEmail = payload.email || '';
        
        if (this.userRol === 'Medico') {
          this.profesionalService.obtenerTodos().subscribe(docs => {
            const doc = docs.find(d => d.email.toLowerCase() === userEmail.toLowerCase());
            if (doc) {
              this.userProfId = doc.id;
              this.form.profesionalId = doc.id;
              this.cargarBloqueos();
            }
          });
        }
      } catch (e) {
        console.error('Error decodificando token en bloqueos:', e);
      }
    }
  }

  cargarDatos() {
    this.profesionalService.obtenerTodos().subscribe(docs => {
      this.profesionales = docs;
    });

    if (this.userRol !== 'Medico') {
      this.cargarBloqueos();
    }
  }

  cargarBloqueos() {
    this.cargando = true;
    if (this.userProfId) {
      this.bloqueoService.obtenerPorProfesional(this.userProfId).subscribe({
        next: (data) => {
          this.bloqueos = data;
          this.cargando = false;
        },
        error: () => {
          this.toastService.error('Error al cargar bloqueos');
          this.cargando = false;
        }
      });
    } else {
      this.bloqueoService.obtenerTodos().subscribe({
        next: (data) => {
          this.bloqueos = data;
          this.cargando = false;
        },
        error: () => {
          this.toastService.error('Error al cargar bloqueos');
          this.cargando = false;
        }
      });
    }
  }

  guardar() {
    if (!this.form.profesionalId || !this.form.fechaHoraInicio || !this.form.fechaHoraFin || !this.form.motivo) {
      this.toastService.error('Por favor, complete todos los campos');
      return;
    }

    const inicio = new Date(this.form.fechaHoraInicio);
    const fin = new Date(this.form.fechaHoraFin);

    if (inicio >= fin) {
      this.toastService.error('La fecha de inicio debe ser anterior a la de fin');
      return;
    }

    if (inicio < new Date()) {
      this.toastService.error('No se pueden crear bloqueos en el pasado');
      return;
    }

    const dto = {
      profesionalId: Number(this.form.profesionalId),
      fechaHoraInicio: inicio.toISOString(),
      fechaHoraFin: fin.toISOString(),
      motivo: this.form.motivo
    };

    this.bloqueoService.crear(dto).subscribe({
      next: () => {
        this.toastService.success('Agenda bloqueada exitosamente. Se cancelaron y notificaron los turnos afectados.');
        this.cargarBloqueos();
        this.form.fechaHoraInicio = '';
        this.form.fechaHoraFin = '';
        this.form.motivo = '';
      },
      error: (err) => {
        this.toastService.error(err.error?.mensaje || 'Error al guardar el bloqueo de agenda');
      }
    });
  }

  eliminar(id: number) {
    if (confirm('¿Está seguro de eliminar este bloqueo? Los horarios volverán a estar disponibles para turnos.')) {
      this.bloqueoService.eliminar(id).subscribe({
        next: () => {
          this.toastService.success('Bloqueo eliminado exitosamente');
          this.cargarBloqueos();
        },
        error: () => {
          this.toastService.error('Error al eliminar bloqueo');
        }
      });
    }
  }
}
