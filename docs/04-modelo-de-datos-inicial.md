# Modelo de datos inicial

Este modelo es una primera base para comenzar el diseno. Puede ajustarse cuando avancemos con casos de uso y pantallas.

## Entidades principales

### Usuario

- Id
- Nombre
- Apellido
- Email
- PasswordHash
- RolId
- Activo
- FechaCreacion

### Rol

- Id
- Nombre
- Descripcion

### Paciente

- Id
- Nombre
- Apellido
- Documento
- FechaNacimiento
- Sexo
- Telefono
- Email
- Direccion
- ObraSocialId
- NumeroAfiliado
- ContactoEmergenciaNombre
- ContactoEmergenciaTelefono
- Activo
- FechaCreacion

### Profesional

- Id
- Nombre
- Apellido
- Matricula
- EspecialidadId
- Email
- Telefono
- Activo

### Especialidad

- Id
- Nombre
- Descripcion

### ObraSocial

- Id
- Nombre
- Codigo
- Activa

### Turno

- Id
- PacienteId
- ProfesionalId
- FechaHoraInicio
- FechaHoraFin
- Estado
- Motivo
- Observaciones
- FechaCreacion

### ConsultaMedica

- Id
- PacienteId
- ProfesionalId
- TurnoId
- Fecha
- MotivoConsulta
- Diagnostico
- Evolucion
- Indicaciones
- Observaciones

### AntecedenteClinico

- Id
- PacienteId
- Tipo
- Descripcion
- FechaRegistro

### Alergia

- Id
- PacienteId
- Sustancia
- Reaccion
- Severidad
- Activa

### Prescripcion

- Id
- ConsultaMedicaId
- PacienteId
- ProfesionalId
- Fecha
- Observaciones

### PrescripcionDetalle

- Id
- PrescripcionId
- MedicamentoId
- Dosis
- Frecuencia
- Duracion
- Indicaciones

### Medicamento

- Id
- Nombre
- Presentacion
- Activo

### Prestacion

- Id
- Nombre
- Codigo
- ImporteBase
- Activa

### Factura

- Id
- PacienteId
- Fecha
- Total
- Estado
- Observaciones

### FacturaDetalle

- Id
- FacturaId
- PrestacionId
- Cantidad
- PrecioUnitario
- Subtotal

### Pago

- Id
- FacturaId
- Fecha
- MedioPago
- Importe
- Observaciones

### Liquidacion

- Id
- ProfesionalId
- PeriodoDesde
- PeriodoHasta
- Total
- Estado
- FechaCreacion

### Auditoria

- Id
- UsuarioId
- Accion
- Entidad
- EntidadId
- Fecha
- Detalle

## Relaciones clave

- Un paciente puede tener muchos turnos.
- Un paciente puede tener muchas consultas medicas.
- Un profesional puede atender muchos turnos.
- Un turno puede generar una consulta medica.
- Una consulta medica puede tener una o mas prescripciones.
- Una factura pertenece a un paciente y contiene varias prestaciones.
- Una liquidacion agrupa prestaciones realizadas por un profesional.
- La auditoria registra acciones realizadas por usuarios.
