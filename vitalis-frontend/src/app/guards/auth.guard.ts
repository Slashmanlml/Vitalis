import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { isTokenExpired } from '../utils/jwt.util';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');

  if (token && !isTokenExpired(token)) {
    return true;
  }

  // No hay token, o el que había en localStorage ya venció: limpiar la sesión
  // y redirigir a login (antes solo se validaba que el token existiera, no su vigencia).
  if (token) {
    localStorage.removeItem('token');
  }
  router.navigate(['/login']);
  return false;
};
