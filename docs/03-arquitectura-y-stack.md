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

## Justificación del stack frente al criterio de tecnologías Microsoft

La cátedra recomendó el uso de frameworks de Microsoft para el desarrollo de la tesina.
El stack elegido cumple ese criterio de forma mayoritaria pero no absoluta, y esta sección
explica por qué.

**Componentes del ecosistema Microsoft utilizados:**

- **ASP.NET Core / .NET 8 LTS** como framework de backend y runtime de ejecución.
- **Entity Framework Core**, el ORM oficial de Microsoft para .NET, como capa de acceso a
  datos.
- **Autenticación JWT** implementada con `Microsoft.AspNetCore.Authentication.JwtBearer`,
  paquete oficial del framework.
- **Swagger/OpenAPI** integrado mediante Swashbuckle, el generador estándar de
  documentación de API en el ecosistema ASP.NET Core.
- El propio **flujo de desarrollo** (Visual Studio / Visual Studio Code, `dotnet` CLI,
  NuGet) pertenece íntegramente al ecosistema Microsoft.

**El único componente ajeno al ecosistema Microsoft es el motor de base de datos**, donde
se optó por PostgreSQL en lugar de SQL Server. Esta decisión se sostiene con el siguiente
argumento técnico:

Entity Framework Core está diseñado explícitamente como una capa de abstracción sobre el
motor de base de datos: el código de las capas `Domain`, `Application` e `Infrastructure`
—entidades, reglas de negocio, servicios— no contiene una sola línea específica de
PostgreSQL. La única referencia al motor concreto está en un punto único de configuración
(`DependencyInjection.cs`, línea `options.UseNpgsql(...)`) y en el paquete NuGet
correspondiente (`Npgsql.EntityFrameworkCore.PostgreSQL`). Migrar el proyecto a SQL Server
implicaría reemplazar ese paquete por `Microsoft.EntityFrameworkCore.SqlServer`, ajustar la
cadena de conexión y regenerar las migraciones existentes (ya expresadas en C#, no en SQL
crudo) — un cambio de configuración de infraestructura, no una reescritura de arquitectura
ni de lógica de negocio.

Dicho de otro modo: la elección de PostgreSQL es una decisión de **infraestructura**,
tomada por su solidez para el modelado de datos relacionales complejos y sensibles (propios
de un sistema de salud), y no compromete el criterio de "framework Microsoft" en las capas
donde ese criterio realmente aplica: el lenguaje, el framework web, el ORM y el modelo de
autenticación y autorización. De ser un requisito estricto no negociable, la migración a
SQL Server queda documentada como una tarea acotada y de bajo riesgo, dado que ya se trabaja
con EF Core Code First.

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
    src/
      Vitalis.Api/
      Vitalis.Application/
      Vitalis.Domain/
      Vitalis.Infrastructure/
    tests/
      Vitalis.Tests/
  vitalis-frontend/
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
