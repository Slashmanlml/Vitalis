import { Component, OnInit, OnDestroy } from '@angular/core';
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
  
  // Real-time call variables
  showCallAlert: boolean = false;
  pacienteLlamado: Turno | null = null;
  private pollingInterval: any;

  constructor(private turnoService: TurnoService) {}

  ngOnInit() {
    this.cargarTurnos();
    
    // Poll for updates every 5 seconds to simulate real-time updates
    this.pollingInterval = setInterval(() => {
      this.cargarTurnos();
    }, 5000);
  }

  ngOnDestroy() {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }

  cargarTurnos() {
    this.turnoService.obtenerTodos().subscribe(data => {
      const hoy = new Date().toISOString().split('T')[0];
      const nuevosTurnos = data.filter(t => t.fechaHora.startsWith(hoy));
      
      // Check if any patient transition happened to 'En Atencion'
      if (this.turnosHoy.length > 0) {
        nuevosTurnos.forEach(nuevo => {
          const viejo = this.turnosHoy.find(t => t.id === nuevo.id);
          if (viejo && viejo.estado !== 'En Atencion' && nuevo.estado === 'En Atencion') {
            this.llamarPaciente(nuevo);
          }
        });
      }
      
      this.turnosHoy = nuevosTurnos;
    });
  }

  llamarPaciente(turno: Turno) {
    this.pacienteLlamado = turno;
    this.showCallAlert = true;
    this.playChime();
    
    // Auto close announcement after 6 seconds
    setTimeout(() => {
      this.showCallAlert = false;
      this.pacienteLlamado = null;
    }, 6000);
  }

  playChime() {
    try {
      const AudioContextClass = (window as any).AudioContext || (window as any).webkitAudioContext;
      const ctx = new AudioContextClass();
      
      // First chime (ding)
      const osc1 = ctx.createOscillator();
      const gain1 = ctx.createGain();
      osc1.connect(gain1);
      gain1.connect(ctx.destination);
      osc1.frequency.setValueAtTime(587.33, ctx.currentTime); // D5
      gain1.gain.setValueAtTime(0.4, ctx.currentTime);
      gain1.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 1.2);
      
      // Second chime (dong)
      const osc2 = ctx.createOscillator();
      const gain2 = ctx.createGain();
      osc2.connect(gain2);
      gain2.connect(ctx.destination);
      osc2.frequency.setValueAtTime(440, ctx.currentTime + 0.3); // A4
      gain2.gain.setValueAtTime(0.4, ctx.currentTime + 0.3);
      gain2.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 1.5);
      
      osc1.start();
      osc1.stop(ctx.currentTime + 1.2);
      
      osc2.start(ctx.currentTime + 0.3);
      osc2.stop(ctx.currentTime + 1.5);
    } catch (e) {
      console.warn('AudioContext not allowed or initialized yet by user interaction.', e);
    }
  }

  get filteredTurnos() {
    if (this.filtroEstado === 'todos') return this.turnosHoy;
    return this.turnosHoy.filter(t => t.estado === this.filtroEstado);
  }

  cambiarEstado(turno: Turno, nuevoEstado: string) {
    this.turnoService.editar(turno.id, {
      pacienteId: turno.pacienteId,
      profesionalId: turno.profesionalId,
      obraSocialId: turno.obraSocialId,
      fechaHora: turno.fechaHora,
      confirmado: true,
      estado: nuevoEstado
    }).subscribe(() => this.cargarTurnos());
  }

  getColor(estado: string): string {
    const colores: any = {
      'Solicitado': 'status-pending',
      'Confirmado': 'status-info',
      'En Espera': 'status-warning',
      'En Atencion': 'status-primary',
      'Atendido': 'status-success',
      'Ausente': 'status-danger',
      'Cancelado': 'status-danger'
    };
    return colores[estado] || 'status-pending';
  }
}
