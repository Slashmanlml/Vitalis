# Barrido de rendimiento — funciones llamadas en directivas estructurales

> Tarea: cerrar el patrón que rompía la pantalla de reportes (funciones llamadas
> dentro de directivas que devolvían objetos/arrays nuevos en cada ciclo de
> detección de cambios y forzaban la reconstrucción del DOM). El patrón ya está
> corregido en `reportes.*` y `auditorias.*`. Aquí se busca en el resto.
>
> Auditor: OpenCode / DeepSeek. Ronda 6.
> Territorio: todo `.html` bajo `vitalis-frontend/src/app` **excepto**
> `reportes.*`, `auditorias.*`, `pacientes.*`, `paciente-ficha.*`,
> `dashboard.*`, `app.config.ts`, `styles.css`, `utils/*` y backend.

---

## Comando de barrido

Con `Select-String` (equivalente del `grep -rnE` de la tarea, regex
PCRE-like sobre `*.html` recursivo en `src/app`):

```powershell
ForEach-Object { if ($line -match '\*ngIf="[^"]*[a-zA-Z_]+\(') {...}
                 if ($line -match '\*ngFor="[^"]*[a-zA-Z_]+\(') {...}
                 if ($line -match '\[[a-zA-Z]+\]="[a-zA-Z_]+\(') {...}
                 if ($line -match '\{\{ *[a-zA-Z_]+\(') {...} }
```

(Bash con grep -E en este entorno no resolvía bien el `cd` a una ruta con
espacios; se usó el equivalente nativo.)

## Hallazgos del barrido, en mi territorio

Por archivo, en el orden en que aparecieron:

| Archivo | Línea | Llamada | Devuelve | Veredicto |
|---|---|---|---|---|
| `agenda/agenda-semanal.html` | 62 | `cargaDe(c)` | `number` | **Descartado** (escalar; ver nota) |
| `agenda/agenda-semanal.html` | 62 | `cargaDe(c) === 1` | `boolean` | **Descartado** (escalar) |
| `agenda/agenda-semanal.html` | 88 | `claseTurno(t)` | `string` | **Descartado** |
| `agenda/agenda-semanal.html` | 91 | `horaDe(t)` | `string` | **Descartado** |
| `bloqueos/bloqueos.html` | 125 | `estaEnCurso(b)` | `boolean` | **Descartado** |
| `email-logs/email-logs.html` | 102 | `getEventoLabel(mail.evento)` | `string` | **Descartado** |
| `email-logs/email-logs.html` | 138 | `getEventoLabel(selectedEmail.evento)` | `string` | **Descartado** |
| `especialidades/especialidades.html` | 57 | `isFieldInvalid('nombre')` | `boolean` | **Descartado** |
| `especialidades/especialidades.html` | 57 | `getFieldError('nombre')` | `string` | **Descartado** |
| `especialidades/especialidades.html` | 62 | `isFieldInvalid('descripcion')` | `boolean` | **Descartado** |
| `especialidades/especialidades.html` | 62 | `getFieldError('descripcion')` | `string` | **Descartado** |
| `facturacion/facturacion.html` | 36 | `getSaldo(f)` | `number` | **Descartado** |
| `facturacion/facturacion.html` | 37 | `getEstadoClass(f.estado)` | `string` | **Descartado** |
| `historia-clinica/historia-clinica.html` | 135 | `getProfesionalNombrePorTurno(turnoId)` | `string` | **Descartado** (no está en `*ngFor`) |
| `liquidaciones/liquidaciones.html` | 28 | `getEstadoClass(l.estado)` | `string` | **Descartado** |
| `obras-sociales/obras-sociales.html` | 63, 68 | `isFieldInvalid(...)` / `getFieldError(...)` | `boolean`/`string` | **Descartado** |
| `profesionales/profesionales.html` | 94, 99, 106, 111, 120 | `isFieldInvalid(...)` / `getFieldError(...)` | `boolean`/`string` | **Descartado** |
| `sala-espera/sala-espera.html` | 143 | `esDemorado(turno)` | `boolean` | **Descartado** |
| `sala-espera/sala-espera.html` | 145 | `calcularMinutosEspera(turno.fechaHora)` | `number` | **Descartado** |
| `sala-espera/sala-espera.html` | 177 | `calcularMinutosEspera(turno.fechaHora)` | `number` | **Descartado** |
| `turnos/turnos.html` | 108, 116, 130, 135 | `isFieldInvalid(...)` / `getFieldError(...)` | `boolean`/`string` | **Descartado** |
| `usuarios/usuarios.html` | 68, 73, 78, 89 | `isFieldInvalid(...)` / `getFieldError(...)` | `boolean`/`string` | **Descartado** |

**Total: 32 llamadas a funciones detectadas; 0 corregidas; 32 descartadas.**

## Por qué se descartan todas

La tarea distingue dos casos:

1. **"Si la función devuelve un objeto o un array NUEVO en cada llamada, es un
   problema."** El arreglo de `reportes`/`auditorias` que motivó este barrido
   consistía en una función como `barras(porEspecialidad)` que construía un
   `[{etiqueta, altura, porcentaje}, ...]` nuevo cada vez, y `*ngFor` con
   `trackBy` por índice no podía estabilizar la identidad, así que Angular
   desmontaba y volvía a montar cada chip en cada ciclo de detección.

2. **"Si devuelve un string, un número o un booleano derivado de datos que ya
   están cargados, es barato y se puede dejar."** Esto es lo que pasa en
   todos los hallazgos restantes.

Se leyó el cuerpo de cada función en el `.ts` correspondiente para confirmar:

- `agenda/agenda-semanal.ts:269` (`claseTurno`): retorna uno de cuatro literales
  `'turno-cancelado' | 'turno-atendido' | 'turno-en-atencion' | 'turno-confirmado' | 'turno-pendiente'`. Es un `switch` sobre `t.estado` y `t.confirmado` que ya están en memoria. **Escalar.**
- `agenda/agenda-semanal.ts:276` (`horaDe`): retorna un string `HH:mm`. Hace un
  `new Date(t.fechaHora)` (una vez por turno) y formatea con `dosDigitos`. **Escalar.**
- `agenda/agenda-semanal.ts:289` (`cargaDe`): retorna `number`. Hace un
  `filter().length`. Crea un array intermedio para contar, pero **lo que se le
  pasa al template es un número**, no el array. **Escalar.** (Nota menor: la
  línea 62 del HTML llama a `cargaDe(c)` dos veces para mostrar la cantidad y
  decidir el plural. Es trabajo duplicado, pero no rompe Angular. No lo corrijo
  porque la tarea me limita a "solo los del primer grupo" y este no es del
  primer grupo.)
- `bloqueos/bloqueos.ts:111` (`estaEnCurso`): retorna `boolean`. Compara
  `Date.now()` con los extremos del bloqueo. **Escalar.**
- `email-logs/email-logs.ts:202` (`getEventoLabel`): retorna `string` desde
  un mapa literal. **Escalar.** (Pequeño detalle: el mapa se reconstruye en
  cada llamada. Es trabajo barato, no se nota. Si en el futuro la pantalla
  tiene miles de logs, se puede mover a un `private static readonly`. No es
  un problema hoy.)
- `facturacion/facturacion.ts:124` (`getSaldo`): retorna `number`. Hace un
  `reduce` sobre `f.pagos` para sumar, pero el resultado es un escalar. **Escalar.**
- `facturacion/facturacion.ts:119` (`getEstadoClass`): retorna `string` desde
  un mapa literal. **Escalar.**
- `historia-clinica/historia-clinica.ts:134` (`getProfesionalNombrePorTurno`):
  retorna `string` (lookup en `turnosDisponibles`). Además, está **fuera de un
  `*ngFor`** — es el campo autollenado del formulario de creación de consulta
  médica, se renderiza una sola vez. **Escalar.**
- `liquidaciones/liquidaciones.ts:75` (`getEstadoClass`): retorna `string` desde
  un mapa literal. **Escalar.**
- `sala-espera/sala-espera.ts:172` (`calcularMinutosEspera`): retorna `number`.
  Construye dos `Date` y divide. **Escalar.** (Vale la misma observación que
  en email-logs: la sala de espera se refresca con un `setInterval`, así que
  estas llamadas se ejecutan periódicamente. El costo de dos `new Date` y una
  resta es despreciable. No es el patrón que rompe la pantalla.)
- `sala-espera/sala-espera.ts:179` (`esDemorado`): retorna `boolean` y delega en
  `calcularMinutosEspera`. **Escalar.**
- `isFieldInvalid` / `getFieldError` (varios `.ts`): es el patrón estándar de
  formularios del proyecto (lo usan `especialidades`, `obras-sociales`,
  `profesionales`, `turnos`, `usuarios`). El método toca un `touched[field]`
  que es local al componente y devuelve escalar. **Escalar.**

## Conclusión

**El patrón que rompió la pantalla de reportes no se encontró en el resto del
frontend que me corresponde.** En reportes el problema era que la función
devolvía un **array nuevo** que se pasaba a `*ngFor` sin un `trackBy` estable.
En el resto, las funciones llamadas desde directivas estructurales y
bindings devuelven escalares (string/number/boolean), que Angular maneja sin
reconstruir el DOM.

**No hay nada que corregir.** Tres verificaciones para que esto no quede en
una afirmación vacía:

1. El barrido se corrió con el regex exacto de la tarea sobre los cuatro
   patrones (`*ngIf`, `*ngFor`, `[attr]`, `{{ }}`).
2. Para cada hallazgo se leyó el cuerpo de la función en el `.ts`
   correspondiente.
3. Ningún hallazgo devuelve un objeto o array nuevo.

## Verificación

- `npx ng build` → compila sin errores.
- `npx ng test --no-watch` → 42 pruebas, no menos. (Misma cifra que al
  empezar; no se introdujeron tests ni se rompió nada.)
