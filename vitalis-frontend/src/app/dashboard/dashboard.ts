import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { forkJoin, filter } from 'rxjs';
import { TurnoService } from '../services/turno.service';
import { ProfesionalService } from '../services/profesional.service';
import { SearchService, SearchResults, SearchItem } from '../services/search.service';
import { ToastService } from '../services/toast.service';
import { decodeToken, obtenerNombreUsuario, obtenerRolUsuario, obtenerEmailUsuario } from '../utils/jwt.util';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit {
  nombreUsuario: string = 'Usuario Vitalis';
  esRutaRaiz: boolean = true;
  rolUsuario: string = 'Administrador';
  emailUsuario: string = '';

  /**
   * El panel dejó de mostrar totales históricos (cuántos pacientes hay en la
   * base) para mostrar la operación del día. Un recepcionista que abre el
   * sistema a la mañana no necesita saber cuántos pacientes existen: necesita
   * saber cuántos turnos hay hoy y cuáles faltan confirmar.
   */
  indicadores: Indicador[] = [];
  cargaHoy: CargaProfesional[] = [];
  proximos: TurnoProximo[] = [];
  turnosHoyTotal = 0;
  pendientesHoy = 0;
  hoyTexto = '';
  private profesionalIdUsuario: number | null = null;

  cargando = true;
  searchQuery = '';
  searchResults: SearchResults | null = null;
  showSearch = false;
  searchTimer: any;

  constructor(
    private router: Router,
    private turnoService: TurnoService,
    private profesionalService: ProfesionalService,
    private searchService: SearchService,
    public toastService: ToastService
  ) {}

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent) {
    const target = e.target as HTMLElement;
    if (!target.closest('.search-container')) this.showSearch = false;
  }

  onSearchInput() {
    clearTimeout(this.searchTimer);
    if (this.searchQuery.length < 2) { this.searchResults = null; this.showSearch = false; return; }
    this.searchTimer = setTimeout(() => {
      this.searchService.buscar(this.searchQuery).subscribe(r => {
        this.searchResults = r;
        this.showSearch = true;
      });
    }, 300);
  }

  irA(item: SearchItem) {
    this.showSearch = false;
    this.searchQuery = '';
    this.searchResults = null;
    this.router.navigate([item.ruta]);
  }

  ngOnInit() {
    const token = localStorage.getItem('token');
    if (token) {
      const claims = decodeToken(token);
      if (claims) {
        this.nombreUsuario = obtenerNombreUsuario(claims);
        this.rolUsuario = obtenerRolUsuario(claims);
        this.emailUsuario = obtenerEmailUsuario(claims);
      }
    }
    // El estado se fija PRIMERO con la URL actual y despues se mantiene con cada
    // navegacion terminada.
    //
    // Antes solo estaba la suscripcion. Al entrar directo a una URL —recargando
    // la pagina o pegando la direccion— el componente se crea, se suscribe a las
    // navegaciones futuras, y la navegacion en curso ya termino: nunca llegaba
    // ningun evento y esRutaRaiz se quedaba en su valor inicial (true). El
    // resumen del panel principal aparecia encima de todas las pantallas.
    //
    // Ademas se filtra por NavigationEnd: router.events emite varios eventos por
    // navegacion, y en los primeros router.url todavia es la direccion anterior.
    this.actualizarRutaRaiz();
    this.router.events
      .pipe(filter(evento => evento instanceof NavigationEnd))
      .subscribe(() => this.actualizarRutaRaiz());
    this.cargarEstadisticas();
  }


  private actualizarRutaRaiz(): void {
    this.esRutaRaiz = this.router.url === '/dashboard';
  }

  cargarEstadisticas() {
    this.cargando = true;
    forkJoin({
      profesionales: this.profesionalService.obtenerTodos(),
      turnos: this.turnoService.obtenerTodos()
    }).subscribe({
      next: ({ profesionales, turnos }) => {
        this.resolverProfesionalDelUsuario(profesionales);

        const ahora = new Date();
        this.hoyTexto = this.formatearFechaLarga(ahora);

        let turnosHoy = turnos.filter(t => this.esMismoDia(new Date(t.fechaHora), ahora));

        // Un médico ve su propia jornada, no la de toda la clínica.
        if (this.rolUsuario === 'Medico' && this.profesionalIdUsuario) {
          turnosHoy = turnosHoy.filter(t => t.profesionalId === this.profesionalIdUsuario);
        }

        const vigentes = turnosHoy.filter(t => t.estado !== 'Cancelado');
        const confirmados = vigentes.filter(t => t.confirmado).length;
        const atendidos = turnosHoy.filter(t => t.estado === 'Atendido').length;
        const cancelados = turnosHoy.filter(t => t.estado === 'Cancelado').length;
        const pendientes = vigentes.filter(t => !t.confirmado).length;

        this.turnosHoyTotal = vigentes.length;
        this.pendientesHoy = pendientes;

        this.indicadores = [
          { etiqueta: 'Turnos de hoy', valor: vigentes.length, tono: 'neutro',
            detalle: cancelados > 0 ? cancelados + ' cancelado' + (cancelados === 1 ? '' : 's') : '' },
          { etiqueta: 'Confirmados', valor: confirmados, tono: 'primary',
            detalle: this.porcentaje(confirmados, vigentes.length) },
          { etiqueta: 'Sin confirmar', valor: pendientes, tono: 'warning',
            detalle: this.porcentaje(pendientes, vigentes.length) },
          { etiqueta: 'Ya atendidos', valor: atendidos, tono: 'success',
            detalle: this.porcentaje(atendidos, vigentes.length) }
        ];

        this.proximos = vigentes
          .filter(t => new Date(t.fechaHora).getTime() >= ahora.getTime())
          .sort((a, b) => new Date(a.fechaHora).getTime() - new Date(b.fechaHora).getTime())
          .slice(0, 6)
          .map(t => ({
            hora: this.formatearHora(new Date(t.fechaHora)),
            paciente: t.pacienteNombre,
            profesional: t.profesionalNombre,
            estado: t.estado === 'Atendido' ? 'Atendido' : (t.confirmado ? 'Confirmado' : 'Sin confirmar'),
            clase: t.estado === 'Atendido' ? 'estado-atendido'
                 : (t.confirmado ? 'estado-confirmado' : 'estado-pendiente')
          }));

        this.cargaHoy = this.calcularCarga(vigentes);
        this.cargando = false;
      },
      // El ErrorInterceptor global ya notifica: un toast propio acá duplicaba el aviso.
      error: (err) => {
        console.error('Error cargando el panel', err);
        this.cargando = false;
      }
    });
  }

  private resolverProfesionalDelUsuario(profesionales: { id: number; email: string }[]) {
    if (this.rolUsuario !== 'Medico' || !this.emailUsuario) return;
    const doc = profesionales.find(
      p => (p.email || '').toLowerCase() === this.emailUsuario.toLowerCase());
    this.profesionalIdUsuario = doc ? doc.id : null;
  }

  /** Carga de la jornada por profesional, de mayor a menor. */
  private calcularCarga(turnos: { profesionalNombre: string }[]): CargaProfesional[] {
    const conteo: { [nombre: string]: number } = {};
    turnos.forEach(t => conteo[t.profesionalNombre] = (conteo[t.profesionalNombre] || 0) + 1);
    const max = Math.max(...Object.values(conteo), 1);
    return Object.keys(conteo)
      .map(nombre => ({
        nombre,
        cantidad: conteo[nombre],
        porcentaje: Math.round((conteo[nombre] / max) * 100)
      }))
      .sort((a, b) => b.cantidad - a.cantidad)
      .slice(0, 6);
  }

  private porcentaje(parte: number, total: number): string {
    return total > 0 ? Math.round((parte / total) * 100) + '% del día' : '';
  }

  private esMismoDia(a: Date, b: Date): boolean {
    return a.getDate() === b.getDate()
        && a.getMonth() === b.getMonth()
        && a.getFullYear() === b.getFullYear();
  }

  private formatearHora(d: Date): string {
    const dd = (n: number) => n < 10 ? '0' + n : '' + n;
    return dd(d.getHours()) + ':' + dd(d.getMinutes());
  }

  private formatearFechaLarga(d: Date): string {
    const dias = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'];
    const meses = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
                   'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];
    const texto = dias[d.getDay()] + ' ' + d.getDate() + ' de ' + meses[d.getMonth()];
    // Sólo la inicial en mayúscula. Dejárselo a text-transform: capitalize
    // producía "Miércoles 26 De Agosto", con la preposición en mayúscula.
    return texto.charAt(0).toUpperCase() + texto.slice(1);
  }

  logout() {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}

export interface Indicador {
  etiqueta: string;
  valor: number;
  detalle?: string;
  tono: 'neutro' | 'primary' | 'warning' | 'success' | 'danger';
}

export interface CargaProfesional {
  nombre: string;
  cantidad: number;
  porcentaje: number;
}

export interface TurnoProximo {
  hora: string;
  paciente: string;
  profesional: string;
  estado: string;
  clase: string;
}
