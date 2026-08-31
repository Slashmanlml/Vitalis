import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { PacienteFichaComponent } from './paciente-ficha';

describe('PacienteFichaComponent', () => {
  let fixture: ComponentFixture<PacienteFichaComponent>;
  let componente: PacienteFichaComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PacienteFichaComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PacienteFichaComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('se crea sin errores', () => {
    expect(componente).toBeTruthy();
  });

  it('calcula la edad correctamente a partir de una fecha de nacimiento', () => {
    expect(componente.calcularEdad(undefined)).toBe('N/D');
    expect(componente.calcularEdad('')).toBe('N/D');

    const fechaNacimiento = '1990-05-12';
    const edadStr = componente.calcularEdad(fechaNacimiento);
    expect(edadStr).toContain('años');
  });

  it('mapea correctamente las clases de estado de los turnos', () => {
    expect(componente.obtenerClaseEstadoTurno('Atendido')).toBe('status-success');
    expect(componente.obtenerClaseEstadoTurno('Confirmado')).toBe('status-success');
    expect(componente.obtenerClaseEstadoTurno('Solicitado')).toBe('status-warning');
    expect(componente.obtenerClaseEstadoTurno('Cancelado')).toBe('status-danger');
    expect(componente.obtenerClaseEstadoTurno('Otro')).toBe('status-info');
  });

  it('bloquea el cambio a pestañas clínicas si el rol no es Médico', () => {
    componente.tabActiva = 'datos';
    
    // Si no es médico, cambiar a historia o recetas no debe cambiar la pestaña activa
    if (!componente.esMedico) {
      componente.cambiarTab('historia');
      expect(componente.tabActiva).toBe('datos');

      componente.cambiarTab('recetas');
      expect(componente.tabActiva).toBe('datos');
    }

    // Cambiar a turnos sí debe permitirse
    componente.cambiarTab('turnos');
    expect(componente.tabActiva).toBe('turnos');
  });

  it('valida los campos del formulario de edición de paciente', () => {
    componente.editForm = {
      nombre: 'J',
      apellido: 'P',
      telefono: '123',
      email: 'invalido'
    };

    expect(componente.isFieldValid('nombre')).toBe(false);
    expect(componente.isFieldValid('apellido')).toBe(false);
    expect(componente.isFieldValid('email')).toBe(false);
    expect(componente.isFormValid()).toBe(false);

    componente.editForm = {
      nombre: 'Juan',
      apellido: 'Pérez',
      telefono: '1155551234',
      email: 'juan@email.com'
    };

    expect(componente.isFieldValid('nombre')).toBe(true);
    expect(componente.isFieldValid('apellido')).toBe(true);
    expect(componente.isFieldValid('email')).toBe(true);
    expect(componente.isFormValid()).toBe(true);
  });
});
