import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { BloqueoService, BloqueoAgenda, ImpactoBloqueo } from './bloqueo.service';

describe('BloqueoService', () => {
  let servicio: BloqueoService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    servicio = TestBed.inject(BloqueoService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const bloqueo: BloqueoAgenda = {
    id: 1,
    profesionalId: 20,
    profesionalNombre: 'Dr. Gómez',
    fechaHoraInicio: '2026-09-01T09:00:00',
    fechaHoraFin: '2026-09-01T11:00:00',
    motivo: 'Vacaciones'
  };

  const impacto: ImpactoBloqueo = {
    cantidadTurnos: 1,
    pacientesAfectados: 2,
    pacientesConEmail: 1,
    turnos: [
      { turnoId: 5, fechaHora: '2026-09-01T09:00:00', pacienteNombre: 'Ana', estado: 'Solicitado', tieneEmail: true }
    ]
  };

  it('obtenerTodos pega a /BloqueosAgenda con GET', () => {
    servicio.obtenerTodos().subscribe(lista => expect(lista).toEqual([bloqueo]));
    const req = http.expectOne(r => r.url.endsWith('/BloqueosAgenda'));
    expect(req.request.method).toBe('GET');
    req.flush([bloqueo]);
  });

  it('obtenerPorProfesional pega a /BloqueosAgenda/profesional/{id} con GET', () => {
    servicio.obtenerPorProfesional(20).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/BloqueosAgenda/profesional/20'));
    expect(req.request.method).toBe('GET');
    req.flush([bloqueo]);
  });

  it('obtenerImpacto pega a /BloqueosAgenda/impacto con los tres query params', () => {
    // El servicio setea profesionalId, desde y hasta SIEMPRE (no son opcionales).
    servicio.obtenerImpacto(20, '2026-09-01', '2026-09-05').subscribe(resultado => {
      expect(resultado).toEqual(impacto);
    });
    const req = http.expectOne(r => r.url.endsWith('/BloqueosAgenda/impacto'));
    expect(req.request.method).toBe('GET');
    const params = req.request.params;
    expect(params.get('profesionalId')).toBe('20');
    expect(params.get('desde')).toBe('2026-09-01');
    expect(params.get('hasta')).toBe('2026-09-05');
    req.flush(impacto);
  });

  it('crear pega a /BloqueosAgenda con POST y el body correcto', () => {
    const dto = {
      profesionalId: 20,
      fechaHoraInicio: '2026-09-01T09:00:00',
      fechaHoraFin: '2026-09-01T11:00:00',
      motivo: 'Vacaciones'
    };
    servicio.crear(dto).subscribe(creado => expect(creado).toEqual(bloqueo));
    const req = http.expectOne(r => r.url.endsWith('/BloqueosAgenda'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(bloqueo);
  });

  it('eliminar pega a /BloqueosAgenda/{id} con DELETE', () => {
    servicio.eliminar(1).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/BloqueosAgenda/1'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});