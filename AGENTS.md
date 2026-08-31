# Vitalis — Instrucciones para asistentes de código

Este archivo lo leen automáticamente las herramientas de asistencia (OpenCode y
similares). Si estás leyendo esto, **empezá por acá antes de escribir una sola
línea**.

---

## Qué es este proyecto

Vitalis es un sistema de gestión para consultorio médico. Es el **Trabajo Final
de Carrera** de José Machado (Tecnicatura Superior en Análisis y Diseño de
Sistemas). No es un proyecto de práctica: se entrega y se defiende ante un
jurado.

- **Backend:** ASP.NET Core sobre .NET 8, PostgreSQL, Entity Framework Core.
  Carpeta `backend/`.
- **Frontend:** Angular 22 con componentes standalone. Carpeta `vitalis-frontend/`.

---

## Regla de oro: cada archivo tiene un solo dueño

Trabajan tres asistentes en paralelo sobre este mismo repositorio. Ya pasó una
vez que dos construyeron el mismo módulo y casi se sobrescribe trabajo terminado.

**Antes de escribir, mirá qué hay:**

```bash
git log --oneline -10
git status
```

Si el módulo que ibas a construir ya existe, **avisá en vez de sobrescribirlo.**

### Archivos compartidos — no los toques

Estos los edita únicamente Claude, porque todos los módulos necesitan agregarles
una línea y es donde se producen los conflictos:

```
vitalis-frontend/src/app/app.config.ts              (rutas)
vitalis-frontend/src/app/dashboard/dashboard.html   (menú lateral)
vitalis-frontend/src/app/dashboard/dashboard.ts     (permisos del menú)
vitalis-frontend/src/styles.css                     (tokens de diseño)
vitalis-frontend/src/app/utils/permisos.ts          (matriz de permisos de la UI)
vitalis-frontend/src/app/utils/jwt.util.ts          (lectura del token)
backend/src/Vitalis.Infrastructure/DependencyInjection.cs
backend/src/Vitalis.Api/Program.cs
```

¿Necesitás una ruta nueva o una entrada de menú? **No la agregues.** Anotá qué
hace falta (ruta, componente, roles que deben verla) y avisá. Claude la agrega.

---

## Reglas de estilo que no se negocian

Estas salieron de errores reales que ya se corrigieron en este proyecto. No las
reintroduzcas.

**1. Cero colores hardcodeados en CSS.** Nada de `#4f46e5`, `#64748b`, `white`.
Todo sale de las variables de `styles.css`:

```
var(--color-primary)  var(--color-primary-hover)  var(--color-primary-bg)
var(--text-primary)   var(--text-secondary)       var(--text-muted)
var(--bg-card)        var(--bg-secondary)         var(--border-color)
var(--color-success)  var(--color-warning)        var(--color-danger)
var(--radius-sm)      var(--radius-md)            var(--shadow-sm)
```

Se barrieron 463 valores pegados a mano justamente para esto, y de paso se
arregló el modo oscuro, que sólo andaba en 4 de 19 pantallas. Verificación:
`grep -E "#[0-9a-fA-F]{3,6}" tu-archivo.css` debe dar cero.

**La excepción, y es importante:** lo que se imprime no usa los tokens del tema
sino los de **papel** (`--paper-bg`, `--paper-text`, `--paper-accent`, y los
demás del bloque PAPEL en `styles.css`). Una receta representa una hoja, y el
papel es blanco también de noche. `PrintService` abre la ventana de impresión
copiando las hojas de estilo de la aplicación, así que un `var(--bg-card)` ahí
adentro hereda el modo oscuro del sistema e imprime una hoja negra. Pasó: un
barrido de colores reemplazó los `#fff` de `.receta-print-box`, que estaban
puestos a propósito, y rompió la impresión sin que ningún comando lo detectara.
Los tokens de papel **no tienen variante oscura**. No se la agregues.

**2. No pongas toasts de error en los `subscribe`.** Existe un `ErrorInterceptor`
global que ya muestra el mensaje del backend ante cualquier error HTTP. Si
agregás otro, el usuario ve dos notificaciones por el mismo error. En el `error:`
va sólo `console.error(...)`. Los toasts de **éxito** sí van.

**3. Nunca comuniques un estado sólo por color.** Siempre insignia con texto. Una
paleta de gráficos se descartó en este proyecto porque el verde y el ámbar
quedaban indistinguibles bajo daltonismo rojo-verde.

**4. Nunca inventes un destinatario ni un dato por defecto.** Había seis lugares
con `paciente@vitalis.local` como respaldo; con envío real de correo eso escribe
a un dominio inexistente. Si el dato no está, no se opera.

**5. Componentes standalone**, con `imports: [CommonModule, FormsModule]`, igual
que el resto del proyecto.

**6. La identidad del que opera sale del token, nunca del cuerpo del pedido.** Un
médico podía registrar consultas sobre turnos ajenos porque el servicio guardaba
el `ProfesionalId` que mandaba el navegador. La regla que salió de ahí: si un
dato identifica a quién opera o sobre qué opera, se toma de la entidad que está
en la base (el turno, la consulta) o de `IUsuarioActual`, y el campo equivalente
del DTO se ignora. Validar el dato del cliente no alcanza: hay que no usarlo.

**7bis. La interfaz no ofrece lo que el backend niega.** Cinco pantallas de
catálogo mostraban los botones de Nuevo, Editar y Eliminar a todos los roles. El
backend los rechazaba con 403, así que no era un problema de seguridad — era
peor en otro sentido: el usuario completaba un formulario y recibía un error, y
concluía que el sistema estaba roto cuando funcionaba bien. Los permisos de la
interfaz salen de `utils/permisos.ts`, nunca de un `rolUsuario === '...'`
repetido en cada componente. La autoridad sigue siendo el backend.

**7. Filtrar por rol en el navegador no es filtrar.** `turnos.ts` pedía la agenda
completa de la clínica y escondía lo ajeno con un `.filter()`. Los datos igual
viajaban y se veían con F12. Lo que un rol no puede ver, no sale del servidor.

---

## Verificación antes de decir "terminé"

No alcanza con que compile. Corré esto:

```bash
# Backend
cd backend
dotnet test tests\Vitalis.Tests

# Frontend
cd vitalis-frontend
npx ng build
npx ng test --no-watch
```

El número de pruebas del backend **no puede bajar**. Si tu cambio rompe una
prueba existente, el problema es tu cambio, no la prueba.

---

## Tu tarea actual

| Asistente | Tarea | Dónde está la especificación |
|---|---|---|
| **Gemini** | Módulo de Pacientes: ficha con pestañas por rol | **`docs/19-tareas-quinta-ronda.md`** |
| **OpenCode / DeepSeek** | Modo lectura en las cinco pantallas de catálogo | **`docs/19-tareas-quinta-ronda.md`** |
| **Claude** | Backend de permisos, menú, documento de la tesina | — |

**Abrí `docs/19-tareas-quinta-ronda.md`** y buscá la sección con tu nombre. Está
todo el detalle ahí.

Rondas anteriores, ya terminadas y verificadas: `docs/10` (notificaciones por
correo), `docs/11` (pantalla de usuarios y reportes de facturación), `docs/12`
(pruebas unitarias del frontend), `docs/13` (sala de espera y datos clínicos).

---

## No hagas esto sin consultar

- Cambiar el modelo de datos o crear migraciones.
- Decidir qué datos se envían por correo o se registran en auditoría.
- Borrar archivos o registros.
- Tocar `Vitalis_Tesina.docx`.
