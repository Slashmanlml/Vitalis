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

- [x] Confirmado por Tito: **73/73** tras rehacer Reportes, y **79/79** tras el módulo
      de mails que sumó Gemini.
- [x] **[Módulo] Pantalla de Prescripciones — la construyó Gemini** (commit `28bea73`).
      Claude estuvo a punto de construirla de nuevo y la habría sobrescrito; se detectó
      revisando `git log` antes de escribir. De ahí salió el plan de trabajo en paralelo
      (docs/11). Se revisó y se le corrigió un problema de seguridad clínica: el
      formulario abría con una **posología precargada** ("500 mg / cada 8 horas / 7 días")
      y el primer medicamento del catálogo preseleccionado, con lo cual alcanzaba con no
      mirar para emitir una receta que ningún profesional indicó. La plantilla ya mostraba
      esos ejemplos como *placeholder*; ahora arrancan vacíos y el medicamento se elige.
- [x] **[Módulo] Bloqueo de agenda completado.** El hueco real no era estético: crear un
      bloqueo **cancela turnos y notifica pacientes de forma irreversible, y no avisaba
      cuántos**. Se agregó `GET /api/BloqueosAgenda/impacto`, que simula el bloqueo sin
      aplicarlo y devuelve los turnos que se perderían, cuántos pacientes afecta y cuántos
      de ellos no tienen correo cargado (o sea, no se van a enterar). El formulario ahora
      exige revisar antes de confirmar.
      Decisión de diseño importante: la previsualización y la cancelación real comparten
      **una sola consulta** (`TurnosAfectadosPor`), porque si cada una tuviera la suya,
      tocar una sola haría que el número anunciado dejara de coincidir con lo que se
      cancela. Hay un test específico que compara ambas.
      Además: separación entre bloqueos vigentes y pasados con marca de "en curso", el
      componente pasó a usar `jwt.util.ts` en vez de decodificar el token a mano (era el
      segundo lugar que lo duplicaba), se sacaron los toasts duplicados, se dejó de pedir
      dos veces la lista de profesionales, y el aviso al eliminar ahora aclara que los
      turnos cancelados **no** se restauran. Tests de BloqueoAgendaService: de 10 a **16**.
- [x] **[Coordinación] Especificaciones para trabajo en paralelo.** Con tres asistentes
      sobre el mismo repositorio, se escribieron dos documentos:
      `docs/10-especificacion-modulo-notificaciones.md` (para Gemini) y
      `docs/11-plan-de-trabajo-en-paralelo.md` (reparto de territorios + brief de la
      pantalla de usuarios para OpenCode/DeepSeek). La regla base: cada archivo tiene un
      único dueño, y los archivos compartidos (rutas, menú, tokens de estilo,
      inyección de dependencias) los toca sólo Claude.
- [x] Limpiadas dos advertencias CS0105 (`using` duplicado en CrearUsuarioDto.cs y
      EditarUsuarioDto.cs).

- [x] **[Documento] Formato APA 7 aplicado a la tesina.** Diagnóstico previo: el
      contenido estaba bien escrito y las 10 referencias todas citadas en el texto (sin
      huérfanas), y ya cumplía Times New Roman 12, interlineado doble y márgenes de
      2,54 cm. Lo que faltaba: **no tenía números de página** (no existía encabezado ni
      pie en todo el documento), los títulos estaban en el azul por defecto de Word
      (`#2E74B5`) y escalonados en 16/13/12 pt, no había sangría de primera línea, y los
      estilos de título carecían de nivel de esquema, que es lo que impide generar el
      índice automático.
      Corregido: títulos en negro al mismo cuerpo que el texto (12 pt), diferenciados por
      negrita y cursiva según nivel APA, con `outlineLvl`; encabezado nuevo con número de
      página arriba a la derecha en las 11 secciones; sangría de 1,27 cm y sin espacio
      extra entre párrafos en los 109 párrafos de cuerpo. Se conservó la numeración de
      secciones por decisión de Tito (convención en tesinas técnicas en español, y el
      propio texto se referencia por número de sección).
      Detalle que importaba: los títulos tenían **formato directo en los runs** que pisaba
      al estilo, así que cambiar sólo la hoja de estilos no habría servido; hubo que
      limpiar los 81. Validado contra el esquema XSD y renderizado a PDF para revisarlo.
- [x] **[Coordinación] Primera ronda en paralelo verificada.** Gemini entregó el módulo de
      notificaciones (MailKit, `IClienteSmtp`, `PlantillasEmail`, `RecordatorioTurnosService`,
      migración `EnriquecerEmailLogParaAuditoria`) y DeepSeek la pantalla de usuarios.
      Ambos respetaron los territorios. Se corrió la lista de aceptación de la
      especificación: cero apariciones de `paciente@vitalis.local`, `SendEmailAsync` y
      `LimpiarLogsAsync` eliminados del contrato, cero hexadecimales en `usuarios.css`,
      `EmailLog` con Origen/Evento/Estado/TurnoId/MensajeError. Verificado por Claude con
      el trabajo de los tres combinado: plantillas estrictas en cero errores, build limpio
      y `ng test` en verde.
      Claude agregó lo que le correspondía: ruta `usuarios` y entrada de menú (sólo
      Administrador). DeepSeek hizo bien en no agregarlas.

- [x] Confirmado por Tito: **100/100** en el backend.
- [x] **[Documento] Defecto grave corregido: las tres figuras se imprimían recortadas.**
      El modelo entidad-relación, la arquitectura y el flujo de información se veían como
      una franja de medio centímetro. La imagen fuente y el tamaño declarado en el XML
      estaban perfectos (6,88 × 4,98 pulgadas, sin recorte); la causa era que los párrafos
      que las contienen heredaban interlineado **exacto** de 24 pt, y una imagen en línea
      dentro de un párrafo con interlineado exacto queda recortada a esa altura. Se les
      puso interlineado simple explícito. Se verificó contra el documento original que el
      defecto era preexistente y no introducido por los cambios de formato.
- [x] **[Documento] Rótulos de figuras y tablas en formato APA 7.** Los 9 estaban debajo
      del elemento, en cursiva y en una sola línea. Ahora van arriba, con el número en
      negrita en su propia línea y el título en cursiva debajo, sin punto final.
- [x] **[Documento] Contenido puesto al día.** Resumen y abstract reescritos (de "trece
      pruebas" a cien, notificaciones reales en lugar de simuladas). Sección 5.3 ampliada
      con la agenda semanal, la previsualización de impacto de bloqueos y los reportes de
      gestión. Sección 6.3 corregida: decía que sólo cuatro servicios tenían pruebas
      dedicadas y que el resto se validaba a mano, cuando hoy están todos cubiertos.
      Sección 6.4 depurada: **tres de las cuatro limitaciones listadas ya estaban
      resueltas** (menú por rol, expiración de token en el guard, notificaciones
      simuladas). Se reemplazaron por limitaciones ciertas —dependencia de que la API
      esté corriendo para los recordatorios, ausencia de cola de reintentos de correo,
      falta de preferencias de notificación del paciente y cobertura acotada del
      frontend— en lugar de simplemente borrarlas: una sección de limitaciones específica
      dice más de un trabajo que una lista corta.

- [x] **[Coordinación] Segunda ronda verificada.** DeepSeek entregó 28 pruebas de
      frontend (jwt.util + 4 servicios) y Gemini el módulo de reportes de facturación.
      Verificado con todo combinado: plantillas estrictas sin errores, build limpio,
      **33 pruebas de frontend en verde** (antes 5).
- [x] **[Módulo] Panel principal rehecho.** Antes mostraba totales históricos —cuántos
      pacientes hay en la base, cuántas obras sociales— que no le sirven a nadie que abre
      el sistema a la mañana. Ahora muestra **la operación del día**: turnos de hoy
      desglosados por estado, los próximos turnos con hora y paciente, y la carga de la
      jornada por profesional. Es sensible al rol: un médico ve su propia jornada, no la
      de toda la clínica.
      Se agregó un aviso accionable que aparece **sólo si hay turnos sin confirmar** —un
      panel que siempre muestra la misma alerta deja de leerse a los dos días— con enlace
      directo a la agenda. Se eliminaron dos servicios inyectados que ya no se usaban y el
      toast de error duplicado, que violaba nuestra propia regla.
      Criterio visual: los cuatro números del encabezado van como fichas y no como
      gráfico, porque son cifras sueltas y no una serie; el color vive en el borde
      superior y el número se queda en tinta de texto para no perder contraste; y la
      etiqueta escrita es lo que comunica el estado, nunca el color solo.

## Pendiente

> Depurado el 27/08/2026. Todo lo que estaba acá y ya se hizo se movió a "Hecho"
> o se eliminó: una lista de pendientes con cosas terminadas adentro deja de
> leerse a los pocos días.

### Bloquea la entrega

- [ ] **[Tito] Nombre del director** para la portada. Pospuesto: todavía no se
      sabe quién integra la mesa.
- [ ] **[Claude] Volcar al documento de la tesina** el material de
      `docs/18-material-para-la-tesina.md`: los tres defectos detectados al
      ejecutar, el hallazgo de autorización, la auditoría de lecturas, la
      integridad de facturación, Docker y el salto de 65 a 116 pruebas.
- [ ] **Actualizar el índice en Word**: Referencias → Actualizar tabla →
      Actualizar todo.
- [ ] **Revisión final de coherencia y ortografía** del documento, cuando el
      texto ya no cambie más.
- [ ] **Ensayar la demo** con `docs/09-guion-demo-y-defensa.md` sobre Docker, y
      grabar el video de respaldo. La mayoría de los tropiezos en una defensa son
      operativos, no conceptuales.
- [ ] **Probar el plan de demostración** (`docs/17`): confirmar qué entradas
      tiene el proyector, probar el acceso desde otra máquina de la red y con el
      celular como punto de acceso.

### Verificación pendiente

- [ ] **Terminar el recorrido de prueba** (`docs/14`): bloque 3 (doce pantallas
      sin recorrer), bloque 4 (circuito completo por rol) y bloque 5 (modo
      oscuro, sesión, URL prohibida, pantalla angosta).
- [ ] **[DeepSeek] Auditoría de seguridad, tandas 2 a 5** — ver `docs/15`. La
      tanda 1 está en `docs/16`: sin hallazgos reales en facturación y
      liquidaciones. Faltan `PacienteService` y `SearchService`,
      `ReporteService` y `ReporteFacturacionService`, `BloqueoAgendaService` y
      controladores, y el barrido de `.filter(` en el frontend.
- [ ] **[DeepSeek] Pruebas de los servicios del frontend** — ver `docs/15`. Cinco
      archivos nuevos: consulta-médica, prescripción, email, usuario y factura.
- [ ] **Pantalla angosta / responsive** (bloque 5 de `docs/14`). Se probó desde
      el celular y no se ve bien. No es prioritario —el jurado usa una
      notebook—, pero achicar la ventana a la mitad es algo que a veces hacen.

### Despliegue y producción (trabajo futuro de la tesina)

> Vitalis corre hoy en contenedores **localmente**. Estar contenedorizado no es
> estar hosteado: falta configuración de producción. Estos puntos van al
> capítulo de trabajo futuro, y son la respuesta si alguien pregunta "¿está
> desplegado?".

- [ ] **Secretos fuera del repositorio.** La clave del JWT y la contraseña de
      PostgreSQL son valores por defecto escritos en `docker-compose.yml`. En
      producción van como secretos del proveedor. Hoy están en el historial de
      git, así que además habría que rotarlas.
- [ ] **HTTPS con certificado.** Sin esto las contraseñas viajan en texto plano.
      Con Let's Encrypt es gratuito.
- [ ] **Base de datos administrada en lugar de un contenedor.** La razón es una
      sola y alcanza: **respaldos**. Un contenedor de PostgreSQL sin copias de
      seguridad es una historia clínica que se pierde entera.
- [ ] **Publicar las imágenes en un registro** (Docker Hub o GitHub Container
      Registry) para que el despliegue sea `pull` y no reconstruir.
- [ ] **Elegir dónde correrlo:** un servidor alquilado con Docker instalado
      (mismo `docker-compose.yml`), o un servicio administrado de contenedores
      que se ocupe de HTTPS y escalado.
- [ ] **Dominio propio.**

### Mejoras identificadas, sin decidir

- [ ] **Bloqueo de agenda: recurrencia.** Hoy se crea un bloqueo por rango de
      fechas. Falta el caso "todos los martes" o "vacaciones anuales". La vista
      previa de impacto ya está hecha.
- [ ] **Doble pago sobre una factura.** Se corrigió el caso de factura saldada y
      el importe negativo. Queda revisar si un pago parcial que excede el saldo
      restante debería recortarse o aceptarse.
- [ ] **Paginación en facturación y liquidaciones.** Hoy se devuelven todos los
      registros en una sola respuesta. Aceptable para el volumen de un
      consultorio; sería un problema con varias sucursales. Ver `docs/16`,
      hallazgo 5.
- [ ] **Sala de espera: revisar el color** (marcado por Tito). Ya migró a los
      tokens nuevos; falta que la mire con la paleta teal y decida.
- [ ] (Opcional) Control de concurrencia optimista en turnos y facturas.
