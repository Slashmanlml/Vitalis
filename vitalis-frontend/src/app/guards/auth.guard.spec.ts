import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { authGuard, pacienteGuard } from './auth.guard';

const CLAIM_ROL = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function tokenCon(rol: string, venceEnSegundos = 3600): string {
  const b64 = (o: object) => btoa(JSON.stringify(o)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `${b64({ alg: 'HS256' })}.${b64({ [CLAIM_ROL]: rol, exp: Math.floor(Date.now() / 1000) + venceEnSegundos })}.firma`;
}

function correr(guard: typeof authGuard): boolean | string {
  const r = TestBed.runInInjectionContext(() => guard({} as any, {} as any));
  return r instanceof UrlTree ? TestBed.inject(Router).serializeUrl(r) : (r as boolean);
}

describe('guards de rutas', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('sin sesión, ambos mandan al login', () => {
    expect(correr(authGuard)).toBe('/login');
    expect(correr(pacienteGuard)).toBe('/login');
  });

  it('un paciente no entra al panel del consultorio: va a su portal', () => {
    localStorage.setItem('token', tokenCon('Paciente'));
    expect(correr(authGuard)).toBe('/portal');
    expect(correr(pacienteGuard)).toBe(true);
  });

  it('el personal no entra al portal de pacientes', () => {
    localStorage.setItem('token', tokenCon('Recepcionista'));
    expect(correr(authGuard)).toBe(true);
    expect(correr(pacienteGuard)).toBe('/dashboard');
  });

  it('un token vencido se descarta', () => {
    localStorage.setItem('token', tokenCon('Administrador', -10));
    expect(correr(authGuard)).toBe('/login');
    expect(localStorage.getItem('token')).toBeNull();
  });
});
