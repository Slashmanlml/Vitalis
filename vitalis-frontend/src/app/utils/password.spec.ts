import { describe, it, expect } from 'vitest';
import { esPasswordSegura, mensajeDeError } from './password';

describe('esPasswordSegura (misma regla que el backend)', () => {
  it.each([
    ['Abc12345', true],
    ['Paciente123', true],
    ['abc12345', false],
    ['ABC12345', false],
    ['Abcdefgh', false],
    ['Ab1', false],
    ['123456', false],
    ['', false]
  ])('%s → %s', (valor, esperado) => {
    expect(esPasswordSegura(valor)).toBe(esperado);
  });
});

describe('mensajeDeError', () => {
  it('usa el mensaje del backend', () => {
    expect(mensajeDeError({ error: { mensaje: 'DNI duplicado' } }, 'x')).toBe('DNI duplicado');
  });
  it('toma el primer error de validación de ASP.NET Core', () => {
    expect(mensajeDeError({ error: { errors: { Dni: ['El DNI debe tener 7 u 8 dígitos'] } } }, 'x'))
      .toBe('El DNI debe tener 7 u 8 dígitos');
  });
  it('avisa si no hay conexión', () => {
    expect(mensajeDeError({ status: 0 }, 'x')).toContain('conectar');
  });
});

import { evaluarPassword, generarPasswordSegura } from './password';

describe('evaluarPassword (medidor de fuerza)', () => {
  it('vacía no tiene nivel', () => {
    expect(evaluarPassword('').nivel).toBe(0);
  });
  it('si no cumple lo obligatorio es débil y dice qué falta', () => {
    const e = evaluarPassword('tito1234');
    expect(e.nivel).toBe(1);
    expect(e.valida).toBe(false);
    expect(e.consejo).toContain('mayúscula');
  });
  it('lo mínimo es aceptable y recomienda un símbolo', () => {
    const e = evaluarPassword('Tito1234');
    expect(e.nivel).toBe(2);
    expect(e.consejo).toContain('símbolo');
  });
  it('con símbolo es buena; con símbolo y 12 caracteres, excelente', () => {
    expect(evaluarPassword('Tito1234!').nivel).toBe(3);
    expect(evaluarPassword('Tito1234!Vitalis').nivel).toBe(4);
  });
  it('el símbolo es recomendado, no obligatorio', () => {
    const simbolo = evaluarPassword('Tito1234').requisitos.find(r => r.texto.startsWith('Un símbolo'))!;
    expect(simbolo.obligatorio).toBe(false);
    expect(simbolo.cumple).toBe(false);
  });
});

describe('generarPasswordSegura', () => {
  it('genera 16 caracteres con los cuatro tipos, que el sistema califica de excelente', () => {
    for (let i = 0; i < 50; i++) {
      const p = generarPasswordSegura();
      expect(p).toHaveLength(16);
      expect(evaluarPassword(p).nivel).toBe(4);
    }
  });
  it('no repite', () => {
    const muestras = new Set(Array.from({ length: 100 }, () => generarPasswordSegura()));
    expect(muestras.size).toBe(100);
  });
});
