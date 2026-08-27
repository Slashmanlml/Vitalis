import { decodeToken, isTokenExpired, obtenerNombreUsuario, JwtClaims } from './jwt.util';

/**
 * Pruebas de las utilidades de JWT. Esta pieza protege el guard de rutas:
 * valida que el token exista Y que no esté vencido antes de dejar navegar.
 * Antes de estas pruebas no tenía ninguna cobertura.
 */

/**
 * Arma un JWT (no firmado, suficiente para decodificar) con el payload dado.
 *
 * Codifica igual que el backend: UTF-8 y base64url (sin relleno, con '-' y '_').
 * La version anterior usaba btoa(JSON.stringify(...)) directo, que revienta ante
 * cualquier caracter acentuado; por eso ninguna prueba llegaba a tocar el bug de
 * codificacion que se arreglo en jwt.util.ts.
 */
function armarToken(payload: object): string {
  const bytes = new TextEncoder().encode(JSON.stringify(payload));
  let binario = '';
  bytes.forEach(b => { binario += String.fromCharCode(b); });

  const base64url = btoa(binario)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

  return 'x.' + base64url + '.y';
}

describe('jwt.util — decodeToken', () => {
  // El nombre de la usuaria sembrada es "Laura Martínez". Con la decodificacion
  // anterior se mostraba "MartÃ­nez" en la barra superior: atob() devuelve un
  // byte por caracter y la "í" en UTF-8 ocupa dos.
  it('conserva los acentos del nombre', () => {
    const claims = decodeToken(armarToken({
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Laura Martínez'
    }));

    expect(obtenerNombreUsuario(claims!)).toBe('Laura Martínez');
  });

  it('conserva enies y otros caracteres no ASCII', () => {
    const claims = decodeToken(armarToken({
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Iñaki Núñez Peña'
    }));

    expect(obtenerNombreUsuario(claims!)).toBe('Iñaki Núñez Peña');
  });

  it('decodifica un payload en base64url, con - y _ y sin relleno', () => {
    // Un payload elegido para que su base64 contenga '+' y '/', que en base64url
    // viajan como '-' y '_'. Si no se tradujeran, atob() devolveria basura.
    const payload = { email: 'admin@vitalis.local', nota: 'ÿÿÿ?>?>', exp: 9999999999 };
    const claims = decodeToken(armarToken(payload));

    expect(claims).not.toBeNull();
    expect(claims!['nota']).toBe(payload.nota);
    expect(claims!.email).toBe(payload.email);
  });

  it('devuelve null si el token no tiene payload', () => {
    expect(decodeToken('solo-una-parte')).toBeNull();
  });

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