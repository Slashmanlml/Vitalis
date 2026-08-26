import { decodeToken, isTokenExpired, JwtClaims } from './jwt.util';

/**
 * Pruebas de las utilidades de JWT. Esta pieza protege el guard de rutas:
 * valida que el token exista Y que no esté vencido antes de dejar navegar.
 * Antes de estas pruebas no tenía ninguna cobertura.
 */

/** Arma un JWT (no firmado, suficiente para decodificar) con el payload dado. */
function armarToken(payload: object): string {
  return 'x.' + btoa(JSON.stringify(payload)) + '.y';
}

describe('jwt.util — decodeToken', () => {
  it('devuelve null con un token mal formado', () => {
    expect(decodeToken('no-es-un-jwt')).toBeNull();
  });

  it('devuelve null con un payload que no es JSON', () => {
    expect(decodeToken('x.' + btoa('{no es json') + '.y')).toBeNull();
  });

  it('devuelve los claims de un JWT válido', () => {
    const payload = { exp: 9999999999, email: 'admin@vitalis.local' };
    const claims = decodeToken(armarToken(payload));
    expect(claims).not.toBeNull();
    expect(claims!.exp).toBe(payload.exp);
    expect(claims!.email).toBe(payload.email);
  });
});

describe('jwt.util — isTokenExpired', () => {
  it('devuelve true con un exp ya vencido', () => {
    const expPasado = Math.floor(Date.now() / 1000) - 60;
    expect(isTokenExpired(armarToken({ exp: expPasado }))).toBe(true);
  });

  it('devuelve false con un exp en el futuro', () => {
    const expFuturo = Math.floor(Date.now() / 1000) + 3600;
    expect(isTokenExpired(armarToken({ exp: expFuturo }))).toBe(false);
  });

  it('devuelve true con un token sin claim exp', () => {
    expect(isTokenExpired(armarToken({ email: 'a@vitalis.local' }))).toBe(true);
  });
});