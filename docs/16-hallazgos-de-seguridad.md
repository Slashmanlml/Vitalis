# Hallazgos de seguridad — FacturaService y LiquidacionService

> Auditoría de los servicios `FacturaService` y `LiquidacionService` y de sus
> controladores. La pregunta de fondo: **¿el mismo patrón que se encontró en
> `ConsultaMedicaService` (guardar el `ProfesionalId`/`PacienteId` que mandaba
> el navegador en vez de resolver la identidad desde el token) está en estos
> dos servicios?**
>
> Respuesta corta: **no**. Los IDs que los DTOs aceptan son el *objeto* de la
> operación (a qué paciente se factura, sobre qué profesional se liquida), no
> la *identidad de quién opera*. Por eso vienen del cliente y está bien que
> así sea. El detalle está abajo.
>
> Auditor: OpenCode / DeepSeek. Fecha: ronda 4. **Sin correcciones aplicadas.**

---

## Hallazgo 1 — `LiquidacionService.CrearAsync` acepta `dto.ProfesionalId` desde el cliente

- **Archivo:** `backend/src/Vitalis.Infrastructure/Services/LiquidacionService.cs`
- **Líneas:** 55, 65, 103
- **Qué permite hacer hoy, en una frase:** un usuario con rol `Facturacion` o
  `Administrador` puede crear una liquidación para cualquier profesional
  mandando su `id` en el cuerpo del POST `/api/Liquidaciones`, y el sistema
  la calcula y la guarda sin chequear que ese profesional le "corresponda" de
  alguna forma.
- **Veredicto: NO es un problema.** No es el mismo patrón que el de la
  consulta médica. En `ConsultaMedicaService.CrearAsync` el `ProfesionalId`
  representaba **quién opera** (el médico que atendía) y se guardaba en la
  historia clínica del paciente — por eso tenía que salir del token o de la
  entidad `Turno`, nunca del cliente. Acá el `ProfesionalId` representa
  **sobre quién se liquida** (es el objeto del cálculo, no la identidad del
  operador): un usuario del sector de facturación es exactamente la persona
  que tiene que poder elegir para qué profesional arma la liquidación. Que
  venga del DTO es correcto. El `ProfesionalId` que el facturador manda
  además se valida con `FindAsync` (línea 55) y se tira si no existe, así
  que no hay forma de liquidar a un id fantasma.

## Hallazgo 2 — `FacturaService.CrearAsync` acepta `dto.PacienteId` desde el cliente

- **Archivo:** `backend/src/Vitalis.Infrastructure/Services/FacturaService.cs`
- **Líneas:** 125
- **Qué permite hacer hoy, en una frase:** un usuario con rol `Facturacion`
  o `Administrador` puede crear una factura para cualquier paciente mandando
  su `id` en el cuerpo del POST `/api/Facturas`, y el sistema la guarda.
- **Veredicto: NO es un problema.** Mismo razonamiento que el hallazgo 1:
  el `PacienteId` en una factura es **a quién se le factura**, no **quién
  está emitiendo la factura**. El operador de facturación es quien tiene
  que poder elegir al paciente. El id se cruza con `Paciente` real a través
  de la FK, así que no se puede facturar a un paciente inexistente. Correcto.

## Hallazgo 3 — `FacturaService.RegistrarPagoAsync` acepta `dto.FacturaId` desde el cliente

- **Archivo:** `backend/src/Vitalis.Infrastructure/Services/FacturaService.cs`
- **Líneas:** 143
- **Qué permite hacer hoy, en una frase:** cualquier usuario con rol
  `Facturacion` o `Administrador` puede registrar un pago contra cualquier
  factura mandando su `id` en el cuerpo del POST `/api/Facturas/pago`,
  incluso si esa factura ya está `Pagada` o pertenece a otro periodo.
- **Veredicto: NO es un problema (con un matiz).** El `FacturaId` es el
  objeto de la operación, no la identidad del operador, así que viene del
  cliente y está bien. **Matiz**: el servicio no chequea si la factura ya
  estaba `Pagada` antes de aceptar el pago nuevo — si se manda un segundo
  pago a una factura ya pagada, el estado se mantiene `Pagada` y se
  acumula un pago extra que no se ve afectado por la transición de estado.
  Esto es un **defecto de integridad de datos**, no de seguridad de acceso:
  un facturador no está saltándose ninguna autorización, solo está dejando
  datos incoherentes. Si se quiere cerrar, va en otro informe (recomendado
  igual: un test del estilo "registrarPago sobre factura Pagada lanza
  BusinessException" lo detecta).

## Hallazgo 4 — Autorización de los controladores: ¿quién puede ver qué?

- **Archivos:**
  - `backend/src/Vitalis.Api/Controllers/FacturasController.cs:11`
  - `backend/src/Vitalis.Api/Controllers/LiquidacionesController.cs:11`
- **Qué permite hacer hoy, en una frase:** ambos controladores tienen
  `[Authorize(Roles = Roles.Administrador + "," + Roles.Facturacion)]` a
  nivel de clase, así que **recepcionistas y médicos no pueden ver ni
  facturación ni liquidaciones**, ni siquiera con un GET directo. El
  servicio tampoco expone datos a un rol que no debería verlos: cuando un
  `Medico` o `Recepcionista` pega a `GET /api/Facturas` recibe 403 antes
  de que el servicio corra.
- **Veredicto: correcto.** La pregunta del brief — *¿debería un
  recepcionista poder ver la facturación de toda la clínica?* — la
  respuesta hoy es **no, y no puede**. La matriz de roles ya lo prohíbe
  y el backend la aplica. No hay un endpoint "huérfano" sin `[Authorize]`
  en estos dos controladores.

## Hallazgo 5 — `ObtenerTodasAsync` no pagina ni filtra por emisor

- **Archivos:**
  - `FacturaService.ObtenerTodasAsync` (líneas 15-44)
  - `LiquidacionService.ObtenerTodasAsync` (líneas 15-32)
- **Qué permite hacer hoy, en una frase:** un usuario con rol
  `Facturacion` ve **todas** las facturas / liquidaciones de la clínica en
  una sola respuesta, sin paginar, sin filtro por fecha, sin distinguir
  quién las emitió.
- **Veredicto: NO es un problema de seguridad, pero conviene mencionarlo.**
  Vitalis es single-clinic (no multi-tenant), así que no hay forma de que
  un facturador vea datos de *otra* clínica. Aceptable para una tesis y
  para el volumen esperado de un consultorio. **Lo dejo registrado** porque
  si en el futuro el sistema se extiende a varias sucursales, este método
  se va a convertir en un agujero: el `Facturacion` de la sucursal A no
  debería ver la facturación de la sucursal B. La solución cuando llegue
  ese día va a ser un `WHERE SucursalId = ...` que se tome del token, no
  del cliente — exactamente el mismo principio que el hallazgo original
  de la consulta médica. Para la defensa actual no es bloqueante.

---

## Lo que miré y descarté explícitamente

Para no obligar a nadie a re-investigar lo mismo:

- **El `ProfesionalId` del DTO en liquidación y el `PacienteId` del DTO en
  facturación NO son identidad del operador.** El bug original de la
  consulta médica era guardar como autor de una historia clínica el id que
  mandaba el cliente, en vez del token o de la entidad turno. Acá los ids
  son objetos de la operación. Documentado arriba.
- **El `Fecha` de la factura se setea server-side** en
  `FacturaService.CrearAsync:126` (`DateTime.UtcNow`). El cliente no puede
  hacer back-dating. Correcto.
- **El cálculo del honorario en liquidación depende de la obra social del
  turno** (`LiquidacionService.CrearAsync:74-97`). No viene del DTO. No
  puede ser manipulado por el operador para inflar el monto. Correcto.
- **El servicio de facturación no filtra por médico emisor.** No hay tal
  concepto en el dominio (el facturador no es un médico, es personal
  administrativo), así que no aplica el principio "filtrar por rol en el
  servidor, no en el cliente" que el AGENTS.md menciona para médicos.
  Correcto.

---

## Resumen ejecutivo

- **0 hallazgos de seguridad reales** en `FacturaService` y `LiquidacionService`
  bajo el criterio del bug original (identidad del operador tomada del
  cuerpo del pedido).
- **1 defecto de integridad menor** (Hallazgo 3, matiz) que no es de
  seguridad y queda fuera del alcance de este informe.
- **1 observación de escalabilidad** (Hallazgo 5) que solo importa si el
  sistema pasa a ser multi-sucursal.

Los controladores están bien protegidos: ni recepcionistas ni médicos llegan
a los servicios de facturación o liquidación.
