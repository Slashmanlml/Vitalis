# Quinta ronda — observaciones del profesor auditor

> Buscá tu nombre. Tenés tus propios archivos y **nadie más los toca**.
>
> Antes de escribir una línea, leé `AGENTS.md` en la raíz del repositorio.

**Estado al arrancar:** 120 pruebas de backend y 37 de frontend, todas en verde,
compilación sin advertencias. El número no puede bajar.

**El backend de esta ronda ya está hecho** (lo hizo Claude): matriz de permisos,
el administrador fuera del contenido clínico, cada médico bloqueando solo su
propia agenda, y el JSON clínico oculto en auditorías. Lo que sigue es frontend.

---

## Contexto: qué observó el profesor

Cinco observaciones, todas sobre lo mismo — **confidencialidad y permisos**:

1. Solo el administrador carga médicos y especialidades; los demás roles, en
   modo lectura.
2. Un médico no puede dar de alta a otros profesionales.
3. Cada médico bloquea su propia agenda, no la de otro.
4. Recepción no ve estudios ni diagnósticos, **y el administrador tampoco ve las
   historias clínicas**.
5. Sacar Profesionales del menú del médico; Obras Sociales y Especialidades en
   modo lectura.

Y una sexta, de alcance distinto: **un módulo de Pacientes completo**, bien
pensado en lógica y en estética.

---

## OpenCode / DeepSeek — modo lectura en los catálogos

### El problema, y qué NO es

Cinco pantallas muestran los botones de **Nuevo**, **Editar** y **Eliminar** a
todos los roles. Ninguna de las cinco sabe qué rol tiene el usuario.

**El backend ya está bien**: cada endpoint de escritura tiene su
`[Authorize(Roles = ...)]` y responde 403. Un médico que apriete "Nuevo
profesional" no crea nada.

Entonces esto **no es un agujero de seguridad**. Es peor en otro sentido: la
interfaz ofrece algo que después niega. El usuario completa un formulario, lo
guarda y recibe un error — y concluye que el sistema está roto, cuando en
realidad está funcionando exactamente como debe.

**Lo que hay que arreglar es la honestidad de la interfaz, no la seguridad.**

### Tus archivos

Estos y ningún otro:

```
vitalis-frontend/src/app/profesionales/profesionales.{ts,html}
vitalis-frontend/src/app/especialidades/especialidades.{ts,html}
vitalis-frontend/src/app/obras-sociales/obras-sociales.{ts,html}
vitalis-frontend/src/app/medicamentos/medicamentos.{ts,html}
vitalis-frontend/src/app/prestaciones/prestaciones.{ts,html}
```

Los `.css` de esas carpetas también son tuyos **si necesitás agregar el estilo
del aviso de solo lectura**. Nada más.

### La herramienta ya existe: usala, no la reescribas

Está creado `vitalis-frontend/src/app/utils/permisos.ts`. **No lo modifiques**:
solo importalo.

```typescript
import { puedeEditar } from '../utils/permisos';

export class ProfesionalesComponent implements OnInit {
  puedeEditar = puedeEditar('profesionales');
  // ...
}
```

Y en la plantilla:

```html
<button *ngIf="puedeEditar" class="btn-primary" (click)="abrirModal()">
  Nuevo profesional
</button>
```

El identificador de cada pantalla es exactamente este:

| Pantalla | Identificador | Quién puede editar |
|---|---|---|
| Profesionales | `'profesionales'` | Administrador |
| Especialidades | `'especialidades'` | Administrador |
| Obras Sociales | `'obras-sociales'` | Administrador |
| Medicamentos | `'medicamentos'` | Administrador |
| Prestaciones | `'prestaciones'` | Administrador y Facturación |

**No inventes la lógica de roles en cada componente.** Si escribís
`rolUsuario === 'Administrador'` cinco veces, el día que cambie un permiso hay
que acordarse de cinco lugares. Una sola función, cinco llamadas.

### Qué hacer en cada pantalla

1. **Ocultar** el botón de crear cuando `puedeEditar` es falso.
2. **Ocultar** los botones de editar y eliminar de cada fila.
3. Si la columna de acciones queda vacía, **ocultar la columna entera** —
   encabezado incluido. Una columna vacía se ve como un error de maquetado.
4. Agregar, arriba del listado y solo en modo lectura, un aviso discreto:

   > Solo lectura. La gestión de este catálogo corresponde a administración.

   Con los tokens del proyecto, nada de colores escritos a mano. Discreto: un
   texto en `var(--text-muted)` con un borde suave alcanza. **No** un cartel
   rojo de error: no es un error, es cómo tiene que ser.

5. Si la pantalla abre un formulario al hacer clic en una fila, que en modo
   lectura **abra igual pero con los campos deshabilitados**, o que no abra. Las
   dos son válidas; elegí una y hacela igual en las cinco.

### Cuándo terminaste

```bash
cd vitalis-frontend
npx ng build
npx ng test --no-watch     # 37, no menos
grep -rhoE "#[0-9a-fA-F]{3,6}" src/app --include=*.css | wc -l   # tiene que dar 0
```

Y probalo a mano, que es lo único que detecta esto: entrá con
`lmartinez@vitalis.local` / `Medico123!` y recorré Medicamentos, Obras Sociales
y Especialidades. **No tenés que poder apretar ningún botón que cree o borre
nada.** Después entrá como `admin@vitalis.local` / `Admin123!` y confirmá que
sigue pudiendo todo.

### No hagas

- No toques `permisos.ts`, `styles.css`, `app.config.ts` ni `dashboard.*`.
- No toques ningún archivo del backend.
- No cambies la lógica de negocio de las pantallas: **solo visibilidad**.

---

## Gemini — módulo de Pacientes: la ficha

### Qué hay que construir

Hoy Pacientes es un listado con un formulario de alta. Falta lo importante:
**abrir un paciente y ver todo lo suyo en un solo lugar**. Es la pantalla que
ata el sistema, porque es donde confluyen turnos, historia clínica y recetas.

Ya está creado el esqueleto y **la ruta ya funciona**:

```
vitalis-frontend/src/app/pacientes/paciente-ficha.{ts,html,css}
ruta: /dashboard/pacientes/:id
```

Andá a `http://localhost:4200/dashboard/pacientes/1` y vas a ver el marcador de
posición. Tu tarea es llenarlo.

### Tus archivos

```
vitalis-frontend/src/app/pacientes/paciente-ficha.{ts,html,css}
vitalis-frontend/src/app/pacientes/pacientes.{ts,html,css}
```

Los cuatro primeros son nuevos y son enteramente tuyos. En `pacientes.*` lo
único que hay que agregar es **la forma de llegar a la ficha**: que al hacer clic
en una fila se navegue a `/dashboard/pacientes/:id`.

### La estructura: encabezado y cuatro pestañas

**Encabezado fijo**, visible siempre: nombre y apellido, DNI, edad calculada a
partir de la fecha de nacimiento, obra social con número de afiliado, y un botón
para volver al listado. Si el paciente no tiene obra social, decir **"Particular"**
y no dejar el espacio vacío.

**Pestaña 1 — Datos personales.** Los datos de contacto y demográficos, con la
posibilidad de editarlos. Reutilizá el formulario que ya existe en
`pacientes.ts`: no escribas otro.

**Pestaña 2 — Turnos.** Los turnos de ese paciente, más recientes primero, con
fecha, profesional, especialidad y estado. El estado va como insignia **con
texto**, nunca solo por color. Se usa `turnoService.obtenerTodos()` filtrando por
paciente.

**Pestaña 3 — Historia clínica.** Las consultas del paciente: fecha, profesional,
motivo, diagnóstico, evolución e indicaciones. Se usa
`consultaMedicaService.obtenerPorPaciente(id)`.

**Pestaña 4 — Recetas.** Las prescripciones con sus medicamentos. Se usa
`prescripcionService.obtenerPorPaciente(id)`.

### Lo que no se negocia: cada pestaña respeta el rol

Esto es el corazón de la observación del profesor y es lo que hay que hacer bien.

| Pestaña | Administrador | Médico | Recepción | Facturación |
|---|:--:|:--:|:--:|:--:|
| Datos personales | ✅ | ✅ | ✅ | ✅ |
| Turnos | ✅ | ✅ | ✅ | ✅ |
| **Historia clínica** | ❌ | ✅ | ❌ | ❌ |
| **Recetas** | ❌ | ✅ | ❌ | ❌ |

Las dos pestañas clínicas **no se muestran deshabilitadas ni con un cartel de
"no tenés permiso": directamente no existen** para esos roles. Una pestaña gris
que no se puede abrir sigue informando que ahí hay algo.

Usá `esRolMedico()` a partir de `rolActual()` de `../utils/permisos`. Si igual se
pidieran esos datos, el backend responde **403**: los endpoints de consultas y
prescripciones son ahora exclusivos del rol Médico.

### Criterios de estética

- **Todo de `styles.css`.** Cero colores escritos a mano. Verificación:
  `grep -E "#[0-9a-fA-F]{3,6}" paciente-ficha.css` debe dar cero.
- **Ningún estado comunicado solo por color.** Insignia con texto, siempre.
- **Los estados vacíos importan tanto como los llenos.** Un paciente sin
  consultas no debe mostrar una tabla vacía: un mensaje breve que diga qué
  significa ("Sin consultas registradas") y, si corresponde, qué se puede hacer.
  Son los estados que más se ven en una demo con pocos datos.
- **Nada de `text-transform: capitalize` en fechas ni nombres.** Produce
  "26 De Agosto". Si hace falta capitalizar, se hace en el TypeScript con
  `charAt(0).toUpperCase()`.
- **Componente standalone**, con `imports: [CommonModule, FormsModule]`.
- **Sin toasts de error en los `subscribe`**: existe un `ErrorInterceptor`
  global. En el `error:` va solo `console.error(...)`. Los toasts de éxito sí.

### Cuándo terminaste

```bash
cd vitalis-frontend
npx ng build
npx ng test --no-watch
grep -rhoE "#[0-9a-fA-F]{3,6}" src/app/pacientes --include=*.css | wc -l   # 0
```

Y a mano, que es lo que importa: entrá con **cada uno de los tres usuarios** y
abrí la ficha del mismo paciente.

- Con `lmartinez@vitalis.local` tienen que verse las cuatro pestañas.
- Con `admin@vitalis.local` y con `recepcion@vitalis.local`, **solo dos**.

### No hagas

- No toques `app.config.ts` (la ruta ya está), `dashboard.*`, `styles.css` ni
  `permisos.ts`.
- No toques el backend.
- No crees servicios nuevos: los cuatro que necesitás ya existen en
  `src/app/services/`.

---

## Claude — lo que queda de mi lado

- Backend de esta ronda: **hecho** (120 pruebas en verde)
- Menú lateral y rutas: **hecho**
- La pantalla de Auditorías tiene que mostrar el aviso de contenido clínico
  oculto (el backend ya envía `contenidoClinicoOculto`)
- Documento de la tesina: incorporar esta ronda a la sección de seguridad
- Archivos compartidos: `styles.css`, `app.config.ts`, `dashboard.*`,
  `permisos.ts`, `jwt.util.ts`, `Program.cs`, `DependencyInjection.cs`,
  `VitalisDbContext.cs`

---

## Si te chocás con otro

```bash
git log --oneline -10
git status
```

Si el archivo que ibas a tocar ya lo modificó otro, **pará y avisá**.
