import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { PrescripcionService } from '../services/prescripcion.service';
import { PacienteService } from '../services/paciente.service';
import { ProfesionalService } from '../services/profesional.service';
import { ConsultaMedicaService } from '../services/consulta-medica.service';
import { MedicamentoService } from '../services/medicamento.service';
import { PrintService } from '../services/print.service';
import { ToastService } from '../services/toast.service';
import { Paciente } from '../models/paciente.model';
import { Profesional } from '../models/profesional.model';
import { ConsultaMedica } from '../models/consulta.model';
import { Medicamento } from '../models/medicamento.model';
import { Prescripcion, CrearPrescripcion } from '../models/prescripcion.model';

@Component({
  selector: 'app-prescripciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './prescripciones.html',
  styleUrls: ['./prescripciones.css']
})
export class PrescripcionesComponent implements OnInit, OnDestroy {
  pacientes: Paciente[] = [];
  profesionales: Profesional[] = [];
  medicamentos: Medicamento[] = [];
  consultasPaciente: ConsultaMedica[] = [];
  prescripciones: Prescripcion[] = [];

  selectedPacienteId: number = 0;
  selectedPaciente: Paciente | null = null;
  selectedPrescripcion: Prescripcion | null = null;

  showModal: boolean = false;
  showDetailModal: boolean = false;
  guardando: boolean = false;
  cargando: boolean = false;

  form: CrearPrescripcion = {
    consultaMedicaId: 0,
    pacienteId: 0,
    profesionalId: 0,
    observaciones: '',
    detalles: []
  };

  private destroy$ = new Subject<void>();

  constructor(
    private prescripcionService: PrescripcionService,
    private pacienteService: PacienteService,
    private profesionalService: ProfesionalService,
    private consultaService: ConsultaMedicaService,
    private medicamentoService: MedicamentoService,
    private printService: PrintService,
    private toastService: ToastService,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargando = true;
    forkJoin({
      pacientes: this.pacienteService.obtenerTodos(),
      profesionales: this.profesionalService.obtenerTodos(),
      medicamentos: this.medicamentoService.obtenerTodos()
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: ({ pacientes, profesionales, medicamentos }) => {
        this.pacientes = pacientes;
        this.profesionales = profesionales;
        this.medicamentos = medicamentos;
        this.cargando = false;

        this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
          if (params['pacienteId']) {
            this.seleccionarPaciente(Number(params['pacienteId']));
          } else if (this.pacientes.length > 0) {
            this.seleccionarPaciente(this.pacientes[0].id);
          }
        });
        this.cdr.detectChanges();
      },
      error: () => {
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  seleccionarPaciente(pacienteId: number) {
    this.selectedPacienteId = Number(pacienteId);
    this.selectedPaciente = this.pacientes.find(p => p.id === this.selectedPacienteId) || null;
    this.selectedPrescripcion = null;

    if (this.selectedPacienteId) {
      this.cargarPrescripciones();
      this.cargarConsultas();
    } else {
      this.prescripciones = [];
      this.consultasPaciente = [];
    }
  }

  cargarPrescripciones() {
    this.prescripcionService.obtenerPorPaciente(this.selectedPacienteId).subscribe({
      next: data => {
        this.prescripciones = data;
        this.cdr.detectChanges();
      },
      error: err => console.error('Error al cargar prescripciones', err)
    });
  }

  cargarConsultas() {
    this.consultaService.obtenerPorPaciente(this.selectedPacienteId).subscribe({
      next: data => {
        this.consultasPaciente = data;
        this.cdr.detectChanges();
      },
      error: err => console.error('Error al cargar consultas', err)
    });
  }

  abrirNuevaPrescripcion() {
    if (!this.selectedPacienteId) {
      this.toastService.info('Seleccione un paciente primero');
      return;
    }

    const defaultConsulta = this.consultasPaciente.length > 0 ? this.consultasPaciente[0] : null;

    this.form = {
      consultaMedicaId: defaultConsulta ? defaultConsulta.id : 0,
      pacienteId: this.selectedPacienteId,
      profesionalId: defaultConsulta ? defaultConsulta.profesionalId : (this.profesionales.length > 0 ? this.profesionales[0].id : 0),
      observaciones: '',
      detalles: [
        {
          medicamentoId: this.medicamentos.length > 0 ? this.medicamentos[0].id : 0,
          dosis: '500 mg',
          frecuencia: 'Cada 8 horas',
          duracion: '7 días',
          indicaciones: 'Tomar con las comidas'
        }
      ]
    };
    this.showModal = true;
  }

  onConsultaChange() {
    const consulta = this.consultasPaciente.find(c => c.id === Number(this.form.consultaMedicaId));
    if (consulta) {
      this.form.profesionalId = consulta.profesionalId;
    }
  }

  agregarDetalle() {
    const defaultMedId = this.medicamentos.length > 0 ? this.medicamentos[0].id : 0;
    this.form.detalles.push({
      medicamentoId: defaultMedId,
      dosis: '',
      frecuencia: '',
      duracion: '',
      indicaciones: ''
    });
  }

  removerDetalle(index: number) {
    if (this.form.detalles.length > 1) {
      this.form.detalles.splice(index, 1);
    } else {
      this.toastService.info('La receta debe contener al menos un medicamento');
    }
  }

  guardarPrescripcion() {
    if (!this.form.consultaMedicaId) {
      this.toastService.error('Debe seleccionar una consulta médica asociada');
      return;
    }
    if (!this.form.profesionalId) {
      this.toastService.error('Debe seleccionar el profesional emisor');
      return;
    }
    if (this.form.detalles.length === 0) {
      this.toastService.error('Debe agregar al menos un medicamento');
      return;
    }

    for (const d of this.form.detalles) {
      if (!d.medicamentoId || !d.dosis || !d.frecuencia || !d.duracion) {
        this.toastService.error('Complete todos los campos del medicamento (Dosis, Frecuencia y Duración)');
        return;
      }
    }

    this.guardando = true;
    this.prescripcionService.crear(this.form).subscribe({
      next: (creada) => {
        this.toastService.success('Prescripción emitida exitosamente');
        this.guardando = false;
        this.showModal = false;
        this.cargarPrescripciones();
        this.verDetalle(creada);
        this.cdr.detectChanges();
      },
      error: () => {
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  verDetalle(prescripcion: Prescripcion) {
    this.selectedPrescripcion = prescripcion;
    this.showDetailModal = true;
  }

  imprimir(prescripcion: Prescripcion) {
    this.selectedPrescripcion = prescripcion;
    this.cdr.detectChanges();
    setTimeout(() => {
      this.printService.imprimir('receta-print-content', `Receta Médica - ${prescripcion.pacienteNombre}`);
    }, 150);
  }

  cerrarModales() {
    this.showModal = false;
    this.showDetailModal = false;
  }
}
