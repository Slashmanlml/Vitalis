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
  filtroTexto = '';

  simulacionForm: SimularEmailDto = {
    destinatario: '',
    tipoNotificacion: 'ConfirmacionTurno',
    asunto: '',
    cuerpo: ''
  };

  tiposNotificacion = [
    { valor: 'ConfirmacionTurno', label: 'Confirmación de Turno Reservado' },
    { valor: 'RecordatorioTurno', label: 'Recordatorio de Cita (24hs antes)' },
    { valor: 'CancelacionTurno', label: 'Aviso de Cancelación de Turno' },
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
    this.emailService.obtenerTodos().subscribe({
      next: (data) => {
        this.emails = data || [];
        this.aplicarFiltro();
        if (this.filteredEmails.length > 0 && !this.selectedEmail) {
          this.selectedEmail = this.filteredEmails[0];
        }
        this.cargando = false;
      },
      error: (err) => {
        console.error('Error al obtener emails:', err);
        this.toastService.error('Error al cargar logs de emails');
        this.cargando = false;
      }
    });
  }

  cargarPacientes() {
    this.pacienteService.obtenerTodos().subscribe({
      next: (data) => {
        this.pacientes = data || [];
        if (this.pacientes.length > 0 && !this.simulacionForm.destinatario) {
          this.simulacionForm.destinatario = this.pacientes[0].email || 'paciente@vitalis.local';
        }
      },
      error: (err) => console.error('Error al cargar pacientes:', err)
    });
  }

  aplicarFiltro() {
    if (!this.filtroTexto.trim()) {
      this.filteredEmails = [...this.emails];
    } else {
      const q = this.filtroTexto.toLowerCase();
      this.filteredEmails = this.emails.filter(e =>
        e.destinatario.toLowerCase().includes(q) ||
        e.asunto.toLowerCase().includes(q)
      );
    }
    if (this.selectedEmail && !this.filteredEmails.some(e => e.id === this.selectedEmail?.id)) {
      this.selectedEmail = this.filteredEmails.length > 0 ? this.filteredEmails[0] : null;
    }
  }

  seleccionarEmail(email: EmailLog) {
    this.selectedEmail = email;
  }

  abrirModalSimulacion() {
    if (this.pacientes.length > 0 && !this.simulacionForm.destinatario) {
      this.simulacionForm.destinatario = this.pacientes[0].email || 'paciente@vitalis.local';
    }
    this.showSimularModal = true;
  }

  cerrarModal() {
    this.showSimularModal = false;
  }

  onPacienteSelect(event: any) {
    const id = Number(event.target.value);
    const pac = this.pacientes.find(p => p.id === id);
    if (pac) {
      this.simulacionForm.destinatario = pac.email || `${pac.nombre.toLowerCase()}.${pac.apellido.toLowerCase()}@vitalis.local`;
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
        this.toastService.success(`Correo simulado enviado a ${logCreado.destinatario}`);
        this.simulando = false;
        this.showSimularModal = false;
        this.emails.unshift(logCreado);
        this.aplicarFiltro();
        this.selectedEmail = logCreado;
      },
      error: (err) => {
        this.simulando = false;
        console.error('Error al simular email:', err);
        this.toastService.error('Error al emitir correo simulado');
      }
    });
  }

  eliminarLog(id: number, event: Event) {
    event.stopPropagation();
    if (!confirm('¿Desea eliminar este registro de correo?')) return;

    this.emailService.eliminar(id).subscribe({
      next: () => {
        this.toastService.success('Registro eliminado');
        this.emails = this.emails.filter(e => e.id !== id);
        this.aplicarFiltro();
        if (this.selectedEmail?.id === id) {
          this.selectedEmail = this.filteredEmails.length > 0 ? this.filteredEmails[0] : null;
        }
      },
      error: (err) => {
        console.error('Error al eliminar log:', err);
        this.toastService.error('Error al eliminar registro');
      }
    });
  }

  limpiarBandeja() {
    if (!confirm('¿Está seguro de vaciar toda la bandeja de correos simulados?')) return;

    this.emailService.limpiar().subscribe({
      next: () => {
        this.toastService.success('Bandeja de correos vaciada con éxito');
        this.emails = [];
        this.filteredEmails = [];
        this.selectedEmail = null;
      },
      error: (err) => {
        console.error('Error al limpiar logs:', err);
        this.toastService.error('Error al vaciar bandeja');
      }
    });
  }
}
