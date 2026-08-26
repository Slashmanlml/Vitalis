import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { ConsultaMedicaService } from '../services/consulta-medica.service';
import { PacienteService } from '../services/paciente.service';
import { TurnoService } from '../services/turno.service';
import { Paciente } from '../models/paciente.model';
import { Turno } from '../models/turno.model';
import { ConsultaMedica, CrearConsulta, Antecedente, CrearAntecedente, Alergia, CrearAlergia } from '../models/consulta.model';
import { PrintService } from '../services/print.service';
import { ToastService } from '../services/toast.service';
import { UploadService } from '../services/upload.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-historia-clinica',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './historia-clinica.html',
  styleUrls: ['./historia-clinica.css']
})
export class HistoriaClinicaComponent implements OnInit, OnDestroy {
  serverUrl = environment.serverUrl;
  pacientes: Paciente[] = [];
  selectedPacienteId: number = 0;
  selectedPaciente: Paciente | null = null;

  consultas: ConsultaMedica[] = [];
  antecedentes: Antecedente[] = [];
  alergias: Alergia[] = [];
  turnosDisponibles: Turno[] = [];
  todosTurnos: Turno[] = [];

  showConsultaModal: boolean = false;
  showAntecedenteModal: boolean = false;
  showAlergiaModal: boolean = false;

  consultaForm: CrearConsulta = {
    turnoId: 0, pacienteId: 0, profesionalId: 0,
    motivoConsulta: '', diagnostico: '', evolucion: '',
    indicaciones: '', observaciones: '', estudioAdjuntoUrl: ''
  };

  antecedenteForm: CrearAntecedente = { pacienteId: 0, tipo: '', descripcion: '' };
  alergiaForm: CrearAlergia = { pacienteId: 0, sustancia: '', reaccion: '', severidad: '' };

  private destroy$ = new Subject<void>();

  constructor(
    private consultaService: ConsultaMedicaService,
    private pacienteService: PacienteService,
    private turnoService: TurnoService,
    private route: ActivatedRoute,
    private printService: PrintService,
    private toastService: ToastService,
    private uploadService: UploadService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    forkJoin({
      pacientes: this.pacienteService.obtenerTodos(),
      turnos: this.turnoService.obtenerTodos()
    }).pipe(takeUntil(this.destroy$)).subscribe(({ pacientes, turnos }) => {
      this.pacientes = pacientes;
      this.todosTurnos = turnos;

      this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
        if (params['pacienteId']) {
          this.seleccionarPaciente(Number(params['pacienteId']));
        }
      });
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  seleccionarPaciente(id: any) {
    const numericId = Number(id);
    this.selectedPacienteId = numericId;
    this.selectedPaciente = this.pacientes.find(p => p.id === numericId) || null;
    if (numericId) {
      this.consultaService.obtenerPorPaciente(numericId).subscribe({
        next: data => {
          this.consultas = data;
          this.cdr.detectChanges();
        },
        error: err => {
          // El ErrorInterceptor global ya muestra un toast con el mensaje del backend
          // para todo error HTTP -- este toastService.error() duplicado quedaba
          // mostrando dos notificaciones para el mismo error. Se deja el console.error
          // para debugging.
          console.error('Error fetching consultations', err);
        }
      });
      this.consultaService.obtenerAntecedentes(numericId).subscribe({
        next: data => {
          this.antecedentes = data;
          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error fetching antecedents', err);
        }
      });
      this.consultaService.obtenerAlergias(numericId).subscribe({
        next: data => {
          this.alergias = data;
          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error fetching allergies', err);
        }
      });
      const now = new Date();
      // Allow selecting any non-attended turn for this patient (relaxed for testing)
      this.turnosDisponibles = this.todosTurnos.filter(t => 
        t.pacienteId === numericId && 
        t.estado !== 'Atendido'
      );
    } else {
      this.consultas = [];
      this.antecedentes = [];
      this.alergias = [];
      this.turnosDisponibles = [];
    }
  }

  getProfesionalNombrePorTurno(turnoId: number): string {
    const t = this.turnosDisponibles.find(x => x.id === turnoId);
    return t ? t.profesionalNombre : '';
  }

  onTurnoChange() {
    const selectedTurno = this.turnosDisponibles.find(t => t.id === this.consultaForm.turnoId);
    if (selectedTurno) {
      this.consultaForm.profesionalId = selectedTurno.profesionalId;
    } else {
      this.consultaForm.profesionalId = 0;
    }
  }

  abrirNuevaConsulta() {
    this.consultaForm = {
      turnoId: 0, pacienteId: this.selectedPacienteId,
      profesionalId: 0, motivoConsulta: '', diagnostico: '',
      evolucion: '', indicaciones: '', observaciones: '', estudioAdjuntoUrl: ''
    };
    const now = new Date();
    this.turnosDisponibles = this.todosTurnos.filter(t => 
      t.pacienteId === this.selectedPacienteId && 
      t.estado !== 'Atendido'
    );
    this.showConsultaModal = true;
  }

  guardarConsulta() {
    if (!this.selectedPacienteId || this.selectedPacienteId === 0) {
      this.toastService.error('Debe seleccionar un paciente primero');
      return;
    }
    if (!this.consultaForm.turnoId || this.consultaForm.turnoId === 0) {
      this.toastService.error('Debe seleccionar un turno para registrar la consulta');
      return;
    }
    if (!this.consultaForm.motivoConsulta || this.consultaForm.motivoConsulta.trim() === '') {
      this.toastService.error('El motivo de la consulta es requerido');
      return;
    }

    const selectedTurno = this.turnosDisponibles.find(t => t.id === this.consultaForm.turnoId);
    if (!selectedTurno) {
      this.toastService.error('El turno seleccionado no es válido o está en el futuro');
      return;
    }

    this.consultaForm.profesionalId = selectedTurno.profesionalId;
    this.consultaForm.pacienteId = this.selectedPacienteId;

    this.consultaService.crear(this.consultaForm).subscribe({
      next: () => {
        this.toastService.success('Consulta médica registrada con éxito');
        this.seleccionarPaciente(this.selectedPacienteId);
        this.showConsultaModal = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al guardar consulta', err);
      }
    });
  }

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    if (file) {
      this.uploadService.subirImagen(file).subscribe({
        next: (res) => {
          this.consultaForm.estudioAdjuntoUrl = res.url;
          this.toastService.success('Estudio clínico adjuntado correctamente');
        },
        error: (err) => {
          console.error('Error al subir estudio clínico', err);
        }
      });
    }
  }

  abrirNuevoAntecedente() {
    this.antecedenteForm = { pacienteId: this.selectedPacienteId, tipo: 'Quirúrgico', descripcion: '' };
    this.showAntecedenteModal = true;
  }

  guardarAntecedente() {
    if (!this.antecedenteForm.descripcion || this.antecedenteForm.descripcion.trim() === '') {
      this.toastService.error('La descripción del antecedente es requerida');
      return;
    }
    this.antecedenteForm.pacienteId = this.selectedPacienteId;
    this.consultaService.crearAntecedente(this.antecedenteForm).subscribe({
      next: () => {
        this.toastService.success('Antecedente guardado con éxito');
        this.seleccionarPaciente(this.selectedPacienteId);
        this.showAntecedenteModal = false;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al guardar antecedente', err)
    });
  }

  abrirNuevaAlergia() {
    this.alergiaForm = { pacienteId: this.selectedPacienteId, sustancia: '', reaccion: '', severidad: 'Leve' };
    this.showAlergiaModal = true;
  }

  guardarAlergia() {
    if (!this.alergiaForm.sustancia || this.alergiaForm.sustancia.trim() === '') {
      this.toastService.error('La sustancia es requerida');
      return;
    }
    this.alergiaForm.pacienteId = this.selectedPacienteId;
    this.consultaService.crearAlergia(this.alergiaForm).subscribe({
      next: () => {
        this.toastService.success('Alergia registrada con éxito');
        this.seleccionarPaciente(this.selectedPacienteId);
        this.showAlergiaModal = false;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al guardar alergia', err)
    });
  }

  imprimirHistoria() {
    if (!this.selectedPaciente) return;
    this.printService.imprimir('hc-print-content', `Historia Clínica - ${this.selectedPaciente.nombre} ${this.selectedPaciente.apellido}`);
  }

  cerrarModal() {
    this.showConsultaModal = false;
    this.showAntecedenteModal = false;
    this.showAlergiaModal = false;
  }
}
