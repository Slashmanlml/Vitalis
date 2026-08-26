import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EmailService, EmailLog, SimularEmailDto } from '../services/email.service';
import { PacienteService } from '../services/paciente.service';
import { ToastService } from '../services/toast.service';
import { Paciente } from '../models/paciente.model';

@Component({
  selector: 'app-email-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './email-logs.html',
  styleUrls: ['./email-logs.css']
})
export class EmailLogsComponent implements OnInit {
  emails: EmailLog[] = [];
  filteredEmails: EmailLog[] = [];
  selectedEmail: EmailLog | null = null;
  pacientes: Paciente[] = [];

  cargando = false;
  simulando = false;
  showSimularModal = false;

  // Filtros
  filtroTexto = '';
  filtroOrigen = '';
  filtroEstado = '';
  filtroEvento = '';

  simulacionForm: SimularEmailDto = {
    destinatario: '',
    tipoNotificacion: 'TurnoConfirmado',
    asunto: '',
    cuerpo: ''
  };

  tiposNotificacion = [
    { valor: 'TurnoCreado', label: 'Reserva de Turno Registrada' },
    { valor: 'TurnoConfirmado', label: 'Turno Confirmado Oficialmente' },
    { valor: 'RecordatorioTurno', label: 'Recordatorio Automático (24hs antes)' },
    { valor: 'TurnoReprogramado', label: 'Aviso de Reprogramación de Turno' },
    { valor: 'TurnoCancelado', label: 'Aviso de Cancelación de Turno' },
    { valor: 'ResumenConsulta', label: 'Resumen de Consulta Médica' },
    { valor: 'NuevaPrescripcion', label: 'Emisión de Receta Médica Electrónica' },
    { valor: 'BienvenidaPaciente', label: 'Bienvenida / Alta de Paciente en Portal' },
    { valor: 'Personalizado', label: 'Notificación Libre / Personalizada' }
  ];

  constructor(
    private emailService: EmailService,
    private pacienteService: PacienteService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.cargarEmails();
    this.cargarPacientes();
  }

  cargarEmails() {
    this.cargando = true;
    this.emailService.obtenerTodos(
      this.filtroOrigen || undefined,
      this.filtroEvento || undefined,
      this.filtroEstado || undefined
    ).subscribe({
      next: (data) => {
        this.emails = data || [];
        this.aplicarFiltro();
        if (this.filteredEmails.length > 0 && (!this.selectedEmail || !this.filteredEmails.some(e => e.id === this.selectedEmail?.id))) {
          this.selectedEmail = this.filteredEmails[0];
        } else if (this.filteredEmails.length === 0) {
          this.selectedEmail = null;
        }
        this.cargando = false;
      },
      error: (err) => {
        console.error('Error al obtener notificaciones:', err);
        this.toastService.error('Error al cargar notificaciones');
        this.cargando = false;
      }
    });
  }

  cargarPacientes() {
    this.pacienteService.obtenerTodos().subscribe({
      next: (data) => {
        this.pacientes = (data || []).filter(p => !!p.email);
        if (this.pacientes.length > 0 && !this.simulacionForm.destinatario) {
          this.simulacionForm.destinatario = this.pacientes[0].email || '';
        }
      },
      error: (err) => console.error('Error al cargar pacientes:', err)
    });
  }

  aplicarFiltro() {
    let resultado = [...this.emails];

    if (this.filtroOrigen) {
      resultado = resultado.filter(e => e.origen === this.filtroOrigen);
    }

    if (this.filtroEstado) {
      resultado = resultado.filter(e => e.estado === this.filtroEstado);
    }

    if (this.filtroEvento) {
      resultado = resultado.filter(e => e.evento === this.filtroEvento);
    }

    if (this.filtroTexto.trim()) {
      const q = this.filtroTexto.toLowerCase();
      resultado = resultado.filter(e =>
        e.destinatario.toLowerCase().includes(q) ||
        e.asunto.toLowerCase().includes(q) ||
        (e.evento && e.evento.toLowerCase().includes(q))
      );
    }

    this.filteredEmails = resultado;

    if (this.selectedEmail && !this.filteredEmails.some(e => e.id === this.selectedEmail?.id)) {
      this.selectedEmail = this.filteredEmails.length > 0 ? this.filteredEmails[0] : null;
    }
  }

  seleccionarEmail(email: EmailLog) {
    this.selectedEmail = email;
  }

  abrirModalSimulacion() {
    if (this.pacientes.length > 0 && !this.simulacionForm.destinatario) {
      this.simulacionForm.destinatario = this.pacientes[0].email || '';
    }
    this.showSimularModal = true;
  }

  cerrarModal() {
    this.showSimularModal = false;
  }

  onPacienteSelect(event: any) {
    const id = Number(event.target.value);
    const pac = this.pacientes.find(p => p.id === id);
    if (pac && pac.email) {
      this.simulacionForm.destinatario = pac.email;
    }
  }

  enviarSimulacion() {
    if (!this.simulacionForm.destinatario) {
      this.toastService.error('Debe ingresar un email destinatario');
      return;
    }

    this.simulando = true;
    this.emailService.simularEnvio(this.simulacionForm).subscribe({
      next: (logCreado) => {
        this.toastService.success(`Notificación simulada para ${logCreado.destinatario}`);
        this.simulando = false;
        this.showSimularModal = false;
        this.emails.unshift(logCreado);
        this.aplicarFiltro();
        this.selectedEmail = logCreado;
      },
      error: (err) => {
        this.simulando = false;
        console.error('Error al simular notificación:', err);
        this.toastService.error('Error al emitir notificación simulada');
      }
    });
  }

  eliminarLog(email: EmailLog, event: Event) {
    event.stopPropagation();
    if (email.origen === 'Sistema') {
      this.toastService.error('No se pueden eliminar notificaciones auditadas del sistema.');
      return;
    }

    if (!confirm('¿Desea eliminar este registro de prueba simulada?')) return;

    this.emailService.eliminar(email.id).subscribe({
      next: () => {
        this.toastService.success('Registro de prueba eliminado');
        this.emails = this.emails.filter(e => e.id !== email.id);
        this.aplicarFiltro();
        if (this.selectedEmail?.id === email.id) {
          this.selectedEmail = this.filteredEmails.length > 0 ? this.filteredEmails[0] : null;
        }
      },
      error: (err) => {
        console.error('Error al eliminar log:', err);
        this.toastService.error(err?.error?.message || 'Error al eliminar registro');
      }
    });
  }

  getEventoLabel(evento: string): string {
    const mapa: { [key: string]: string } = {
      'TurnoCreado': 'Reserva de Turno',
      'TurnoConfirmado': 'Turno Confirmado',
      'TurnoReprogramado': 'Turno Reprogramado',
      'TurnoCancelado': 'Cancelación de Turno',
      'RecordatorioTurno': 'Recordatorio (24hs)',
      'ResumenConsulta': 'Resumen de Atención',
      'NuevaPrescripcion': 'Receta Médica',
      'BienvenidaPaciente': 'Bienvenida al Portal',
      'Personalizado': 'Notificación Manual'
    };
    return mapa[evento] || evento || 'Notificación';
  }
}
