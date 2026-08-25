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

/** Decodifica el payload de un JWT. Devuelve null si el token no tiene formato válido. */
export function decodeToken(token: string): JwtClaims | null {
  try {
    const payloadBase64 = token.split('.')[1];
    return JSON.parse(atob(payloadBase64));
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
