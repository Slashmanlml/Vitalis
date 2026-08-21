# Estado actual del sistema

Este documento describe el sistema **tal como está implementado hoy**, a diferencia de
[05-backlog-mvp.md](05-backlog-mvp.md), que refleja el plan original de trabajo. Se
actualizó después de una revisión técnica completa y una ronda de correcciones de
seguridad y de datos, verificadas contra PostgreSQL real (no solo con tests en memoria).

## 1. Alcance implementado

El sistema construido supera el alcance del MVP original. Además de los 10 módulos
funcionales documentados en [02-modulos-funcionales.md](02-modulos-funcionales.md), están
implementados:

- **Auditoría automática**: toda alta, baja o modificación queda registrada (tabla, acción,
  usuario, valores anteriores/nuevos) mediante un interceptor sobre `SaveChangesAsync`.
- **Bloqueos de agenda**: permite bloquear horarios de un profesional y cancela
  automáticamente los turnos superpuestos, notificando al paciente.
- **Registro de emails simulados** (`EmailLogs`): como no hay proveedor de correo real
  contratado, el sistema registra en base de datos cada notificación que se "enviaría"
  (confirmación de turno, cancelación por bloqueo, etc.), útil para demostrar el flujo sin
  depender de infraestructura externa.
- **Búsqueda global** (`SearchController`): búsqueda unificada por nombre/DNI a través de
  pacientes, profesionales y turnos.
- **Carga de imágenes de perfil** (`UploadsController`), con validación de extensión y
  límite de tamaño.

## 2. Matriz de autorización por rol

El modelo de roles (`Administrador`, `Medico`, `Recepcionista`, `Facturacion`, `Paciente`)
está definido desde el modelo de datos inicial, pero originalmente no se aplicaba de forma
granular a nivel de API. Se revisó y corrigió: los 20 controladores ahora exigen
autenticación y, donde corresponde, un rol específico.

| Módulo | Lectura | Escritura |
|---|---|---|
| Usuarios | Administrador | Administrador |
| Pacientes | Administrador, Médico, Recepcionista, Facturación | Administrador, Recepcionista |
| Profesionales / Especialidades / Obras Sociales / Medicamentos | Cualquier usuario autenticado | Administrador |
| Prestaciones | Cualquier usuario autenticado | Administrador, Facturación |
| Turnos / Bloqueos de agenda | Administrador, Recepcionista, Médico | Administrador, Recepcionista, Médico |
| Historia clínica (consultas, antecedentes, alergias) / Prescripciones | Administrador, Médico | Administrador, Médico |
| Facturas / Liquidaciones | Administrador, Facturación | Administrador, Facturación |
| Reportes / Auditorías / Email logs | Administrador | — (solo lectura) |
| Búsqueda global | Administrador, Médico, Recepcionista, Facturación | — |
| Subida de archivos | Cualquier usuario autenticado | Cualquier usuario autenticado |

La creación de pacientes y la subida de archivos eran, antes de esta revisión, accesibles
sin autenticación (`[AllowAnonymous]`); quedó corregido.

## 3. Bugs corregidos durante el hardening

Se encontraron y corrigieron los siguientes problemas, la mayoría no detectables con la
suite de tests original porque corre contra una base de datos en memoria y no contra
PostgreSQL real:

1. **Fechas rechazadas por PostgreSQL**: cualquier fecha enviada por el cliente sin
   información de zona horaria (por ejemplo, un `<input type="date">` del navegador) hacía
   fallar con error 500 la creación de pacientes, turnos, bloqueos de agenda y
   liquidaciones. PostgreSQL exige `DateTimeKind.Utc` para columnas `timestamp with time
   zone`, y el valor deserializado desde JSON llegaba como `Unspecified`. Corregido
   normalizando la fecha a UTC en el punto de entrada de cada servicio, antes de usarla
   tanto en validaciones como en el guardado.
2. **Auditoría con usuario fijo**: el registro de auditoría siempre atribuía las acciones a
   `admin@vitalis.local`, sin importar qué usuario las ejecutó realmente. Corregido para
   tomar el usuario autenticado de la sesión HTTP.
3. **Doble conteo de pagos en facturación**: al registrar un pago, EF Core vincula
   automáticamente el pago nuevo a la colección de pagos de la factura en cuanto se lo
   agrega al contexto (antes de guardar). El código sumaba el pago nuevo dos veces al
   calcular el total pagado, lo que podía marcar como "Pagada" una factura con pago
   parcial. Corregido calculando el total pagado previo antes de agregar el pago nuevo.
4. **Mensajes de error engañosos en el frontend**: cualquier error de validación (400) se
   mostraba como "Error de conexión con el servidor", ocultando el motivo real (por
   ejemplo, un email con formato inválido). Corregido para interpretar el formato estándar
   de errores de validación de ASP.NET Core y mostrar el mensaje específico del campo.
5. Además de lo anterior: eliminación de código muerto en `Program.cs` (variable sin usar,
   servido estático de un frontend HTML anterior a la migración a Angular), archivo de
   rutas de Angular vacío y sin usar (`app.routes.ts`, las rutas reales viven en
   `app.config.ts`), y URL de API centralizada en `environment.ts` en lugar de repetida en
   17 archivos de servicio.

## 4. Cobertura de tests

`backend/tests/Vitalis.Tests` contiene 13 tests: unitarios para `PacienteService`,
`TurnoService`, `AuthService` y `FacturaService` (con base de datos en memoria), más un
test de integración sobre `PacientesController` que verifica que el endpoint de creación
efectivamente exige autenticación (regresión directa del hallazgo de seguridad corregido).

La cobertura sigue siendo parcial: de los 19 servicios de aplicación, 4 tienen tests
dedicados. Los servicios sin cobertura (Facturación de liquidaciones, Consulta médica,
Prescripciones, Bloqueos de agenda, Reportes, Búsqueda) se ejercitan hoy solo mediante
prueba manual vía Swagger o la interfaz.

## 5. Limitaciones conocidas / trabajo futuro

- El menú del frontend no oculta secciones según el rol del usuario logueado: un usuario
  con rol Médico ve en el menú enlaces a Facturación aunque el backend le niegue el acceso
  (403). Es una mejora de experiencia pendiente, no un problema de seguridad (la API sigue
  protegida).
- El guard de rutas de Angular (`authGuard`) verifica solo que exista un token en
  `localStorage`, no que siga siendo válido o no haya expirado; una sesión vencida se
  detecta recién en la primera llamada a la API (el interceptor de errores redirige a
  `/login` en ese momento).
- No hay control de concurrencia optimista en turnos ni facturas: dos usuarios operando en
  simultáneo sobre el mismo registro pueden pisarse cambios.
- La base de datos es PostgreSQL; ver la nota de justificación de stack en
  [03-arquitectura-y-stack.md](03-arquitectura-y-stack.md) para el argumento frente al
  pedido de frameworks Microsoft.
