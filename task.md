# Tareas

> Actualizado tras la revisión técnica y el hardening de seguridad/datos. Ver
> [docs/07-estado-actual-del-sistema.md](docs/07-estado-actual-del-sistema.md) para el
> detalle de qué se corrigió y por qué.

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

## Pendiente

- [ ] Ocultar en el menú del frontend las secciones que el rol del usuario no puede usar
      (hoy la API ya bloquea con 403, pero el link sigue visible)
- [ ] Validar expiración de token en el guard de rutas, no solo su presencia
- [ ] Sumar tests para los servicios sin cobertura (Liquidacion, ConsultaMedica,
      Prescripcion, BloqueoAgenda, Reporte, Search)
- [ ] Diagrama de arquitectura y modelo entidad-relación para la tesina
- [ ] Redactar en la tesina la justificación del stack (PostgreSQL vs. pedido de
      frameworks Microsoft del profesor)
- [ ] Guion de demo en vivo + plan B para la defensa
- [ ] (Opcional / trabajo futuro) Control de concurrencia optimista en turnos y facturas
