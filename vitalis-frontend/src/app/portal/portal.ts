import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { PortalService, MiPerfilPaciente, MiTurno } from '../services/portal.service';
import { AuthService } from '../services/auth.service';

@Component({
  // Angular 22 usa OnPush por defecto. Esta pantalla guarda lo que llega del
  // servidor en propiedades comunes (no signals), así que necesita la
  // detección de cambios "de siempre" para que se dibuje al llegar la respuesta.
  changeDetection: ChangeDetectionStrategy.Eager,
  selector: 'app-portal',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './portal.html',
  styleUrls: ['./portal.css']
})
export class PortalComponent implements OnInit {
  perfil: MiPerfilPaciente | null = null;
  proximos: MiTurno[] = [];
  anteriores: MiTurno[] = [];
  cargando = true;
  error = '';

  constructor(private portal: PortalService, private auth: AuthService, private router: Router) {}

  ngOnInit() {
    this.portal.miPerfil().subscribe({
      next: p => this.perfil = p,
      error: () => this.error = 'No pudimos cargar tu ficha. Si el problema sigue, comunicate con recepción.'
    });

    this.portal.misTurnos().subscribe({
      next: turnos => {
        const ahora = Date.now();
        this.proximos = turnos
          .filter(t => new Date(t.fechaHora).getTime() >= ahora && t.estado !== 'Cancelado')
          .sort((a, b) => a.fechaHora.localeCompare(b.fechaHora));
        this.anteriores = turnos.filter(t => !this.proximos.includes(t));
        this.cargando = false;
      },
      error: () => this.cargando = false
    });
  }

  /** El turno que más importa: el más cercano que todavía no pasó. */
  get siguiente(): MiTurno | null {
    return this.proximos[0] ?? null;
  }

  get restoProximos(): MiTurno[] {
    return this.proximos.slice(1);
  }

  get saludo(): string {
    const h = new Date().getHours();
    return h < 13 ? 'Buen día' : h < 20 ? 'Buenas tardes' : 'Buenas noches';
  }

  /** "en 3 días", "mañana", "hoy" */
  cuandoFalta(fecha: string): string {
    const dia = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
    const dias = Math.round((dia(new Date(fecha)) - dia(new Date())) / 86400000);
    if (dias <= 0) return 'Hoy';
    if (dias === 1) return 'Mañana';
    return `En ${dias} días`;
  }

  claseEstado(estado: string): string {
    return 'estado-' + estado.toLowerCase();
  }

  salir() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
