import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { LoginComponent } from './login';

/**
 * Pruebas de humo de la pantalla de login.
 *
 * El archivo generado por Angular CLI importaba una clase `Login` que no
 * existe (la clase real es `LoginComponent`), así que ni siquiera compilaba
 * y dejaba toda la suite del frontend rota.
 */
describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let componente: LoginComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('se crea sin errores', () => {
    expect(componente).toBeTruthy();
  });

  it('arranca con los campos vacíos y sin mensaje de error', () => {
    expect(componente.email).toBe('');
    expect(componente.password).toBe('');
    expect(componente.errorMsg).toBe('');
    expect(componente.isLoading).toBe(false);
  });

  it('no intenta autenticar si falta el correo o la contraseña', () => {
    componente.email = '';
    componente.password = '';
    componente.login();
    // Sin credenciales no debe quedar en estado "cargando": ni siquiera llama a la API.
    expect(componente.isLoading).toBe(false);
  });
});
