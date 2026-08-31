import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PacienteService } from '../services/paciente.service';
import { TurnoService } from '../services/turno.service';
import { ConsultaMedicaService } from '../services/consulta-medica.service';
import { PrescripcionService } from '../services/prescripcion.service';
import { ObraSocialService } from '../services/obra-social.service';
import { UploadService } from '../services/upload.service';
import { ToastService } from '../services/toast.service';
import { Paciente, EditarPaciente } from '../models/paciente.model';
import { Turno } from '../models/turno.model';
import { ConsultaMedica, Antecedente, Alergia } from '../models/consulta.model';
import { Prescripcion } from '../models/prescripcion.model';
import { ObraSocial } from '../models/obra-social.model';
import { rolActual } from '../utils/permisos';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-paciente-ficha',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './paciente-ficha.html',
  styleUrl: './paciente-ficha.css'
})
export class PacienteFichaComponent implements OnInit {
  serverUrl = environment.serverUrl;
  pacienteId: number = 0;
  paciente: Paciente | null = null;
  cargando: boolean = true;
  tabActiva: 'datos' | 'turnos' | 'historia' | 'recetas' = 'datos';

  turnos: Turno[] = [];
  consultas: ConsultaMedica[] = [];
  antecedentes: Antecedente[] = [];
  alergias: Alergia[] = [];
  prescripciones: Prescripcion[] = [];
  obrasSociales: ObraSocial[] = [];

  showEditModal: boolean = false;
  touched: { [key: string]: boolean } = {};
  editForm: EditarPaciente = {
    nombre: '',
    apellido: '',
    telefono: '',
    email: '',
    direccion: '',
    obraSocialId: undefined,
    numeroAfiliado: '',
    fotoUrl: ''
  };

  constructor(
    private ruta: ActivatedRoute,
    private router: Router,
    private pacienteService: PacienteService,
    private turnoService: TurnoService,
    private consultaMedicaService: ConsultaMedicaService,
    private prescripcionService: PrescripcionService,
    private obraSocialService: ObraSocialService,
    private uploadService: UploadService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  get esMedico(): boolean {
    return rolActual() === 'Medico';
  }

  ngOnInit(): void {
    this.pacienteId = Number(this.ruta.snapshot.paramMap.get('id')) || 0;
    if (this.pacienteId > 0) {
      this.cargarPaciente();
      this.cargarTurnos();
      this.cargarObrasSociales();
      if (this.esMedico) {
        this.cargarHistoriaClinica();
        this.cargarPrescripciones();
      }
    } else {
      this.cargando = false;
    }
  }

  cargarPaciente(): void {
    this.cargando = true;
    this.pacienteService.obtenerPorId(this.pacienteId).subscribe({
      next: (data) => {
        this.paciente = data;
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar datos del paciente', err);
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarTurnos(): void {
    this.turnoService.obtenerTodos().subscribe({
      next: (data) => {
        this.turnos = data
          .filter(t => t.pacienteId === this.pacienteId)
          .sort((a, b) => new Date(b.fechaHora).getTime() - new Date(a.fechaHora).getTime());
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar turnos del paciente', err);
      }
    });
  }

  cargarObrasSociales(): void {
    this.obraSocialService.obtenerTodas().subscribe({
      next: (data) => {
        this.obrasSociales = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar obras sociales', err);
      }
    });
  }

  cargarHistoriaClinica(): void {
    if (!this.esMedico) return;

    this.consultaMedicaService.obtenerPorPaciente(this.pacienteId).subscribe({
      next: (data) => {
        this.consultas = data.sort((a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime());
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar consultas médicas', err);
      }
    });

    this.consultaMedicaService.obtenerAntecedentes(this.pacienteId).subscribe({
      next: (data) => {
        this.antecedentes = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar antecedentes', err);
      }
    });

    this.consultaMedicaService.obtenerAlergias(this.pacienteId).subscribe({
      next: (data) => {
        this.alergias = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar alergias', err);
      }
    });
  }

  cargarPrescripciones(): void {
    if (!this.esMedico) return;

    this.prescripcionService.obtenerPorPaciente(this.pacienteId).subscribe({
      next: (data) => {
        this.prescripciones = data.sort((a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime());
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar recetas del paciente', err);
      }
    });
  }

  calcularEdad(fechaNacimiento?: string): string {
    if (!fechaNacimiento) return 'N/D';
    const cumple = new Date(fechaNacimiento);
    const hoy = new Date();
    let edad = hoy.getFullYear() - cumple.getFullYear();
    const mes = hoy.getMonth() - cumple.getMonth();
    if (mes < 0 || (mes === 0 && hoy.getDate() < cumple.getDate())) {
      edad--;
    }
    return edad >= 0 ? `${edad} años` : 'N/D';
  }

  obtenerClaseEstadoTurno(estado?: string): string {
    switch (estado?.toLowerCase()) {
      case 'atendido':
      case 'confirmado':
        return 'status-success';
      case 'solicitado':
      case 'en espera':
      case 'en atencion':
        return 'status-warning';
      case 'cancelado':
      case 'ausente':
        return 'status-danger';
      default:
        return 'status-info';
    }
  }

  cambiarTab(tab: 'datos' | 'turnos' | 'historia' | 'recetas'): void {
    if ((tab === 'historia' || tab === 'recetas') && !this.esMedico) {
      return;
    }
    this.tabActiva = tab;
  }

  abrirModalEditar(): void {
    if (!this.paciente) return;
    this.editForm = {
      nombre: this.paciente.nombre,
      apellido: this.paciente.apellido,
      telefono: this.paciente.telefono || '',
      email: this.paciente.email || '',
      direccion: this.paciente.direccion || '',
      obraSocialId: this.paciente.obraSocialId,
      numeroAfiliado: this.paciente.numeroAfiliado || '',
      fotoUrl: this.paciente.fotoUrl || ''
    };
    this.touched = {};
    this.showEditModal = true;
  }

  cerrarModalEditar(): void {
    this.showEditModal = false;
  }

  markFieldTouched(field: string): void {
    this.touched[field] = true;
  }

  isFieldInvalid(field: string): boolean {
    return !!this.touched[field] && !this.isFieldValid(field);
  }

  isFieldValid(field: string): boolean {
    switch (field) {
      case 'nombre': return !!this.editForm.nombre && this.editForm.nombre.trim().length >= 2;
      case 'apellido': return !!this.editForm.apellido && this.editForm.apellido.trim().length >= 2;
      case 'email': return !this.editForm.email || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.editForm.email);
      case 'telefono': {
        if (!this.editForm.telefono) return true;
        const validChars = /^[+0-9\s()-]*$/.test(this.editForm.telefono);
        if (!validChars) return false;
        const cleanTel = this.editForm.telefono.replace(/[^\d]/g, '');
        return cleanTel.length >= 6 && cleanTel.length <= 15;
      }
      default: return true;
    }
  }

  isFormValid(): boolean {
    return this.isFieldValid('nombre') &&
           this.isFieldValid('apellido') &&
           this.isFieldValid('email') &&
           this.isFieldValid('telefono');
  }

  getFieldError(field: string): string {
    if (!this.touched[field]) return '';
    switch (field) {
      case 'nombre':
        if (!this.editForm.nombre) return 'El nombre es requerido';
        if (this.editForm.nombre.trim().length < 2) return 'El nombre debe tener al menos 2 caracteres';
        return '';
      case 'apellido':
        if (!this.editForm.apellido) return 'El apellido es requerido';
        if (this.editForm.apellido.trim().length < 2) return 'El apellido debe tener al menos 2 caracteres';
        return '';
      case 'email':
        if (this.editForm.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.editForm.email)) return 'Email inválido';
        return '';
      case 'telefono': {
        if (this.editForm.telefono) {
          const validChars = /^[+0-9\s()-]*$/.test(this.editForm.telefono);
          if (!validChars) return 'El teléfono contiene caracteres no permitidos';
          const cleanTel = this.editForm.telefono.replace(/[^\d]/g, '');
          if (cleanTel.length < 6 || cleanTel.length > 15) return 'El teléfono debe tener entre 6 y 15 dígitos';
        }
        return '';
      }
      default: return '';
    }
  }

  guardarEdicion(): void {
    ['nombre', 'apellido', 'email', 'telefono'].forEach(f => this.touched[f] = true);
    if (!this.isFormValid()) return;

    this.pacienteService.editar(this.pacienteId, this.editForm).subscribe({
      next: () => {
        this.toastService.success('Datos del paciente actualizados con éxito');
        this.showEditModal = false;
        this.cargarPaciente();
      },
      error: (err) => {
        console.error('Error al guardar datos del paciente', err);
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      this.uploadService.subirImagen(file).subscribe({
        next: (res) => {
          this.editForm.fotoUrl = res.url;
          this.toastService.success('Imagen de perfil subida correctamente');
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error al subir imagen de perfil', err);
        }
      });
    }
  }

  volver(): void {
    this.router.navigate(['/dashboard/pacientes']);
  }
}
