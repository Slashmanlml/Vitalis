import { describe, expect, it, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { RestablecerPasswordComponent } from './restablecer-password';

function crear(token: string | null, respuesta: any = of({ mensaje: 'Listo' })) {
  const route: any = { snapshot: { queryParamMap: { get: () => token } } };
  const router: any = { navigate: vi.fn() };
  const auth: any = { restablecerPassword: vi.fn(() => respuesta) };
  const host: any = { nativeElement: document.createElement('div') };
  const c = new RestablecerPasswordComponent(route, router, auth, host);
  c.ngOnInit();
  return { c, router, auth };
}

describe('RestablecerPasswordComponent', () => {
  it('borra el token de la barra de direcciones apenas lo lee', () => {
    const { router } = crear('abc');
    expect(router.navigate).toHaveBeenCalledWith([], { replaceUrl: true, queryParams: {} });
  });

  it('sin token muestra el aviso de enlace incompleto', () => {
    expect(crear(null).c.estado).toBe('sin-token');
  });

  it('no manda nada si la contraseña es débil o no coincide', () => {
    const { c, auth } = crear('abc');
    c.password = 'corta';
    c.confirmar = 'corta';
    c.guardar();
    expect(c.errorDe('password')).not.toBe('');
    c.password = 'Segura2026!';
    c.confirmar = 'Otra2026!';
    c.guardar();
    expect(c.errorDe('confirmar')).toBe('Las dos contraseñas no coinciden.');
    expect(auth.restablecerPassword).not.toHaveBeenCalled();
  });

  it('con datos válidos manda el token y la contraseña, y limpia el formulario', () => {
    const { c, auth } = crear('abc');
    c.usarSugerida('Segura2026!x');
    c.guardar();
    expect(auth.restablecerPassword).toHaveBeenCalledWith('abc', 'Segura2026!x', 'Segura2026!x');
    expect(c.estado).toBe('ok');
    expect(c.password).toBe('');
  });

  it('si el enlace venció ofrece pedir uno nuevo', () => {
    const err = { status: 400, error: { mensaje: 'El enlace venció.' } };
    const { c } = crear('abc', throwError(() => err));
    c.usarSugerida('Segura2026!x');
    c.guardar();
    expect(c.enlaceInvalido).toBe(true);
    expect(c.errorMsg).toContain('venció');
  });
});
