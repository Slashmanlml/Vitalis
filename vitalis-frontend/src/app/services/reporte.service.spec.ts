import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ReporteService } from './reporte.service';
import { Turno } from '../models/turno.model';

describe('ReporteService', () => {
  let servicio: ReporteService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    servicio = TestBed.inject(ReporteService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const turno: Turno = {
    id: 1,
    pacienteId: 10,
    pacienteNombre: 'Ana Pérez',
    profesionalId: 20,
    profesionalNombre: 'Dr. Gómez',
    obraSocialId: 30,
    obraSocialNombre: 'OSC',
    fechaHora: '2026-09-01T09:00:00',
    confirmado: false,
    estado: 'Solicitado'
  };

  it('estadisticas pega a /Reportes/Estadisticas con GET', () => {
    const stats = {
      totalTurnos: 10,
      confirmados: 4,
      pendientes: 3,
      atendidos: 2,
      cancelados: 1,
      porEspecialidad: [{ etiqueta: 'Cardiología', cantidad: 5 }],
      porObraSocial: [],
      porProfesional: [],
      porMes: []
    };
    servicio.estadisticas().subscribe(resultado => expect(resultado).toEqual(stats));
    const req = http.expectOne(r => r.url.endsWith('/Reportes/Estadisticas'));
    expect(req.request.method).toBe('GET');
    req.flush(stats);
  });

  it('turnosPorProfesional sin fechas no arma query params', () => {
    servicio.turnosPorProfesional(20).subscribe(lista => expect(lista).toEqual([turno]));
    const req = http.expectOne(r => r.url.endsWith('/Reportes/TurnosPorProfesional/20'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([turno]);
  });

  it('turnosPorProfesional con fechas arma los query params desde y hasta', () => {
    servicio.turnosPorProfesional(20, '2026-09-01', '2026-09-30').subscribe();
    const req = http.expectOne(r => r.url.endsWith('/Reportes/TurnosPorProfesional/20'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('desde')).toBe('2026-09-01');
    expect(req.request.params.get('hasta')).toBe('2026-09-30');
    req.flush([]);
  });

  it('turnosPorPaciente pega a /Reportes/TurnosPorPaciente/{id} con GET', () => {
    servicio.turnosPorPaciente(10).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/Reportes/TurnosPorPaciente/10'));
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('turnosPorObraSocial pega a /Reportes/TurnosPorObraSocial/{id} con GET', () => {
    servicio.turnosPorObraSocial(30).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/Reportes/TurnosPorObraSocial/30'));
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});