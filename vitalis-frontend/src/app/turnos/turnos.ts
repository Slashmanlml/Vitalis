import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TurnoService } from '../services/turno.service';
import { PacienteService } from '../services/paciente.service';
import { ProfesionalService } from '../services/profesional.service';
import { ObraSocialService } from '../services/obra-social.service';
import { Turno, CrearTurno, EditarTurno } from '../models/turno.model';
import { Paciente } from '../models/paciente.model';
import { Profesional } from '../models/profesional.model';
import { ObraSocial } from '../models/obra-social.model';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-turnos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './turnos.html',
  styleUrls: ['./turnos.css']
})
export class TurnosComponent implements OnInit {
  turnos: Turno[] = [];
  filteredTurnos: Turno[] = [];
  pacientes: Paciente[] = [];
  profesionales: Profesional[] = [];
  obrasSociales: ObraSocial[] = [];
  touched: { [key: string]: boolean } = {};

  searchTerm: string = '';
  filtroEstado: string = 'todos';
  showModal: boolean = false;
  editMode: boolean = false;
  selectedTurno: Turno | null = null;
  
  userRol: string = '';
  userEmail: string = '';
  userProfId: number | null = null;

  form: CrearTurno & { confirmado: boolean } = {
    pacienteId: 0, profesionalId: 0, obraSocialId: 0,
    fechaHora: '', confirmado: false
  };

  constructor(
    private turnoService: TurnoService,
    private pacienteService: PacienteService,
    private profesionalService: ProfesionalService,
    private obraSocialService: ObraSocialService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargarTurnos();
    this.pacienteService.obtenerTodos().subscribe(d => {
      this.pacientes = d;
      this.cdr.detectChanges();
    });
    this.profesionalService.obtenerTodos().subscribe(d => {
      this.profesionales = d;
      this.detectUserRole();
      this.cdr.detectChanges();
    });
    this.obraSocialService.obtenerTodas().subscribe(d => {
      this.obrasSociales = d;
      this.cdr.detectChanges();
    });
  }

  detectUserRole() {
    const token = localStorage.getItem('token');
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        this.userRol = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.Rol || 'Administrador';
        this.userEmail = payload.email || '';
        
        if (this.userRol === 'Medico') {
          const doc = this.profesionales.find(d => d.email.toLowerCase() === this.userEmail.toLowerCase());
          if (doc) {
            this.userProfId = doc.id;
            this.aplicarFiltros();
          }
        }
      } catch (e) {
        console.error('Error al decodificar token en turnos:', e);
      }
    }
  }

  cargarTurnos() {
    this.turnoService.obtenerTodos().subscribe(data => {
      this.turnos = data;
      this.aplicarFiltros();
    });
  }

  aplicarFiltros() {
    let result = this.turnos;
    
    if (this.userRol === 'Medico' && this.userProfId) {
      result = result.filter(t => t.profesionalId === this.userProfId);
    }

    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(t =>
        t.pacienteNombre.toLowerCase().includes(term) ||
        (this.userRol !== 'Medico' && t.profesionalNombre.toLowerCase().includes(term))
      );
    }
    if (this.filtroEstado === 'confirmados') result = result.filter(t => t.confirmado);
    else if (this.filtroEstado === 'pendientes') result = result.filter(t => !t.confirmado);
    this.filteredTurnos = result;
    this.cdr.detectChanges();
  }

  abrirNuevo() {
    this.editMode = false; this.selectedTurno = null;
    const now = new Date();
    const localDate = new Date(now.getTime() - now.getTimezoneOffset() * 60000);
    const local = localDate.toISOString().slice(0, 16);
    this.form = { 
      pacienteId: 0, 
      profesionalId: this.userRol === 'Medico' ? (this.userProfId || 0) : 0, 
      obraSocialId: 0, 
      fechaHora: local, 
      confirmado: false 
    };
    this.touched = {};
    this.showModal = true;
  }

  abrirEditar(t: Turno) {
    this.editMode = true; this.selectedTurno = t;
    const dt = new Date(t.fechaHora);
    const localDate = new Date(dt.getTime() - dt.getTimezoneOffset() * 60000);
    const local = localDate.toISOString().slice(0, 16);
    this.form = {
      pacienteId: t.pacienteId, profesionalId: t.profesionalId,
      obraSocialId: t.obraSocialId, fechaHora: local, confirmado: t.confirmado
    };
    this.touched = {};
    this.showModal = true;
  }

  isFieldInvalid(field: string): boolean {
    return !!this.touched[field] && !this.isFieldValid(field);
  }

  isFieldValid(field: string): boolean {
    switch (field) {
      case 'pacienteId': return !!this.form.pacienteId && this.form.pacienteId > 0;
      case 'profesionalId': return !!this.form.profesionalId && this.form.profesionalId > 0;
      case 'obraSocialId': return !!this.form.obraSocialId && this.form.obraSocialId > 0;
      case 'fechaHora': {
        if (!this.form.fechaHora) return false;
        
        const selectedDate = new Date(this.form.fechaHora);
        const day = selectedDate.getDay();
        if (day === 0 || day === 6) return false; // 0 Sunday, 6 Saturday
        
        const hour = selectedDate.getHours();
        if (hour < 8 || hour >= 20) return false;

        if (!this.editMode) {
          const now = new Date();
          return selectedDate.getTime() >= now.getTime() - 5 * 60 * 1000;
        }
        return true;
      }
      default: return true;
    }
  }

  markFieldTouched(field: string) {
    this.touched[field] = true;
  }

  isFormValid(): boolean {
    return this.isFieldValid('pacienteId') && 
           this.isFieldValid('profesionalId') && 
           this.isFieldValid('obraSocialId') &&
           this.isFieldValid('fechaHora');
  }

  getFieldError(field: string): string {
    if (!this.touched[field]) return '';
    
    switch (field) {
      case 'pacienteId': 
        if (!this.form.pacienteId || this.form.pacienteId === 0) return 'Selecciona un paciente';
        return '';
      case 'profesionalId':
        if (!this.form.profesionalId || this.form.profesionalId === 0) return 'Selecciona un profesional';
        return '';
      case 'obraSocialId':
        if (!this.form.obraSocialId || this.form.obraSocialId === 0) return 'Selecciona una obra social';
        return '';
      case 'fechaHora':
        if (!this.form.fechaHora) return 'La fecha y hora son requeridas';
        const selectedDate = new Date(this.form.fechaHora);
        const day = selectedDate.getDay();
        if (day === 0 || day === 6) {
          return 'No se pueden agendar turnos los fines de semana';
        }
        const hour = selectedDate.getHours();
        if (hour < 8 || hour >= 20) {
          return 'Los turnos deben ser entre las 08:00 y las 20:00';
        }
        if (!this.editMode) {
          const now = new Date();
          if (selectedDate.getTime() < now.getTime() - 5 * 60 * 1000) {
            return 'No se pueden programar turnos en el pasado';
          }
        }
        return '';
      default: return '';
    }
  }

  guardar() {
    ['pacienteId', 'profesionalId', 'obraSocialId', 'fechaHora'].forEach(field => {
      this.touched[field] = true;
    });

    if (!this.isFormValid()) {
      return;
    }

    const fechaHora = new Date(this.form.fechaHora).toISOString();
    if (this.editMode && this.selectedTurno) {
      const dto: EditarTurno = { ...this.form, fechaHora };
      this.turnoService.editar(this.selectedTurno.id, dto).subscribe({
        next: () => {
          this.toastService.success('Turno reprogramado exitosamente');
          this.cargarTurnos();
          this.showModal = false;
        },
        error: (err) => {
          this.toastService.error(err.error?.mensaje || 'Error al reprogramar el turno');
        }
      });
    } else {
      const dto: CrearTurno = { ...this.form, fechaHora };
      this.turnoService.crear(dto).subscribe({
        next: () => {
          this.toastService.success('Turno reservado exitosamente');
          this.cargarTurnos();
          this.showModal = false;
        },
        error: (err) => {
          this.toastService.error(err.error?.mensaje || 'Error al reservar el turno');
        }
      });
    }
  }

  confirmarTurno(t: Turno) {
    const dto: EditarTurno = {
      pacienteId: t.pacienteId, profesionalId: t.profesionalId,
      obraSocialId: t.obraSocialId, fechaHora: t.fechaHora, confirmado: true
    };
    this.turnoService.editar(t.id, dto).subscribe(() => {
      this.toastService.success('Turno confirmado exitosamente');
      this.cargarTurnos();
    });
  }

  cancelarTurno(t: Turno) {
    if (confirm(`¿Cancelar turno de ${t.pacienteNombre}?`)) {
      this.turnoService.eliminar(t.id).subscribe(() => {
        this.toastService.success('Turno cancelado exitosamente');
        this.cargarTurnos();
      });
    }
  }

  cerrarModal() { this.showModal = false; }
}
