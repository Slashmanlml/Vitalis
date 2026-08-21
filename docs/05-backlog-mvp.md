# Backlog inicial del MVP

> **Nota:** este documento es el plan de trabajo original y se conserva como registro
> histórico del proceso (útil para mostrar la evolución planeado → construido en la
> tesina). El sistema hoy supera este alcance: ver
> [07-estado-actual-del-sistema.md](07-estado-actual-del-sistema.md) para el estado real
> implementado.

El MVP es la primera version funcional defendible. La idea es construir lo suficiente para demostrar el flujo completo del consultorio.

## Version 1: base del sistema

- Crear solucion backend
- Crear proyecto frontend
- Configurar base de datos PostgreSQL
- Configurar autenticacion JWT
- Crear roles iniciales
- Crear usuarios iniciales
- Configurar Swagger

## Version 2: pacientes y profesionales

- Alta, edicion, baja logica y listado de pacientes
- Busqueda de pacientes por nombre, apellido o documento
- Alta, edicion y listado de profesionales
- Gestion de especialidades
- Gestion de obras sociales

## Version 3: turnos y sala de espera

- Crear turnos
- Consultar agenda por profesional y fecha
- Cancelar turnos
- Reprogramar turnos
- Confirmar asistencia
- Ingresar paciente a sala de espera
- Marcar paciente en atencion
- Marcar paciente atendido o ausente

## Version 4: historia clinica

- Crear consulta medica desde un turno
- Registrar motivo de consulta
- Registrar diagnostico
- Registrar evolucion e indicaciones
- Ver historial clinico por paciente
- Registrar antecedentes y alergias

## Version 5: recetas y prescripciones

- Crear prescripcion desde una consulta
- Cargar medicamentos
- Registrar dosis, frecuencia y duracion
- Generar vista imprimible de receta
- Ver historial de prescripciones

## Version 6: facturacion

- Crear prestaciones
- Generar factura para paciente
- Registrar pagos
- Consultar deuda
- Liquidar prestaciones por profesional

## Version 7: reportes

- Reporte de turnos por periodo
- Reporte de pacientes atendidos
- Reporte de ausentismo
- Reporte de ingresos
- Reporte de prestaciones realizadas

## Prioridad recomendada

1. Seguridad
2. Pacientes
3. Profesionales
4. Turnos
5. Sala de espera
6. Historia clinica
7. Prescripciones
8. Facturacion
9. Reportes
