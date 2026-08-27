/**
 * Utilidades para decodificar el token JWT y leer sus claims en el frontend.
 *
 * Los nombres de claim (identidad, rol) son los que emite JwtTokenService en el backend
 * (Vitalis.Infrastructure/Services/JwtTokenService.cs) a través de ClaimTypes.Name y
 * ClaimTypes.Role de .NET, que se serializan con sus URIs completas de schemas.xmlsoap.org /
 * schemas.microsoft.com. El claim `exp` (expiración) lo agrega automáticamente
 * JwtSecurityTokenHandler a partir del parámetro `expires` con el que se genera el token.
 */

export interface JwtClaims {
  exp?: number;
  email?: string;
  [claim: string]: unknown;
}

const CLAIM_NOMBRE = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const CLAIM_ROL = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

/**
 * Convierte el payload de un JWT (base64url, UTF-8) a texto.
 *
 * Hay dos trampas acá, y las dos estuvieron activas en este proyecto:
 *
 * 1. Un JWT no usa base64 común sino **base64url**: '-' y '_' en lugar de '+' y
 *    '/', y sin el relleno de '='. atob() espera base64 común, así que primero
 *    hay que traducir.
 *
 * 2. atob() devuelve una cadena donde **cada carácter representa un byte**. Los
 *    caracteres acentuados ocupan dos bytes en UTF-8 ("í" es C3 AD), de modo que
 *    llegan como dos caracteres sueltos y "Martínez" se muestra "MartÃ­nez". Hay
 *    que rearmar los bytes y decodificarlos como UTF-8.
 */
function decodificarPayload(payloadBase64Url: string): string {
  const base64 = payloadBase64Url.replace(/-/g, '+').replace(/_/g, '/');
  const faltante = base64.length % 4;
  const relleno = faltante === 0 ? '' : '='.repeat(4 - faltante);

  const binario = atob(base64 + relleno);
  const bytes = Uint8Array.from(binario, caracter => caracter.charCodeAt(0));

  return new TextDecoder('utf-8').decode(bytes);
}

/** Decodifica el payload de un JWT. Devuelve null si el token no tiene formato válido. */
export function decodeToken(token: string): JwtClaims | null {
  try {
    const payloadBase64 = token.split('.')[1];
    if (!payloadBase64) {
      return null;
    }
    return JSON.parse(decodificarPayload(payloadBase64));
  } catch (e) {
    console.error('Error al decodificar el token:', e);
    return null;
  }
}

/**
 * true si el token es inválido, no tiene claim `exp`, o ya venció.
 * Se usa en el guard de rutas para no dejar navegar con un token vencido
 * (antes solo se validaba que el token existiera en localStorage).
 */
export function isTokenExpired(token: string): boolean {
  const claims = decodeToken(token);
  if (!claims || typeof claims.exp !== 'number') {
    return true;
  }
  const expiraEnMs = claims.exp * 1000;
  return Date.now() >= expiraEnMs;
}

export function obtenerNombreUsuario(claims: JwtClaims): string {
  return (claims[CLAIM_NOMBRE] as string) || (claims['NombreCompleto'] as string) || 'Usuario';
}

export function obtenerRolUsuario(claims: JwtClaims): string {
  return (claims[CLAIM_ROL] as string) || (claims['Rol'] as string) || 'Administrador';
}

export function obtenerEmailUsuario(claims: JwtClaims): string {
  return claims['email'] as string || '';
}
