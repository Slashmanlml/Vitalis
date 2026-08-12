# Arquitectura y stack tecnologico

## Criterio de seleccion

El sistema se plantea como una aplicacion web dividida en frontend, backend y base de datos. Esta separacion permite organizar responsabilidades, facilitar el mantenimiento y demostrar una arquitectura profesional para la tesis.

## Frontend: Angular

Angular es adecuado para sistemas administrativos porque favorece una estructura modular, componentes reutilizables, formularios robustos, validaciones y navegacion por rutas.

Uso previsto:

- Pantallas de pacientes
- Agenda de turnos
- Historia clinica
- Facturacion
- Reportes
- Paneles por rol

Ventajas:

- Arquitectura ordenada
- Buen soporte para formularios complejos
- Ecosistema maduro
- Muy defendible academicamente

Desventajas:

- Curva de aprendizaje mayor que alternativas mas livianas
- Requiere mantener una estructura prolija desde el inicio

## Backend: ASP.NET Core / .NET 8 LTS

ASP.NET Core permite construir una API REST robusta, segura y mantenible. Es una tecnologia muy usada en sistemas empresariales.

Uso previsto:

- API REST
- Reglas de negocio
- Autenticacion y autorizacion
- Validaciones
- Acceso a datos
- Generacion de reportes

Ventajas:

- Alto rendimiento
- Buena seguridad
- Excelente integracion con Entity Framework Core
- Muy apropiado para sistemas de gestion

Desventajas:

- Requiere instalar SDK de .NET
- Exige organizar bien capas y dependencias

## Base de datos: PostgreSQL

PostgreSQL es una base de datos relacional potente, estable y abierta. Es adecuada para informacion estructurada y sensible como la de un sistema medico.

Uso previsto:

- Pacientes
- Turnos
- Consultas medicas
- Prescripciones
- Facturacion
- Auditoria
- Reportes

Ventajas:

- Confiable y robusta
- Buen manejo de relaciones complejas
- Excelente para reportes
- Software libre

Desventajas:

- Requiere instalacion y administracion inicial
- Conviene disenar bien el modelo para evitar cambios grandes luego

## Arquitectura propuesta

Frontend Angular:

- Modulos por dominio funcional
- Componentes visuales
- Servicios para consumir API
- Guards para proteger rutas
- Interceptors para enviar token JWT

Backend ASP.NET Core:

- Controllers
- Services
- Repositories o acceso mediante DbContext
- DTOs
- Entidades
- Validaciones
- Autenticacion JWT

Base de datos PostgreSQL:

- Tablas relacionales
- Claves primarias y foraneas
- Indices para busquedas frecuentes
- Baja logica en entidades principales
- Auditoria de operaciones relevantes

## Estructura sugerida del repositorio

```text
Vitalis/
  backend/
    Vitalis.Api/
    Vitalis.Application/
    Vitalis.Domain/
    Vitalis.Infrastructure/
  frontend/
    vitalis-web/
  database/
    scripts/
    seed/
  docs/
```

## Capas del backend

- `Domain`: entidades principales y reglas del dominio.
- `Application`: casos de uso, servicios y DTOs.
- `Infrastructure`: base de datos, repositorios, integraciones externas.
- `Api`: controladores, configuracion HTTP, autenticacion y Swagger.

Para una primera version tambien se puede simplificar en un solo proyecto API y luego separar capas si el alcance lo requiere.
