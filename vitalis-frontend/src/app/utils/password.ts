/**
 * Misma regla que PasswordSeguraAttribute en el backend. Acá solo sirve para
 * avisar antes de enviar; la que manda es la del servidor.
 */
export const DESCRIPCION_PASSWORD =
  'La contraseña debe tener entre 8 y 128 caracteres, con al menos una mayúscula, una minúscula y un número.';

export function esPasswordSegura(valor: string): boolean {
  return !!valor
    && valor.length >= 8 && valor.length <= 128
    && /[A-ZÁÉÍÓÚÑ]/.test(valor)
    && /[a-záéíóúñ]/.test(valor)
    && /\d/.test(valor);
}

export interface RequisitoPassword {
  texto: string;
  cumple: boolean;
  /** false = recomendado, no lo exige el servidor. */
  obligatorio: boolean;
}

export interface EvaluacionPassword {
  /** 0 vacía, 1 débil, 2 aceptable, 3 buena, 4 excelente. */
  nivel: 0 | 1 | 2 | 3 | 4;
  etiqueta: string;
  consejo: string;
  requisitos: RequisitoPassword[];
  valida: boolean;
}

const SIMBOLO = /[^A-Za-z0-9ÁÉÍÓÚÑáéíóúñ\s]/;

export function evaluarPassword(valor: string): EvaluacionPassword {
  const v = valor ?? '';
  const requisitos: RequisitoPassword[] = [
    { texto: '8 caracteres o más', cumple: v.length >= 8, obligatorio: true },
    { texto: 'Una mayúscula', cumple: /[A-ZÁÉÍÓÚÑ]/.test(v), obligatorio: true },
    { texto: 'Una minúscula', cumple: /[a-záéíóúñ]/.test(v), obligatorio: true },
    { texto: 'Un número', cumple: /\d/.test(v), obligatorio: true },
    { texto: 'Un símbolo (recomendado)', cumple: SIMBOLO.test(v), obligatorio: false }
  ];
  const valida = esPasswordSegura(v);

  if (!v) {
    return { nivel: 0, etiqueta: '', consejo: '', requisitos, valida };
  }

  if (!valida) {
    const faltan = requisitos.filter(r => r.obligatorio && !r.cumple).map(r => r.texto.toLowerCase());
    return {
      nivel: 1,
      etiqueta: 'Débil',
      consejo: `Le falta: ${faltan.join(', ')}.`,
      requisitos,
      valida
    };
  }

  const tieneSimbolo = SIMBOLO.test(v);
  if (tieneSimbolo && v.length >= 12) {
    return { nivel: 4, etiqueta: 'Excelente', consejo: 'Es difícil de adivinar.', requisitos, valida };
  }
  if (tieneSimbolo || v.length >= 12) {
    return {
      nivel: 3,
      etiqueta: 'Buena',
      consejo: tieneSimbolo ? 'Si la alargás a 12 caracteres, queda excelente.' : 'Sumale un símbolo para que quede excelente.',
      requisitos,
      valida
    };
  }
  return {
    nivel: 2,
    etiqueta: 'Aceptable',
    consejo: 'Cumple lo mínimo. Recomendamos sumar un símbolo (como ! # $ %) y llegar a 12 caracteres.',
    requisitos,
    valida
  };
}

/**
 * Genera una contraseña de 16 caracteres con mayúsculas, minúsculas, números y
 * símbolos, usando el generador criptográfico del navegador (no Math.random).
 * Se evitan caracteres que se confunden al leerlos: 0/O, 1/l/I.
 */
export function generarPasswordSegura(largo = 16): string {
  const grupos = [
    'ABCDEFGHJKLMNPQRSTUVWXYZ',
    'abcdefghijkmnpqrstuvwxyz',
    '23456789',
    '!#$%&*+-=?@_'
  ];
  const todos = grupos.join('');
  const azar = (n: number) => {
    const b = new Uint32Array(1);
    crypto.getRandomValues(b);
    return b[0] % n;
  };
  // Uno de cada grupo, garantizado; el resto, de cualquiera.
  const chars = grupos.map(g => g[azar(g.length)]);
  while (chars.length < largo) chars.push(todos[azar(todos.length)]);
  // Mezcla (Fisher-Yates) para que los obligatorios no queden siempre al principio.
  for (let i = chars.length - 1; i > 0; i--) {
    const j = azar(i + 1);
    [chars[i], chars[j]] = [chars[j], chars[i]];
  }
  return chars.join('');
}

/** Primer mensaje de error legible de una respuesta del backend. */
export function mensajeDeError(err: any, porDefecto: string): string {
  const cuerpo = err?.error;
  if (cuerpo?.mensaje) return cuerpo.mensaje;
  if (cuerpo?.errors) {
    const primero = Object.values(cuerpo.errors as Record<string, string[]>)[0];
    if (primero?.[0]) return primero[0];
  }
  if (err?.status === 429) return 'Hiciste muchos intentos seguidos. Esperá unos minutos y probá de nuevo.';
  if (err?.status === 0) return 'No se puede conectar con el servidor. Revisá que la API esté encendida.';
  return porDefecto;
}
