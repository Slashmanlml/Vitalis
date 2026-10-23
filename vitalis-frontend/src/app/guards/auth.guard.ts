import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { decodeToken, isTokenExpired, obtenerRolUsuario } from '../utils/jwt.util';

export const ROL_PACIENTE = 'Paciente';

/** Rol del token vigente, o null si no hay sesión válida (y la limpia). */
function rolDeSesionVigente(): string | null {
  const token = localStorage.getItem('token');
  if (token && !isTokenExpired(token)) {
    const claims = decodeToken(token);
    return claims ? obtenerRolUsuario(claims) : null;
  }
  // No hay token, o el que había ya venció: limpiar la sesión.
  if (token) {
    localStorage.removeItem('token');
  }
  return null;
}

/**
 * Panel del consultorio (/dashboard): solo personal. Un paciente que escribe
 * /dashboard a mano va a su portal. Igual que siempre, esto es comodidad de la
 * interfaz: lo que protege los datos es el [Authorize] del backend.
 */
export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const rol = rolDeSesionVigente();

  if (rol === null) {
    return router.createUrlTree(['/login']);
  }
  if (rol === ROL_PACIENTE) {
    return router.createUrlTree(['/portal']);
  }
  return true;
};

/** Portal del paciente (/portal): solo cuentas con rol Paciente. */
export const pacienteGuard: CanActivateFn = () => {
  const router = inject(Router);
  const rol = rolDeSesionVigente();

  if (rol === null) {
    return router.createUrlTree(['/login']);
  }
  if (rol !== ROL_PACIENTE) {
    return router.createUrlTree(['/dashboard']);
  }
  return true;
};
