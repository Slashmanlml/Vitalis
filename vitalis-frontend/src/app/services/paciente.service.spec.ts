import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PacienteService } from './paciente.service';
import { Paciente, CrearPaciente, EditarPaciente } from '../models/paciente.model';

describe('PacienteService', () => {
  let servicio: PacienteService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    servicio = TestBed.inject(PacienteService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const paciente: Paciente = {
    id: 1,
    nombre: 'Ana',
    apellido: 'Pérez',
    dni: '30111222',
    fechaNacimiento: '1990-01-01',
    email: 'ana@test.com',
    activo: true
  };

  it('obtenerTodos sin búsqueda pega a /pacientes con GET y sin query params', () => {
    servicio.obtenerTodos().subscribe(lista => expect(lista).toEqual([paciente]));
    const req = http.expectOne(r => r.url.endsWith('/pacientes'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([paciente]);
  });

  it('obtenerTodos con búsqueda arma el query param buscar', () => {
    servicio.obtenerTodos('gomez').subscribe();
    const req = http.expectOne(r => r.url.endsWith('/pacientes'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('buscar')).toBe('gomez');
    req.flush([]);
  });

  it('obtenerTodos devuelve lo que responde el backend', () => {
    const lista = [paciente, { ...paciente, id: 2, dni: '33344455' }];
    servicio.obtenerTodos().subscribe(res => expect(res).toEqual(lista));
    const req = http.expectOne(r => r.url.endsWith('/pacientes'));
    req.flush(lista);
  });

  it('obtenerPorId pega a /pacientes/{id} con GET', () => {
    servicio.obtenerPorId(1).subscribe(p => expect(p).toEqual(paciente));
    const req = http.expectOne(r => r.url.endsWith('/pacientes/1'));
    expect(req.request.method).toBe('GET');
    req.flush(paciente);
  });

  it('crear pega a /pacientes con POST y el body correcto', () => {
    const dto: CrearPaciente = {
      nombre: 'Ana',
      apellido: 'Pérez',
      dni: '11122233',
      fechaNacimiento: '1990-01-15',
      email: 'ana@test.com'
    };
    servicio.crear(dto).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/pacientes'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(paciente);
  });

  it('editar pega a /pacientes/{id} con PUT y el body correcto', () => {
    const dto: EditarPaciente = { nombre: 'Ana', apellido: 'Pérez', telefono: '1234' };
    servicio.editar(1, dto).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/pacientes/1'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dto);
    req.flush(paciente);
  });

  it('desactivar pega a /pacientes/{id} con DELETE', () => {
    servicio.desactivar(1).subscribe();
    const req = http.expectOne(r => r.url.endsWith('/pacientes/1'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});