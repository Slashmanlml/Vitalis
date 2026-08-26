# Tarea asignada — Pruebas unitarias del frontend

> Para OpenCode / DeepSeek. Autoconténido: no necesitás más contexto que este
> archivo y `AGENTS.md` en la raíz del proyecto.
>
> Directorio de trabajo: `vitalis-frontend/`

---

## Por qué esta tarea

El backend tiene alrededor de **100 pruebas automatizadas**. El frontend tiene
**5**, y son apenas pruebas de humo. Es la asimetría más visible del proyecto y
la pregunta más fácil que puede hacer un jurado en la defensa.

Tu trabajo cierra esa brecha.

---

## Alcance exacto

**Creá únicamente estos cinco archivos. No modifiques ningún archivo existente.**

```
src/app/utils/jwt.util.spec.ts
src/app/services/turno.service.spec.ts
src/app/services/paciente.service.spec.ts
src/app/services/bloqueo.service.spec.ts
src/app/services/reporte.service.spec.ts
```

Como son todos archivos nuevos, no podés romper nada. Esa es la idea.

---

## 1. `jwt.util.spec.ts` — empezá por acá

Es el más importante y el más simple: **no necesita HTTP**. `jwt.util.ts` es lo
que protege el guard de rutas (decide si dejar entrar a alguien) y hoy no tiene
ni una sola prueba.

Funciones a probar (están en `src/app/utils/jwt.util.ts`):

- `decodeToken(token)` → devuelve los claims, o `null` si el token es inválido.
- `isTokenExpired(token)` → `true` si venció, si no tiene `exp`, o si es inválido.
- `obtenerRolUsuario(claims)`, `obtenerEmailUsuario(claims)`.

Para fabricar un token de prueba, un JWT es `header.payload.firma` con el payload
en base64:

```typescript
function tokenCon(payload: object): string {
  return 'encabezado.' + btoa(JSON.stringify(payload)) + '.firma';
}
```

Casos que tenés que cubrir:

| Caso | Resultado esperado |
|---|---|
| `decodeToken('cualquier-cosa')` | `null` |
| `decodeToken('')` | `null` |
| `decodeToken(tokenCon({exp: 123}))` | objeto con `exp === 123` |
| `isTokenExpired(tokenCon({exp: <hace una hora>}))` | `true` |
| `isTokenExpired(tokenCon({exp: <en una hora>}))` | `false` |
| `isTokenExpired(tokenCon({email: 'a@b.c'}))` (sin `exp`) | `true` |
| `isTokenExpired('roto')` | `true` |

Para las fechas: `exp` va en **segundos**, no milisegundos.
`Math.floor(Date.now() / 1000) - 3600` es "hace una hora".

Ojo con el caso "sin `exp`": es el que importa de verdad. Un token sin fecha de
vencimiento tiene que tratarse como vencido, nunca como válido.

---

## 2. Los cuatro servicios

Todos siguen el mismo molde. Usá `HttpTestingController`, que intercepta las
llamadas sin salir a la red.

```typescript
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TurnoService } from './turno.service';

describe('TurnoService', () => {
  let servicio: TurnoService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    servicio = TestBed.inject(TurnoService);
    http = TestBed.inject(HttpTestingController);
  });

  // Falla el test si quedó alguna llamada sin verificar.
  afterEach(() => http.verify());

  it('obtenerTodos pide la lista de turnos con GET', () => {
    let recibido: any;
    servicio.obtenerTodos().subscribe(d => recibido = d);

    const req = http.expectOne(r => r.url.endsWith('/turnos'));
    expect(req.request.method).toBe('GET');

    req.flush([{ id: 1, pacienteNombre: 'Juan Perez' }]);
    expect(recibido.length).toBe(1);
    expect(recibido[0].pacienteNombre).toBe('Juan Perez');
  });
});
```

Por cada método público de cada servicio, verificá **cuatro cosas**: la URL, el
verbo HTTP, el cuerpo enviado cuando corresponde, y que devuelva lo que el
backend respondió.

### `turno.service.ts`

Métodos: `obtenerTodos`, `obtenerPorId`, `crear`, `editar`, `eliminar`.

- `obtenerPorId(5)` → GET a una URL que termina en `/turnos/5`
- `crear(dto)` → POST, y `req.request.body` tiene que ser el dto
- `editar(3, dto)` → PUT a `/turnos/3`
- `eliminar(3)` → DELETE a `/turnos/3`

### `paciente.service.ts`

Mirá primero el archivo: `obtenerTodos` acepta un parámetro de búsqueda opcional.
Probá **los dos casos**: con búsqueda (el parámetro viaja en la consulta) y sin
búsqueda (no viaja).

```typescript
const req = http.expectOne(r => r.url.endsWith('/pacientes'));
expect(req.request.params.get('buscar')).toBe('perez');
```

### `bloqueo.service.ts` — prestá atención acá

`obtenerImpacto(profesionalId, desde, hasta)` arma tres parámetros de consulta.
Este método alimenta la pantalla que le avisa al usuario cuántos turnos va a
cancelar antes de confirmar, así que si los parámetros viajan mal, el número que
ve el usuario es incorrecto.

Verificá que los tres parámetros lleguen con el valor exacto que se pasó.

También: `obtenerTodos`, `obtenerPorProfesional`, `crear`, `eliminar`.

### `reporte.service.ts` — prestá atención acá también

`turnosPorProfesional(id, desde?, hasta?)` tiene **fechas opcionales**. Probá:

1. Con las dos fechas → ambos parámetros presentes.
2. Sin fechas → **ningún** parámetro de fecha en la consulta.

Ese segundo caso es el que suele romperse: si el código manda `desde=undefined`,
el backend recibe basura.

También: `estadisticas`, `turnosPorPaciente`, `turnosPorObraSocial`.

---

## Cómo saber que terminaste

```bash
cd vitalis-frontend
npx ng test --no-watch
```

Tiene que dar **en verde** y con **25 pruebas como mínimo** (las 5 que ya
existen más las tuyas).

Si alguna prueba de las que ya existían se pone en rojo, el problema es tuyo:
esta tarea es puramente aditiva y no debería afectar a nada.

---

## Recordatorios

- Los nombres de los tests, en español y describiendo la conducta esperada:
  `it('no deja pasar un token sin fecha de vencimiento', ...)`, no
  `it('test 1', ...)`.
- No modifiques ningún archivo que no sea uno de los cinco listados arriba.
- No agregues dependencias nuevas: todo lo necesario ya está instalado.
- Si algo del código real te parece mal, **no lo arregles**: anotalo y avisá. Los
  arreglos los coordina Claude para que no choquen con lo que hacen los otros
  asistentes.
