# Plan de trabajo en paralelo — Vitalis

> Cómo repartir el trabajo entre tres asistentes sobre un mismo repositorio sin
> que se pisen. Coordinación: Claude. Ejecución: Gemini y OpenCode/DeepSeek.
> Fecha: 26/08/2026

---

## 1. Por qué hace falta un reparto explícito

Hoy ya pasó una vez: mientras Claude se preparaba para construir el módulo de
Prescripciones, Gemini **ya lo había construido**. Se detectó a tiempo sólo
porque se revisó el historial de git antes de escribir. Si no, se habría
sobrescrito trabajo terminado.

Con tres asistentes el riesgo se multiplica. La regla que lo elimina es simple:

> **Cada archivo tiene un único dueño en cada momento.** Nadie edita un archivo
> fuera de su territorio, ni siquiera "una línea".

---

## 2. Territorios

### Gemini — Módulo de notificaciones

Su especificación completa está en `docs/10-especificacion-modulo-notificaciones.md`.

**Archivos que le pertenecen:**

```
backend/src/Vitalis.Domain/Entities/EmailLog.cs
backend/src/Vitalis.Domain/Constants/           (constantes nuevas de notificación)
backend/src/Vitalis.Application/Interfaces/IEmailService.cs
backend/src/Vitalis.Application/DTOs/Emails/
backend/src/Vitalis.Infrastructure/Services/EmailService.cs
backend/src/Vitalis.Infrastructure/Notificaciones/   (carpeta nueva)
backend/src/Vitalis.Api/Controllers/EmailLogsController.cs
backend/tests/Vitalis.Tests/EmailServiceTests.cs
backend/tests/Vitalis.Tests/RecordatorioTurnosServiceTests.cs   (nuevo)
vitalis-frontend/src/app/email-logs/
vitalis-frontend/src/app/services/email.service.ts
```

**Además** debe modificar las llamadas a correo dentro de `TurnoService.cs`,
`ConsultaMedicaService.cs`, `PrescripcionService.cs` y `BloqueoAgendaService.cs`.
Son archivos compartidos: ver la sección 4.

---

### OpenCode / DeepSeek — Pantalla de gestión de usuarios

Tarea elegida a propósito para un modelo más liviano: es **CRUD puro, con un
patrón ya resuelto tres veces en el mismo repositorio para copiar**, y con un
criterio de terminado que se verifica solo (compila / no compila).

Su brief completo está en la sección 5.

**Archivos que le pertenecen:**

```
vitalis-frontend/src/app/usuarios/          (carpeta nueva completa)
vitalis-frontend/src/app/models/usuario.model.ts    (nuevo)
vitalis-frontend/src/app/services/usuario.service.ts (nuevo)
```

Todos nuevos: no puede romper nada existente. Es la asignación de menor riesgo
posible.

---

### Claude — Diseño, revisión y archivos compartidos

- Especificaciones y decisiones de arquitectura.
- Revisión del código que entregan los otros dos.
- Verificación: compilación estricta de plantillas, build y tests en un entorno
  propio, antes de dar nada por terminado.
- **Todos los archivos compartidos** (sección 4).
- Panel principal, reportes de facturación, y el documento de la tesina.

---

## 2.bis. Segunda ronda de asignaciones (26/08, tarde)

La primera ronda salió bien: ambos entregaron respetando los territorios y la
lista de aceptación. Verificado por Claude: compilación estricta de plantillas
en cero errores, build limpio y `ng test` en verde con el trabajo de los tres
combinado.

Un solo ajuste: **DeepSeek hizo bien en NO agregar la ruta ni la entrada de
menú** de la pantalla de usuarios, tal como decía la regla. Eso quedaba del lado
de Claude y ya está hecho (ruta `usuarios` y entrada de menú visible sólo para
Administrador, que es el rol que exige `UsuariosController`).

### Gemini — Reportes de facturación y liquidaciones

Los reportes actuales cubren sólo la agenda (turnos). Falta todo el sector
económico, que hoy no tiene un solo indicador.

**Backend** — nuevo `IReporteFacturacionService` + su controlador, con:

- Facturación total por período, y desglose por obra social.
- Cobranzas: facturado vs. cobrado vs. pendiente. Ojo con `FacturaService`, que
  ya tuvo un defecto de doble conteo en el cálculo de pagos: los tests deben
  cubrir el caso de una factura con varios pagos parciales.
- Liquidaciones por profesional en un período.
- Un DTO tipado por reporte. **No devolver `object`** — ese error ya se cometió
  en `ReporteService` y obligó a que los tests leyeran el resultado por
  reflexión.

**Frontend** — pestaña nueva dentro de la pantalla de Reportes ya existente,
no una pantalla aparte.

**Reglas de visualización, no negociables:**

- Barras de un solo tono para comparar magnitudes. La longitud ya codifica el
  valor; teñir cada barra de un color distinto gasta el canal de identidad en
  repetir esa información.
- Nunca comunicar un estado sólo por color: siempre insignia con texto.
- Los importes en color de texto, jamás en el color de la serie.
- Cero hexadecimales en el CSS.

**Territorio:** `Vitalis.Application/DTOs/Reportes/`, el servicio y controlador
nuevos, sus tests, y `vitalis-frontend/src/app/reportes/`. **No tocar**
`ReporteService.cs` ni `reporte.service.ts` (los de turnos), que son de Claude.

### DeepSeek — Pruebas unitarias del frontend

Hoy el backend tiene ~100 pruebas y el frontend **5**. Es la asimetría más
visible del proyecto y la más fácil de preguntar en una defensa.

Escribir archivos `.spec.ts` para los servicios, **sólo archivos nuevos**:

```
src/app/services/turno.service.spec.ts
src/app/services/paciente.service.spec.ts
src/app/services/reporte.service.spec.ts
src/app/services/bloqueo.service.spec.ts
src/app/utils/jwt.util.spec.ts
```

Usar `provideHttpClient()` + `provideHttpClientTesting()` y `HttpTestingController`
para verificar, en cada servicio: que pega a la URL correcta, con el verbo
correcto, que arma bien los parámetros de consulta, y que devuelve lo que el
backend responde. Copiar la forma de `src/app/login/login.spec.ts`.

Para `jwt.util.spec.ts` no hace falta HTTP: probar `decodeToken` con un token
inválido (debe devolver `null`) y `isTokenExpired` con un `exp` vencido y con uno
futuro. Es la pieza que protege el guard de rutas y hoy no tiene ninguna prueba.

**Territorio:** únicamente archivos `.spec.ts` nuevos. No modificar ningún
archivo existente. Criterio de terminado: `npx ng test` en verde con al menos
25 pruebas.

---

## 3. Estado actual del reparto

| Módulo | Dueño | Estado |
|---|---|---|
| Notificaciones por correo | Gemini | Especificado, sin empezar |
| Pantalla de usuarios | DeepSeek | Especificado, sin empezar |
| Bloqueo de agenda | Claude | **Terminado**, falta `dotnet test` |
| Reportes | Claude | Terminado |
| Agenda / calendario | Claude | Terminado |
| Prescripciones | Gemini | Terminado |
| Panel principal | Claude | Pendiente |
| Documento de la tesina | Claude | Pendiente |

---

## 4. Archivos compartidos — los toca **sólo Claude**

Estos cuatro son los que van a generar conflicto si más de uno los edita,
porque todos los módulos necesitan agregarles una línea:

```
vitalis-frontend/src/app/app.config.ts         (rutas)
vitalis-frontend/src/app/dashboard/dashboard.html  (menú lateral)
vitalis-frontend/src/app/dashboard/dashboard.ts    (permisos del menú por rol)
vitalis-frontend/src/styles.css                (tokens del sistema de diseño)
```

**Protocolo:** el asistente que necesite una ruta nueva o una entrada de menú
**no la agrega**. Deja anotado qué necesita (ruta, componente, roles que deben
verla) y Claude hace la edición.

Lo mismo para `backend/src/Vitalis.Infrastructure/DependencyInjection.cs` y
`Program.cs`: quien necesite registrar un servicio lo pide, no lo edita.

Sobre los cuatro servicios que Gemini debe modificar (`TurnoService`,
`ConsultaMedicaService`, `PrescripcionService`, `BloqueoAgendaService`): son
suyos **sólo para las llamadas a correo**. La lógica de negocio de esos archivos
no se toca. En particular `BloqueoAgendaService` acaba de recibir cambios de
Claude (previsualización de impacto) que no deben deshacerse.

---

## 5. Brief para OpenCode / DeepSeek — Pantalla de gestión de usuarios

### Contexto

El backend ya tiene `UsuariosController` completo y funcionando, con cinco
endpoints, pero **ningún componente Angular los consume**. Hay que construir esa
pantalla. No hay que tocar el backend.

Endpoints disponibles (todos exigen rol `Administrador`):

```
GET    /api/usuarios?buscar=  -> lista, con búsqueda opcional por texto
GET    /api/usuarios/{id}     -> uno
POST   /api/usuarios          -> crear
PUT    /api/usuarios/{id}     -> editar
DELETE /api/usuarios/{id}     -> DESACTIVA (baja lógica, no borra la fila)
```

### Contratos exactos — respetarlos al pie de la letra

```typescript
// Lo que devuelve el backend
export interface Usuario {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  rol: string;
  activo: boolean;
}

// POST /api/usuarios — todos los campos son obligatorios
export interface CrearUsuario {
  nombre: string;
  apellido: string;
  email: string;
  password: string;
  rol: string;
}

// PUT /api/usuarios/{id} — todos opcionales, y OJO: NO lleva password
export interface EditarUsuario {
  nombre?: string;
  apellido?: string;
  email?: string;
  rol?: string;
}
```

**Tres detalles que cambian el diseño de la pantalla:**

1. **La contraseña sólo existe en el alta.** `EditarUsuarioDto` no tiene campo
   `password`: desde esta pantalla no se puede cambiar la contraseña de nadie.
   Por lo tanto el modal de edición **no lleva campo de contraseña**, y no hay que
   inventarle uno.
2. **El borrado es una baja lógica.** El endpoint se llama `Desactivar` y sólo
   pone `Activo = false`. El botón debe decir **"Desactivar"**, no "Eliminar", y
   el texto de confirmación tiene que reflejar eso.
3. **`activo` se muestra.** La tabla lleva una columna de estado
   (Activo / Inactivo) usando el patrón de insignias del proyecto.

Roles válidos: los que define `backend/src/Vitalis.Domain/Constants/Roles.cs`
(`Administrador`, `Medico`, `Recepcionista`, `Facturacion`). Cargarlos como
opciones fijas de un `<select>`, no como texto libre.

### Qué copiar

**Copiar la estructura de `vitalis-frontend/src/app/obras-sociales/`**
(`obras-sociales.ts`, `.html`, `.css`). Es el CRUD más simple del proyecto y
tiene exactamente la forma que se busca: lista en tabla, botón de alta, modal de
alta/edición, y borrado con confirmación.

**Copiar `vitalis-frontend/src/app/services/obra-social.service.ts`** como molde
para `usuario.service.ts`.

### Reglas que no se pueden romper

1. **Cero colores hardcodeados en el CSS.** Nada de `#4f46e5`, `#64748b`, `white`
   ni similares. Todo sale de las variables del sistema:
   `var(--color-primary)`, `var(--text-primary)`, `var(--text-secondary)`,
   `var(--text-muted)`, `var(--bg-card)`, `var(--bg-secondary)`,
   `var(--border-color)`, `var(--radius-md)`, `var(--shadow-sm)`.
   Ya se barrieron 403 valores pegados a mano en el proyecto justamente para esto;
   no volver a introducirlos. Verificación: `grep -E "#[0-9a-fA-F]{3,6}" usuarios.css`
   debe dar cero resultados.

2. **No mostrar toasts de error en los `subscribe`.** Existe un
   `ErrorInterceptor` global que ya muestra el mensaje del backend en cualquier
   error HTTP. Agregar otro produce dos notificaciones por el mismo error. En el
   `error:` va sólo `console.error(...)`. Los toasts de **éxito** sí van.

3. **La contraseña sólo aparece en el alta.** El modal de edición no lleva ese
   campo (el backend no lo acepta). Nunca mostrar hashes ni contraseñas en la
   tabla.

4. **No tocar ningún archivo fuera de estos tres:**
   `app/usuarios/usuarios.{ts,html,css}`, `app/models/usuario.model.ts`,
   `app/services/usuario.service.ts`.
   La ruta y la entrada de menú las agrega Claude: **no editar `app.config.ts`
   ni `dashboard.html`.**

5. **Componente standalone**, con `imports: [CommonModule, FormsModule]`, igual
   que el resto del proyecto.

### Definición de terminado

- [ ] `npx ng build` compila sin errores.
- [ ] La tabla lista usuarios con nombre, apellido, email, rol y estado.
- [ ] El alta crea un usuario (con contraseña) y la lista se refresca.
- [ ] La edición funciona **sin** campo de contraseña.
- [ ] "Desactivar" pide confirmación y aclara que es una baja lógica.
- [ ] El rol se elige de un `<select>` con los cuatro roles válidos.
- [ ] `grep -E "#[0-9a-fA-F]{3,6}" usuarios.css` → cero resultados.
- [ ] La pantalla se ve igual en tema claro y oscuro (los tokens lo resuelven solos
      si se respetó la regla 1).

### Lo que hay que reportar al terminar

Una línea con: qué ruta necesita (`usuarios`), qué componente
(`UsuariosComponent`) y qué roles deben ver la entrada de menú (`Administrador`).
Claude hace esa edición.

---

## 6. Protocolo de trabajo

1. **Antes de escribir, mirar el historial.** `git log --oneline -10` y
   `git status`. Si el módulo que se va a construir ya existe, avisar en vez de
   sobrescribirlo.

2. **Commits chicos y frecuentes**, uno por unidad de trabajo terminada. Un
   commit gigante al final es lo que hace imposible resolver un conflicto.

3. **Nunca reescribir un archivo entero que no es propio.** Si hay que cambiar
   una línea en territorio ajeno, se pide.

4. **No commitear archivos temporales.** Ya se coló un `.tgz` de 165 KB en el
   historial. Antes de commitear, revisar `git status`.

5. **Verificación antes de dar por terminado:**
   - Backend: `dotnet test tests\Vitalis.Tests` — hoy en **79**, no puede bajar.
   - Frontend: `ng build` y `ng test`.

---

## 7. Qué NO delegar

Estas quedan para Claude, porque un error acá no lo detecta un compilador:

- **Decisiones de arquitectura y de modelo de datos.** Los cuatro defectos de
  `ReporteService` (estado nunca asignado, apellido faltante, agrupación por
  profesional en vez de por especialidad, retorno sin tipar) pasaron todos los
  builds y todos los tests durante meses. Compilaban perfecto y estaban mal.
- **Criterios de seguridad y de datos sensibles.** Qué se manda por correo, qué
  se registra en auditoría, qué se puede borrar.
- **Accesibilidad de color.** La primera paleta de los gráficos de reportes
  compilaba y se veía bien, y era indistinguible para daltonismo rojo-verde. Eso
  se detecta corriendo un validador, no leyendo el código.
- **El documento de la tesina.** Es lo único que efectivamente se entrega.
