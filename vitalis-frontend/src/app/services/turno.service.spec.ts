import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TurnoService } from './turno.service';
import { Turno, CrearTurno, EditarTurno } from '../models/turno.model';

describe('TurnoService', () => {
  let servicio: TurnoService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    servicio = TestBed.inject(TurnoService);
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
    obraSocialNombre: 'OSDE',
    fechaHora: '2026-09-01T09:00:00',
    confirmado: false,
    estado: 'Solicitado'
  };

  it('obtenerTodos pega a /turnos con GET y devuelve la lista', () => {
    servicio.obtenerTodos().subscribe(data => {
      expect(data).toEqual([turno]);
    });
    const req = http.expectOne(r => r.url.endsWith('/turnos'));
    expect(req.request.method).toBe('GET');
    req.flush([turno]);
  });

  it('obtenerPorId pega a /turnos/{id} con GET', () => {
    servicio.obtenerPorId(7).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/turnos/7'));
    expect(req.request.method).toBe('GET');
    req.flush(turno);
  });

  it('crear pega a /turnos con POST y el body correcto', () => {
    const dto: CrearTurno = {
      pacienteId: 10,
      profesionalId: 20,
      obraSocialId: 30,
      fechaHora: '2026-09-01T09:00:00'
    };
    servicio.crear(dto).subscribe(creado => expect(creado).toEqual(turno));
    const req = http.expectOne(r => r.url.endsWith('/turnos'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(turno);
  });

  it('editar pega a /turnos/{id} con PUT y el body correcto', () => {
    const dto: EditarTurno = {
      pacienteId: 10,
      profesionalId: 20,
      obraSocialId: 30,
      fechaHora: '2026-09-01T10:00:00',
      confirmado: true,
      estado: 'Confirmado'
    };
    servicio.editar(1, dto).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/turnos/1'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dto);
    req.flush({ ...turno, confirmado: true });
  });

  it('eliminar pega a /turnos/{id} con DELETE', () => {
    servicio.eliminar(1).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/turnos/1'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});