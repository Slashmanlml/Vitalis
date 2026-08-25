import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PacienteService } from '../services/paciente.service';
import { TurnoService } from '../services/turno.service';
import { ProfesionalService } from '../services/profesional.service';
import { ObraSocialService } from '../services/obra-social.service';
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

  estadisticas: any[] = [];
  obrasSocialesStats: any[] = [];
  consultasStats: any[] = [];

  cargando = true;
  searchQuery = '';
  searchResults: SearchResults | null = null;
  showSearch = false;
  searchTimer: any;

  constructor(
    private router: Router,
    private pacienteService: PacienteService,
    private turnoService: TurnoService,
    private profesionalService: ProfesionalService,
    private obraSocialService: ObraSocialService,
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
    this.router.events.subscribe(() => {
      this.esRutaRaiz = this.router.url === '/dashboard';
    });
    this.cargarEstadisticas();
  }


  cargarEstadisticas() {
    this.cargando = true;
    forkJoin({
      pacientes: this.pacienteService.obtenerTodos(),
      profesionales: this.profesionalService.obtenerTodos(),
      turnos: this.turnoService.obtenerTodos(),
      obras: this.obraSocialService.obtenerTodas()
    }).subscribe({
      next: ({ pacientes, profesionales, turnos, obras }) => {
        const hoy = new Date().toISOString().split('T')[0];
        const turnosHoy = turnos.filter((t: any) => t.fechaHora.startsWith(hoy));
        const pendientesHoy = turnosHoy.filter((t: any) => !t.confirmado).length;

        // Calculate Obra Social Stats
        const obraSocialMap: { [key: string]: number } = {};
        pacientes.forEach((p: any) => {
          const nombre = p.obraSocialNombre || 'Particular';
          obraSocialMap[nombre] = (obraSocialMap[nombre] || 0) + 1;
        });
        const totalPacientesCount = pacientes.length || 1;
        this.obrasSocialesStats = Object.keys(obraSocialMap).map(key => ({
          nombre: key,
          cantidad: obraSocialMap[key],
          porcentaje: Math.round((obraSocialMap[key] / totalPacientesCount) * 100)
        })).sort((a, b) => b.cantidad - a.cantidad);

        // Calculate consultations stats per doctor
        const doctorMap: { [key: string]: number } = {};
        turnos.forEach((t: any) => {
          doctorMap[t.profesionalNombre] = (doctorMap[t.profesionalNombre] || 0) + 1;
        });
        const maxConsultas = Math.max(...Object.values(doctorMap), 1);
        this.consultasStats = Object.keys(doctorMap).map(key => ({
          nombre: key,
          cantidad: doctorMap[key],
          porcentaje: Math.round((doctorMap[key] / maxConsultas) * 100)
        })).sort((a, b) => b.cantidad - a.cantidad).slice(0, 5);

        this.cargando = false;
        this.estadisticas = [
          { titulo: 'Pacientes Registrados', valor: pacientes.length.toString(), cambio: '+ Registros activos', icono: 'users', color: 'accent' },
          { titulo: 'Médicos Activos', valor: profesionales.filter((p: any) => p.activo).length.toString(), cambio: `${profesionales.length} en total`, icono: 'doctor', color: 'primary' },
          { titulo: 'Turnos para Hoy', valor: turnosHoy.length.toString(), cambio: `${pendientesHoy} pendientes de confirmar`, icono: 'calendar', color: 'warning' },
          { titulo: 'Obras Sociales', valor: obras.length.toString(), cambio: 'Convenios activos', icono: 'shield', color: 'info' }
        ];
      },
      error: (err) => {
        console.error('Error cargando estadísticas', err);
        this.cargando = false;
        this.toastService.error('Error al cargar los datos del tablero');
      }
    });
  }

  logout() {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
