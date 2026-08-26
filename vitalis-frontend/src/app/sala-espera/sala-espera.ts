import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TurnoService } from '../services/turno.service';
import { Turno } from '../models/turno.model';

@Component({
  selector: 'app-sala-espera',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sala-espera.html',
  styleUrls: ['./sala-espera.css']
})
export class SalaEsperaComponent implements OnInit, OnDestroy {
  turnosHoy: Turno[] = [];
  filtroEstado: string = 'todos';
  horaActual: string = '';
  
  // Real-time call variables
  showCallAlert: boolean = false;
  pacienteLlamado: Turno | null = null;
  private pollingInterval: any;
  private relojInterval: any;

  constructor(
    private turnoService: TurnoService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.actualizarReloj();
    this.cargarTurnos();
    
    // Reloj en tiempo real
    this.relojInterval = setInterval(() => {
      this.actualizarReloj();
    }, 1000);

    // Polling de sincronización cada 5 segundos
    this.pollingInterval = setInterval(() => {
      this.cargarTurnos();
    }, 5000);
  }

  ngOnDestroy() {
    if (this.pollingInterval) clearInterval(this.pollingInterval);
    if (this.relojInterval) clearInterval(this.relojInterval);
  }

  actualizarReloj() {
    const ahora = new Date();
    this.horaActual = ahora.toLocaleTimeString('es-AR', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  cargarTurnos() {
    this.turnoService.obtenerTodos().subscribe({
      next: (data) => {
        const hoy = new Date().toISOString().split('T')[0];
        const nuevosTurnos = (data || [])
          .filter(t => t.fechaHora.startsWith(hoy))
          .sort((a, b) => new Date(a.fechaHora).getTime() - new Date(b.fechaHora).getTime());
        
        // Detectar si algún turno cambió a 'En Atencion' para disparar el llamado automático
        if (this.turnosHoy.length > 0) {
          nuevosTurnos.forEach(nuevo => {
            const viejo = this.turnosHoy.find(t => t.id === nuevo.id);
            if (viejo && viejo.estado !== 'En Atencion' && nuevo.estado === 'En Atencion') {
              this.llamarPaciente(nuevo);
            }
          });
        }
        
        this.turnosHoy = nuevosTurnos;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al actualizar sala de espera:', err)
    });
  }

  llamarPaciente(turno: Turno) {
    this.pacienteLlamado = turno;
    this.showCallAlert = true;
    this.playChime();
    
    // Cierre automático tras 7 segundos
    setTimeout(() => {
      this.showCallAlert = false;
      this.pacienteLlamado = null;
      this.cdr.detectChanges();
    }, 7000);
  }

  cerrarLlamadoManual() {
    this.showCallAlert = false;
    this.pacienteLlamado = null;
  }

  playChime() {
    try {
      const AudioContextClass = (window as any).AudioContext || (window as any).webkitAudioContext;
      if (!AudioContextClass) return;
      const ctx = new AudioContextClass();
      
      // Primer tono (agudo)
      const osc1 = ctx.createOscillator();
      const gain1 = ctx.createGain();
      osc1.connect(gain1);
      gain1.connect(ctx.destination);
      osc1.frequency.setValueAtTime(659.25, ctx.currentTime); // E5
      gain1.gain.setValueAtTime(0.35, ctx.currentTime);
      gain1.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 1.2);
      
      // Segundo tono armónico
      const osc2 = ctx.createOscillator();
      const gain2 = ctx.createGain();
      osc2.connect(gain2);
      gain2.connect(ctx.destination);
      osc2.frequency.setValueAtTime(523.25, ctx.currentTime + 0.3); // C5
      gain2.gain.setValueAtTime(0.35, ctx.currentTime + 0.3);
      gain2.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 1.6);
      
      osc1.start();
      osc1.stop(ctx.currentTime + 1.2);
      osc2.start(ctx.currentTime + 0.3);
      osc2.stop(ctx.currentTime + 1.6);
    } catch (e) {
      console.warn('AudioContext no inicializado por interacción de usuario.', e);
    }
  }

  get filteredTurnos(): Turno[] {
    if (this.filtroEstado === 'todos') return this.turnosHoy;
    return this.turnosHoy.filter(t => t.estado === this.filtroEstado);
  }

  // Contadores de estado en tiempo real
  get countEnEspera(): number {
    return this.turnosHoy.filter(t => t.estado === 'En Espera').length;
  }

  get countEnAtencion(): number {
    return this.turnosHoy.filter(t => t.estado === 'En Atencion').length;
  }

  get countAtendidos(): number {
    return this.turnosHoy.filter(t => t.estado === 'Atendido').length;
  }

  get countSolicitadosOConfirmados(): number {
    return this.turnosHoy.filter(t => t.estado === 'Solicitado' || t.estado === 'Confirmado').length;
  }

  cambiarEstado(turno: Turno, nuevoEstado: string) {
    this.turnoService.editar(turno.id, {
      pacienteId: turno.pacienteId,
      profesionalId: turno.profesionalId,
      obraSocialId: turno.obraSocialId,
      fechaHora: turno.fechaHora,
      confirmado: true,
      estado: nuevoEstado
    }).subscribe({
      next: () => this.cargarTurnos(),
      error: (err) => console.error('Error al cambiar estado de turno:', err)
    });
  }

  // Cálculo de tiempo transcurrido y demoras
  calcularMinutosEspera(fechaHoraIso: string): number {
    const horaTurno = new Date(fechaHoraIso).getTime();
    const ahora = new Date().getTime();
    const difMin = Math.floor((ahora - horaTurno) / (1000 * 60));
    return Math.max(0, difMin);
  }

  esDemorado(turno: Turno): boolean {
    if (turno.estado !== 'En Espera' && turno.estado !== 'Solicitado' && turno.estado !== 'Confirmado') {
      return false;
    }
    return this.calcularMinutosEspera(turno.fechaHora) > 20;
  }
}
