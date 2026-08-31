import { decodeToken, obtenerRolUsuario } from './jwt.util';

/**
 * Qué puede EDITAR cada rol en las pantallas de catálogo.
 *
 * IMPORTANTE, y no es un detalle: esto NO es seguridad. La seguridad está en el
 * backend, donde cada endpoint de escritura lleva su [Authorize(Roles = ...)] y
 * responde 403 sin mirar lo que diga el navegador. Cualquiera puede cambiar
 * estas constantes desde las herramientas de desarrollo y no va a conseguir
 * nada.
 *
 * Esto sirve para otra cosa, que también importa: que la interfaz no ofrezca lo
 * que después va a negar. Un botón "Nuevo profesional" que un médico puede
 * apretar para recibir un 403 no es un fallo de seguridad, es un fallo de
 * honestidad, y le enseña al usuario que el sistema está roto cuando en realidad
 * funciona bien.
 *
 * Al ser dos lugares con la misma regla, pueden separarse con el tiempo. Si
 * cambiás un permiso acá, cambialo también en el controlador correspondiente
 * de backend/src/Vitalis.Api/Controllers/. La autoridad es el backend.
 */
export type RecursoDeCatalogo =
  | 'profesionales'
  | 'especialidades'
  | 'obras-sociales'
  | 'medicamentos'
  | 'prestaciones';

const ROLES_CON_ESCRITURA: Record<RecursoDeCatalogo, string[]> = {
  'profesionales':   ['Administrador'],
  'especialidades':  ['Administrador'],
  'obras-sociales':  ['Administrador'],
  'medicamentos':    ['Administrador'],
  // Las prestaciones son el catálogo de precios: también las mantiene facturación.
  'prestaciones':    ['Administrador', 'Facturacion']
};

/** Rol del usuario autenticado, o cadena vacía si no hay sesión válida. */
export function rolActual(): string {
  const token = localStorage.getItem('token');
  if (!token) return '';

  const claims = decodeToken(token);
  return claims ? obtenerRolUsuario(claims) : '';
}

/** True si el rol actual puede crear, editar o eliminar en ese catálogo. */
export function puedeEditar(recurso: RecursoDeCatalogo): boolean {
  return ROLES_CON_ESCRITURA[recurso].includes(rolActual());
}

/** True si el rol actual es administrador. */
export function esAdministrador(): boolean {
  return rolActual() === 'Administrador';
}
