# Tareas

> Actualizado tras la revisión técnica y el hardening de seguridad/datos, y luego
> reconciliado con [docs/Plan_de_Desarrollo_Vitalis.docx](docs/Plan_de_Desarrollo_Vitalis.docx)
> (semana 1 del cronograma). Ver [docs/07-estado-actual-del-sistema.md](docs/07-estado-actual-del-sistema.md)
> para el detalle de qué se corrigió y por qué.

## Hecho

- [x] Add Serilog packages (AspNetCore, Console, File)
- [x] Configure Serilog in Program.cs
- [x] Add .editorconfig
- [x] Add data-annotation validation to DTOs
- [x] Enable EF Core automatic migrations on startup
- [x] Create test project Vitalis.Tests with xUnit + InMemory provider
- [x] Write unit tests for PacienteService, TurnoService, AuthService, FacturaService
- [x] Write integration test for PacientesController (incluye regresión de autorización)
- [x] Run all builds and tests, verify Swagger UI
- [x] Exigir autenticación/rol en los 20 controladores (antes: Pacientes y Uploads eran anónimos)
- [x] Auditoría: tomar el usuario autenticado real en vez de un valor fijo
- [x] Corregir fechas rechazadas por PostgreSQL (Pacientes, Turnos, Bloqueos, Liquidaciones)
- [x] Corregir doble conteo de pagos en FacturaService.RegistrarPagoAsync
- [x] Corregir mensajes de error genéricos en el interceptor HTTP del frontend
- [x] Centralizar URL de API en environment.ts (antes duplicada en 17 servicios)
- [x] Eliminar código muerto (Program.cs, app.routes.ts sin usar)
- [x] Actualizar .gitignore (Logs/) y hacer el primer commit del repositorio
- [x] Diagrama de arquitectura y modelo entidad-relación para la tesina
      (docs/diagrams/, insertados como Figuras 1 y 2 en Vitalis_Tesina.docx)
- [x] Redactar en la tesina la justificación del stack (PostgreSQL vs. pedido de
      frameworks Microsoft del profesor) — sección 4.7 de Vitalis_Tesina.docx
- [x] Guion de demo en vivo + plan B para la defensa — docs/09-guion-demo-y-defensa.md
- [x] Reparar Vitalis_Tesina.docx (tenía 7 tags XML inválidos que impedían abrirlo
      correctamente / disparaban el aviso de "reparar documento" en Word)
- [x] Ocultar en el menú del frontend las secciones que el rol del usuario no puede usar.
      Se corrigieron además varios casos donde el menú estaba mal alineado con la matriz
      real de autorización del backend: Pacientes no se mostraba a Médico/Facturación
      (que sí tienen acceso), Bloqueo de Agenda no se mostraba a Recepcionista (que sí
      tiene acceso), y Reportes / Simulación Mails se mostraban a Recepcionista aunque el
      backend le responde 403. Turnos y Sala de Espera, antes visibles para cualquier rol,
      ahora se ocultan para Facturación. Ver dashboard.html / dashboard.ts.
- [x] Validar expiración de token en el guard de rutas, no solo su presencia
      (app/utils/jwt.util.ts + auth.guard.ts, a partir del claim `exp` del JWT)
- [x] Eliminar código muerto residual: app/services/auth.ts y auth.spec.ts (stub generado
      por Angular CLI, nunca usado — el servicio real es auth.service.ts). Movidos a
      app/services/_to_delete/ para que los borres manualmente.
- [x] Tests de LiquidacionService (8 casos: tarifa por obra social, suma con obras
      sociales distintas, tarifa "particular" por defecto, filtro por período, filtro por
      profesional, exclusión de turnos no atendidos, error si el profesional no existe,
      transición a "Liquidada"). backend/tests/Vitalis.Tests/LiquidacionServiceTests.cs.
      Confirmado con `dotnet test`: 21/21 en verde (13 previos + 8 nuevos).
- [x] Tests de ConsultaMedicaService (10 casos: alta con nombres de paciente/profesional,
      marcar el turno de origen como "Atendido", ordenar historial por fecha descendente,
      editar campos clínicos, antecedentes filtrados por paciente, alergias activas vs.
      inactivas). backend/tests/Vitalis.Tests/ConsultaMedicaServiceTests.cs. Confirmado
      por Tito: 31/31 en verde (21 previos + 10 nuevos).
      (Hallazgo de esa semana —no validaba que TurnoId/PacienteId/ProfesionalId existan—
      corregido más adelante, ver más abajo.)
- [x] Tests de PrescripcionService (5 casos: alta con un medicamento, alta con varios
      medicamentos en la misma prescripción, orden del historial por fecha, filtro por
      paciente, id inexistente). backend/tests/Vitalis.Tests/PrescripcionServiceTests.cs.
- [x] Tests de BloqueoAgendaService (10 casos: validación de fechas —inicio/fin y no en el
      pasado—, profesional inexistente, cancelación en cascada de turnos superpuestos con
      notificación al paciente, turnos fuera de rango no afectados, turnos ya cancelados no
      vuelven a notificarse, eliminar bloqueo, y el chequeo de superposición horaria
      EsHorarioBloqueadoAsync). backend/tests/Vitalis.Tests/BloqueoAgendaServiceTests.cs.
      Se agregó un `RecordingEmailService` (fake de IEmailService) para poder verificar que
      se notifica exactamente a los pacientes correctos, no solo que no explota.
- [x] Corregido un fallo real detectado por Tito al correr `dotnet test`: en
      ConsultaMedicaServiceTests, el test de orden por fecha usaba el mismo TurnoId en dos
      consultas. `Turno.ConsultaMedica` es una relación uno a uno (no una lista) — un turno
      admite como máximo una consulta asociada — así que EF Core rompía la asociación
      anterior y fallaba. Corregido usando un TurnoId distinto por consulta. 45/46 pasaron
      en la primera corrida; falta confirmar que el fix deja los 46/46 (ver Pendiente,
      ahora se confirma junto con los 13 tests nuevos de esta semana).
- [x] Tests de ReporteService (5 casos: turnos por profesional, filtro por rango de
      fechas, turnos por paciente, turnos por obra social, estadísticas generales —total,
      confirmados, pendientes—). backend/tests/Vitalis.Tests/ReporteServiceTests.cs.
- [x] Tests de SearchService (8 casos: paciente por nombre, paciente por DNI, profesional
      por apellido, profesional por matrícula, turno por nombre de paciente, búsqueda
      case-insensitive, sin coincidencias, y el límite de 5 resultados por categoría —
      Take(5) en el servicio). backend/tests/Vitalis.Tests/SearchServiceTests.cs.
      Con esto quedan cubiertos los 6 servicios que no tenían tests (Liquidacion,
      ConsultaMedica, Prescripcion, BloqueoAgenda, Reporte, Search). Total esperado:
      59/59 (46 previos + 13 nuevos). Falta que Tito confirme con `dotnet test`.
- [x] Se investigó cómo integrar herramientas para que Claude pueda autoverificar el
      backend sin depender de que Tito ejecute y pegue el resultado de `dotnet test`.
      Se instaló .NET 8 SDK (8.0.130) en el sandbox de esta sesión y se clonó el repo real
      de GitHub (https://github.com/Slashmanlml/vitalis.git) — pero `dotnet restore`
      falla ahí: el acceso de red del sandbox está en lista blanca y NO incluye
      api.nuget.org (sí incluye, por ejemplo, npmjs.org y pypi.org). Es un límite de
      infraestructura, no algo resoluble con configuración de NuGet. Conclusión: por ahora
      seguimos con el flujo de "Claude revisa y ajusta el código, Tito corre `dotnet test`
      y pega el resultado" — ya demostró que funciona (detectó y permitió corregir el bug
      de la relación 1:1 de ConsultaMedica).
- [x] Se generó un snapshot completo del repositorio con `repomix` (corrido en el sandbox
      de Claude, ya que el dispositivo de Tito no tiene acceso de red para bajarlo) a
      partir de un tar del proyecto actual (sin node_modules/bin/obj/.git). 285 archivos,
      ~254k tokens. Le da a Claude contexto directo del código sin tener que pedir
      archivo por archivo — se puede repetir cuando haga falta una foto actualizada.
- [x] Corregido el hallazgo pendiente de la semana 3: ConsultaMedicaService.CrearAsync y
      PrescripcionService.CrearAsync ahora validan que Turno/Paciente/Profesional (y, en
      Prescripcion, cada Medicamento del detalle) existan antes de crear, lanzando
      NotFoundException igual que BloqueoAgendaService (ya mapeado a 404 por el
      ExceptionHandlingMiddleware existente, se verificó el mapeo). Antes se podían crear
      consultas/prescripciones "huérfanas" con ids inexistentes. Se actualizaron los tests:
      ConsultaMedicaServiceTests.cs (el test que documentaba el hueco ahora verifica que
      tira NotFoundException, +2 tests nuevos de Paciente/Profesional inexistente) y
      PrescripcionServiceTests.cs (+4 tests nuevos: Consulta/Paciente/Profesional/
      Medicamento inexistente). Total esperado ahora: 65/65.
- [x] **[Estética] Paleta de colores unificada + look premium.** Se cambió el hue base del
      sistema de diseño global (styles.css) de azul (215) a índigo (243) — así la mayoría
      de las pantallas, que ya usaban #4f46e5 hardcodeado, casi no cambia de color, y el
      resto (dashboard, login, bloqueos, email-logs) se realinea. Se agregaron tokens
      nuevos (--color-primary-rgb, --color-primary-hover, --color-primary-dark,
      --color-primary-light, --color-primary-bg) y se reemplazaron los hardcodeos
      (#4f46e5, #4338ca, #6366f1, #eef2ff, rgba(79,70,229,...)) por esas variables en los
      16 archivos CSS que los tenían (auditorias, dashboard, especialidades, facturacion,
      historia-clinica, liquidaciones, medicamentos, obras-sociales, pacientes, perfil,
      prestaciones, profesionales, reportes, turnos, bloqueos, email-logs). sala-espera.css
      (paleta violeta separada) se migró también a los mismos tokens y se le sacó el fondo
      en gradiente + la tipografía distinta que tenía hardcodeados, para que herede el
      fondo y la fuente "Outfit" del resto del sistema — se mantuvieron a propósito sus
      6 estados de badge (solicitado/confirmado/en espera/en atención/atendido/cancelado)
      y la superposición oscura de "llamado a paciente", que son un momento visual
      deliberadamente distinto. **Falta que confirmes visualmente que se ve bien** — no
      hay forma de que Claude renderice/capture el resultado en esta sesión.
- [x] **[Bug] "Cancelar turno" ya no borra el registro, lo marca Cancelado.** Se cambió
      turnos.ts: `cancelarTurno()` ahora hace un PUT (Estado="Cancelado") en vez de un
      DELETE físico — reutiliza el mismo mail de cancelación que TurnoService.EditarAsync
      ya envía cuando el Estado pasa a "Cancelado" (backend no se tocó, el fix fue en el
      frontend para dejar de llamar a eliminar()). Se actualizó turnos.html: el badge de
      estado ahora distingue Confirmado/Pendiente/Cancelado (antes solo tenía dos estados),
      y los botones Editar/Confirmar/Cancelar se ocultan una vez que el turno está
      Cancelado (antes el botón Cancelar no tenía guarda y quedaba visible siempre).
      También se agregó manejo de error a `confirmarTurno()` (antes no tenía).
- [x] **[UX menor] Doble toast en Historia Clínica, corregido.** Se sacaron los
      `toastService.error(...)` redundantes de los 7 `.subscribe({error: ...})` en
      historia-clinica.ts (cargar consultas/antecedentes/alergias, crear consulta, subir
      estudio, crear antecedente, crear alergia) — quedó solo el `console.error` para
      debugging, ya que el ErrorInterceptor global muestra el toast con el mensaje real
      del backend para cualquier error HTTP.
- [x] Confirmado por Tito con `dotnet test`: 65/65 en verde (46 previos + 13 de
      Reporte/Search + 6 netos de la validación de FKs de ConsultaMedica/Prescripcion).
- [x] **[Estética] Paleta definitiva "teal médico" + neutros desacoplados.** La ronda
      anterior (índigo) se descartó: se había elegido para minimizar el diff, no por
      criterio de diseño. Ahora `--hue-primary: 175` (teal profundo, ~#0F766E) con acento
      ámbar, sobre una base de grises **cálidos**. Cambio de arquitectura: el hue de marca
      quedó **desacoplado** del hue de los neutros (`--hue-neutral`), que antes eran la
      misma variable — así cambiar la identidad visual ya no tiñe fondos, textos y bordes.
      Argumentable en la defensa como decisión de sistema de diseño, no de gusto.
- [x] **[Estética] Barrido de 403 grises hardcodeados → tokens.** Era la causa real de la
      inconsistencia: 16 CSS de componentes tenían la escala slate de Tailwind pegada a
      mano (#0f172a, #334155, #64748b, #94a3b8, #e2e8f0, #f8fafc, etc.), que **no responde
      a `prefers-color-scheme`**. Por eso el modo oscuro solo funcionaba en 4 pantallas.
      Se mapearon a --text-primary/-secondary/-muted, --border-color/-light y
      --bg-secondary/-card según su uso real (se analizó propiedad por propiedad: los
      grises oscuros eran siempre `color:`, los claros siempre borde o fondo). Además se
      tokenizaron 72 líneas de colores de estado (verde/ámbar/rojo). Quedaron a propósito
      sin tocar dos bloques deliberadamente oscuros: el visor de JSON del diff de
      auditorías y el overlay de llamado a paciente de sala de espera.
- [x] **[Funcionalidad] Agenda/calendario de turnos.** Componente nuevo
      `app/agenda/agenda-semanal.{ts,html,css}` (~830 líneas), desarrollo propio — se
      descartó FullCalendar porque la vista que se necesitaba (una columna por profesional)
      es parte de su versión paga. Dos modos: **Semana** (lunes a viernes en columnas,
      con filtro por profesional) y **Día** (un profesional por columna). Franjas de 30
      min de 08:00 a 20:00, replicando las reglas de negocio ya validadas en el alta.
      Muestra turnos con color por estado, sombrea los bloqueos de agenda con su motivo,
      atenúa las franjas pasadas, marca el día de hoy y lleva un contador de carga por
      columna. Click en un espacio libre abre el alta ya posicionada en ese día/hora/
      profesional; click en un turno abre su edición. Se integró en la pantalla de Turnos
      con un conmutador Listado/Agenda (turnos.html/ts/css), sin pantalla ni ruta nueva.
- [x] **[Tooling] Verificación de frontend sin depender de Tito.** Se logró compilar el
      proyecto Angular en el sandbox de Claude: se empaqueta el fuente (sin node_modules),
      se instala desde npm —que sí está en la lista blanca, a diferencia de NuGet— y se
      corre el compilador AOT `ngc`. Resultado de esta ronda: **0 errores**, incluso
      forzando `strictTemplates` + `fullTemplateTypeCheck`. Además se renderizó la agenda
      con datos de ejemplo en Chromium (Playwright) y se capturó en claro y oscuro para
      validar la maquetación. Es el equivalente frontend del `dotnet test` del backend,
      pero sin intervención manual.

- [x] **[Limpieza] Relevamiento completo de código muerto y basura.** Resultado del
      barrido: el backend **no tiene código muerto** (cero DTOs, entidades o servicios
      huérfanos), no hay un solo TODO/FIXME ni `console.log` olvidado, y las 62 llamadas
      HTTP del frontend dan todas contra alguno de los 77 endpoints reales. Eliminado:
      `backend/frontend/` (prototipo en JS plano anterior a Angular, que el propio
      Program.cs declaraba obsoleto en un comentario), 4 MB de logs de ejecución,
      `Vitalis.Api.http` (andamio de .NET que apuntaba a `/weatherforecast/`), `ng.log`,
      las carpetas `_to_delete/` pendientes, y la ruta duplicada `configuracion` de
      app.config.ts (apuntaba al mismo componente que `especialidades` y ningún menú
      navegaba a ella).
- [x] **[Tests] Suite del frontend reparada.** `ng test` estaba roto y nadie lo sabía:
      `login.spec.ts` importaba una clase `Login` que no existe (es `LoginComponent`), así
      que ni compilaba, y `app.spec.ts` verificaba que la página dijera "Hello,
      vitalis-frontend", el texto del andamio de Angular CLI. Reescritos como pruebas de
      humo reales (5 casos). Verificado en el sandbox: 2 archivos, 5 pruebas, en verde.
- [x] **[Módulo] Reportes rehechos y conectados al backend.** El diagnóstico: la lógica
      de reportes ya estaba escrita y probada en `ReporteService`, pero el frontend **no
      llamaba a ninguno** de sus cuatro endpoints — `reportes.ts` se descargaba todos los
      turnos, pacientes y profesionales y los agregaba en el navegador (algo que además no
      escala). Ni siquiera existía un `reporte.service.ts`.
      Se corrigieron cuatro defectos del backend en el camino: (1) las proyecciones
      mostraban sólo el nombre de pila, sin apellido, a diferencia del resto del sistema;
      (2) **nunca asignaban `Estado`**, así que todos los turnos de todos los reportes
      salían como "Solicitado"; (3) `EstadisticasGenerales` devolvía un `object` anónimo
      sin contrato (los tests debían leerlo por reflexión), ahora es
      `EstadisticasGeneralesDto`; (4) `PorEspecialidad` hacía un GroupJoin que emitía una
      fila **por profesional**, de modo que dos cardiólogos generaban dos filas
      "Cardiología" en vez de sumarse. Se agregaron desgloses por obra social, por
      profesional y serie mensual. Tests de ReporteService: de 5 a **13**, con regresiones
      explícitas para cada uno de esos cuatro defectos.
      El frontend se rehízo con fichas de indicadores, gráfico de línea con tooltip y tres
      gráficos de barras, más una consulta de detalle por profesional (con rango de fechas)
      / paciente / obra social y exportación CSV. Los gráficos son SVG y CSS propios, sin
      librería. Criterio de color validado con herramienta, no a ojo: la primera propuesta
      (barra apilada con los cuatro estados) se descartó porque verde y ámbar quedaban a
      ΔE 5.1 bajo protanopia — indistinguibles para daltonismo rojo-verde. Se reemplazó por
      fichas con etiqueta escrita y barras de un solo tono, donde la longitud codifica la
      magnitud y el color no carga información.

## Pendiente
- [x] **Confirmar 73/73 en el backend**: `dotnet test tests\Vitalis.Tests` (65 previos − 5
      tests viejos de Reporte + 13 nuevos). Cambió `ReporteService`, su interfaz y su DTO. (Completado: se ejecutó dotnet test y pasaron las 81 pruebas con éxito).
- [ ] **Confirmar visualmente la paleta teal** corriendo `ng serve` y recorriendo las
      pantallas. Agenda y Reportes ya se validaron con capturas, pero el resto cambió de
      base cromática y conviene mirarlas.
- [ ] **[Módulo] Pantalla de Prescripciones** (decidido con Tito: construirla). El backend
      tiene `PrescripcionesController` completo con 9 tests, y el frontend tiene
      `prescripcion.service.ts` y su modelo con **0 usos** — falta la pantalla: emitir
      receta desde una consulta, elegir medicamentos del catálogo, historial por paciente
      e impresión.
- [ ] **[Reportes] Ampliar a facturación y liquidaciones.** Los reportes actuales cubren
      la agenda (turnos). El backend no expone todavía indicadores de facturación,
      cobranzas ni liquidaciones a profesionales, que es el otro sector con peso propio.
- [ ] **[Módulo] Bloqueo de agenda incompleto** (marcado por Tito). Falta al menos vista de
      bloqueos vigentes, recurrencia (vacaciones/días fijos) y previsualización del impacto
      sobre los turnos antes de confirmar.
- [ ] **[Módulo] Panel principal básico** (marcado por Tito). Rehacerlo con métricas reales:
      turnos del día, ocupación por profesional, facturación del mes, alertas.
- [ ] **[Módulo] Simulador de correos → registro de notificaciones.** Decidido con Tito:
      reconvertirlo en auditoría real de mails enviados (destinatario, evento que lo
      disparó, fecha, resultado) y evaluar envío real con un servicio gratuito. Confirmar
      los límites del plan gratuito al momento de implementarlo, no de memoria.
- [ ] **[Estética] Sala de espera: revisar el color** (marcado por Tito). La pantalla le
      gusta funcionalmente pero no el color; ya migró a los tokens nuevos, falta que la
      mire con la paleta teal y decida.
- [ ] Recomendación de tooling para acelerar el resto de la tesis (ver detalle en el chat):
      (1) subir a GitHub los cambios de esta sesión (hoy están sin commitear/pushear en tu
      máquina) para que el historial sea real y Claude pueda trabajar directo con git;
      (2) considerar habilitar "Computer use" para que Claude pueda manejar la terminal de
      PowerShell directamente en vez de que copies/pegues resultados; (3) si no, seguir con
      el flujo actual de revisión manual + vos confirmás con `dotnet test`, que ya viene
      funcionando bien.
- [ ] Completar el nombre del director en la portada de Vitalis_Tesina.docx
- [ ] Revisión final de la tesina: actualizar índice en Word, ortografía, referencias
      cruzadas a tablas/figuras (ver plan de desarrollo, sección 6)
- [ ] Ensayar el guion de demo (docs/09) y grabar el video de respaldo
- [ ] Evaluar si conviene agregar accesos de menú para Historia Clínica, Prescripciones,
      Facturación, Liquidaciones, Auditorías, Medicamentos y Prestaciones: hoy esas
      pantallas existen y tienen ruta, pero no tienen entrada en el menú lateral (se
      revisó al ajustar el menú por rol; no se tocó porque excede el alcance de "ocultar
      secciones", pero conviene decidirlo antes de la defensa)
- [ ] Evaluar si conviene una pantalla de gestión de usuarios en el frontend: el backend
      ya tiene UsuariosController (solo Administrador), pero no hay ninguna ruta ni
      componente Angular que lo consuma
- [ ] Revisar backend/frontend/js/app.js: parece un frontend previo/prototipo (no Angular)
      que sigue en el repo con ~10.7k tokens de código. Confirmar si es código muerto que
      se puede borrar o si todavía se usa para algo.
- [ ] (Opcional / trabajo futuro) Control de concurrencia optimista en turnos y facturas
